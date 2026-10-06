using ReyplastCoreAPI.Application.DTOs.Integrations.Osis;
using ReyplastCoreAPI.Application.DTOs.Order;
using ReyplastCoreAPI.Application.Interfaces.Integrations;
using ReyplastCoreAPI.Application.Interfaces.Repositories;
using ReyplastCoreAPI.Application.Interfaces.Services;
using ReyplastCoreAPI.Domain.Catalog;
using ReyplastCoreAPI.Domain.Catalog.Entities;
using ReyplastCoreAPI.Domain.Orders.Entities;
using ReyplastCoreAPI.Shared.Exceptions;
using ReyplastCoreAPI.Shared.Mappers;
using Microsoft.Extensions.Options;
using ReyplastCoreAPI.Shared.Helpers;

namespace ReyplastCoreAPI.Application.Services.Orders;

/// <summary>
/// Orquesta sincronizaciones OSIS. La consulta HTTP, el seguimiento de ejecuciones y el mapeo
/// viven en componentes separados para evitar mezclar responsabilidades.
/// </summary>
public sealed class OsisOrderSyncService : IOsisOrderSyncService
{
    private readonly OrderSynchronizationLock _synchronizationLock;
    private readonly OrderOnDemandSyncLock _onDemandLock;
    private readonly IOrderRepository _orders;
    private readonly IOrderSyncRepository _orderSync;
    private readonly IOsisOrderReader _reader;
    private readonly IOrderSyncQueue _queue;

    public OsisOrderSyncService(IOrderRepository orders, IOrderSyncRepository orderSync, IOsisOrderReader reader,
        IOrderSyncQueue queue, OrderSynchronizationLock synchronizationLock, OrderOnDemandSyncLock onDemandLock)
    {
        _orders = orders; _orderSync = orderSync;
        _reader = reader; _queue = queue;
        _synchronizationLock = synchronizationLock;
        _onDemandLock = onDemandLock;
    }

    public async Task<OrderSyncAcceptedResponse> QueueRangeAsync(DateTime dateFrom, DateTime dateTo,
        CancellationToken cancellationToken = default)
    {
        DateTime from = DateHelper.RequiredDate(dateFrom, nameof(dateFrom));
        DateTime to = DateHelper.RequiredDate(dateTo, nameof(dateTo));
        if (from > to) throw new AppException("dateFrom no puede ser mayor que dateTo.");
        // Si el mismo rango ya está en cola o corriendo, se devuelve ese job (no se encola otro).
        OrderSyncEnqueueResult job = await _queue.EnqueueOsisRangeAsync(from, to, cancellationToken)
            ?? throw new AppException("No fue posible agregar la sincronización OSIS a la cola.", 503);
        return new OrderSyncAcceptedResponse { ExecutionId = job.ExecutionId, Status = job.Status, JobId = job.JobId };
    }

    /// <summary>
    /// Relee un único pedido de OSIS y lo guarda en RP_ReyPlastCoreBD. No recorre el catálogo completo.
    /// Usa OrderOnDemandSyncLock (NO el de sincronización por rango) -- esto se dispara cada vez que
    /// alguien abre "Asignar"/"Gestionar" y debe responder rápido, sin quedar esperando detrás de
    /// una sincronización masiva; el bloqueo es por pedido, así que pedidos distintos van en paralelo.
    /// Devuelve si el pedido existía y si realmente cambió algo (cabecera o líneas).</summary>
    public async Task<OrderDocumentSyncResult> SyncByDocNumAsync(
        string docNum,
        CancellationToken cancellationToken = default)
    {
        using (await _onDemandLock.AcquireAsync(docNum, cancellationToken))
        {
            // Siempre relee el pedido solicitado para incorporar cambios del origen sin recorrer otros pedidos.
            IReadOnlyList<OsisOrderRecord> source = await _reader.ReadByDocNumAsync(docNum, cancellationToken);
            if (source.Count == 0) return OrderDocumentSyncResult.NotFound;
            if (source.Count > 1)
                throw new AppException(
                    "El número de pedido existe en más de una sucursal OSIS y no es único.", 409);

            SyncExecution execution = NewExecution(Guid.NewGuid());
            execution.RecordsRead = source.Count;
            execution.LastCursor = $"{source[0].DocNum}|{source[0].BranchCode}";
            execution.Status = SyncStatuses.Completed;
            execution.CompletedAt = DateHelper.PeruNow();
            OrderSyncChangeWindow changes = new();
            // Único camino que trae y guarda el detalle: cabecera + TODAS las líneas del documento
            // (si el pedido no estaba en Core, se inserta completo).
            await PersistPageAsync(new OsisOrderPage { Orders = source }, execution, changes,
                OrderPersistMode.SingleDocument, includeDetails: true, cancellationToken);
            return OrderDocumentSyncResult.Synced(changed: changes.From is not null);
        }
    }

    // Contadores de la ejecución en curso (no se persisten: Hangfire es el historial). Su Id se graba como
    // SyncExecutionId en las filas que toca.
    private static SyncExecution NewExecution(Guid executionId) => new()
    {
        Id = executionId,
        SourceSystem = SourceSystems.Osis,
        ResourceType = SyncResourceTypes.Orders,
        StartedAt = DateHelper.PeruNow(),
        Status = SyncStatuses.Running
    };

    public Task<OrdersSynchronizedEvent?> ExecuteRangeAsync(Guid executionId, DateTime dateFrom, DateTime dateTo,
        CancellationToken cancellationToken = default) =>
        ExecutePagedAsync(executionId, OrderSyncKinds.Range, dateFrom.Date, dateTo.Date,
            cursor => _reader.ReadRangePageAsync(dateFrom.Date, dateTo.Date, cursor, cancellationToken),
            cursor => DateHelper.RangeProgress(dateFrom, dateTo, cursor), cancellationToken);

    public Task<OrdersSynchronizedEvent?> ExecuteApprovedDayAsync(Guid executionId, DateTime date,
        CancellationToken cancellationToken = default) =>
        ExecutePagedAsync(executionId, OrderSyncKinds.ApprovedDay, date.Date, date.Date,
            cursor => _reader.ReadApprovedDayPageAsync(date.Date, cursor, cancellationToken),
            cursor => string.IsNullOrWhiteSpace(cursor)
                ? $"aprobados:{date:yyyy-MM-dd}"
                : $"aprobados:{date:yyyy-MM-dd}@{cursor}", cancellationToken);

    // Recorre las páginas de OSIS y persiste cada una con la misma comparación (nuevo / cambiado / sin
    // cambios) para cualquier tipo de sincronización; solo cambia la consulta que arma cada página.
    private async Task<OrdersSynchronizedEvent?> ExecutePagedAsync(Guid executionId, string kind,
        DateTime eventDateFrom, DateTime eventDateTo, Func<string?, Task<OsisOrderPage>> readPage,
        Func<string?, string> progress, CancellationToken cancellationToken)
    {
        try
        {
            SyncExecution execution = NewExecution(executionId);
            OrderSyncChangeWindow changes = new();
            string? cursor = null;
            do
            {
                OsisOrderPage page = await readPage(cursor);
                execution.RecordsRead += page.Orders.Count;
                execution.LastCursor = progress(page.NextCursor);
                if (!page.HasMore)
                {
                    execution.Status = SyncStatuses.Completed;
                    execution.CompletedAt = DateHelper.PeruNow();
                }

                await _synchronizationLock.Gate.WaitAsync(cancellationToken);
                try
                {
                    // Masivo: solo agrega o actualiza cabeceras (el rango además trae el resumen de líneas y
                    // unidades, que vive en la cabecera). Las líneas solo se escriben por docNumber.
                    if (page.Orders.Count > 0)
                        await PersistPageAsync(page, execution, changes, OrderPersistMode.Batch,
                            includeDetails: false, cancellationToken);
                }
                finally
                {
                    _synchronizationLock.Gate.Release();
                }

                if (!page.HasMore) break;
                cursor = page.NextCursor
                    ?? throw new AppException("OSIS no devolvió el cursor del pedido.");
            } while (true);
            return changes.ToEvent(execution, kind, eventDateFrom, eventDateTo);
        }
        catch (OperationCanceledException)
        {
            // Cancelada (apagado del API): Hangfire la deja registrada; sin aviso a MasterLogic.
            return null;
        }
    }

    // Siempre se evalúa contra lo que ya existe en Core antes de decidir insertar o actualizar. El refresco
    // por docNumber y la sincronización masiva usan candados distintos, así que el otro puede insertar el
    // mismo pedido entre la lectura y el guardado: el índice único lo rechaza, nada queda a medias (todo va
    // en una transacción) y se vuelve a evaluar una vez -- lo que se iba a insertar pasa a actualización.
    private async Task PersistPageAsync(OsisOrderPage page, SyncExecution execution,
        OrderSyncChangeWindow? changes, OrderPersistMode mode, bool includeDetails, CancellationToken cancellationToken)
    {
        (int Created, int Changed, int Unchanged, int ChildCreated, int ChildChanged, int ChildUnchanged) counters =
            (execution.RecordsCreated, execution.RecordsChanged, execution.RecordsUnchanged,
             execution.ChildRecordsCreated, execution.ChildRecordsChanged, execution.ChildRecordsUnchanged);
        try
        {
            await UpsertPageAsync(page, execution, changes, mode, includeDetails, cancellationToken);
        }
        catch (Exception exception) when (DatabaseErrorHelper.IsDuplicateKey(exception))
        {
            (execution.RecordsCreated, execution.RecordsChanged, execution.RecordsUnchanged,
             execution.ChildRecordsCreated, execution.ChildRecordsChanged, execution.ChildRecordsUnchanged) = counters;
            await UpsertPageAsync(page, execution, changes, mode, includeDetails, cancellationToken);
        }
    }

    // includeDetails = false (sincronización masiva): solo se inserta/actualiza la cabecera; las líneas
    // guardadas no se tocan ni se desactivan (la página llega sin detalle a propósito).
    private async Task UpsertPageAsync(OsisOrderPage page, SyncExecution execution,
        OrderSyncChangeWindow? changes, OrderPersistMode mode, bool includeDetails, CancellationToken cancellationToken)
    {
        string[] keys = page.Orders.Select(OsisOrderSyncMapper.BuildSourceKey).ToArray();
        List<SalesOrder> existing = await _orders.GetByKeysAsync(keys, includeDetails, cancellationToken);
        Dictionary<string, SalesOrder> byKey = existing.ToDictionary(
            OsisOrderSyncMapper.BuildSourceKey, StringComparer.OrdinalIgnoreCase);
        List<SalesOrder> insertOrders = new();
        List<SalesOrder> updateOrders = new();
        // "Touch": vistos de nuevo en esta ejecucion pero SIN cambios reales -- ver comentario en
        // IOrderSyncRepository.PersistPageAsync. Antes se mezclaban con updateOrders/updateDetails
        // y BulkUpdate reescribia la fila COMPLETA (todas las columnas) aunque nada hubiera
        // cambiado, solo porque ApplyHeader/ApplyDetail sellan SyncExecutionId/LastSynchronizedAt
        // sin condicion. Con ~207k detalles "sin cambios" por ejecucion completa, eso era
        // literalmente reescribir casi toda la tabla en cada sync.
        List<SalesOrder> touchOrders = new();
        List<SalesOrderDetail> insertDetails = new();
        List<SalesOrderDetail> updateDetails = new();
        List<SalesOrderDetail> touchDetails = new();
        DateTime synchronizedAt = DateHelper.PeruNow();

        foreach (OsisOrderRecord source in page.Orders)
        {
            bool isNew = !byKey.TryGetValue(OsisOrderSyncMapper.BuildSourceKey(source), out SalesOrder? order);
            order ??= new SalesOrder { Id = Guid.NewGuid(), CreatedAt = source.DocDate };
            bool changed = isNew || OsisOrderSyncMapper.HasHeaderChanged(order, source);
            OsisOrderSyncMapper.ApplyHeader(order, source, execution.Id, synchronizedAt, isNew, changed);
            if (isNew) { insertOrders.Add(order); execution.RecordsCreated++; }
            else if (changed) { updateOrders.Add(order); execution.RecordsChanged++; }
            else { touchOrders.Add(order); execution.RecordsUnchanged++; }
            // Un pedido "touch" en cabecera igual cuenta como cambiado si alguna linea cambio.
            bool orderAffected = changed;
            if (!includeDetails)
            {
                if (orderAffected) changes?.Include(order.DocDate);
                continue;
            }

            // La PK real de PPD termina en PRD_CODPRD: dentro de una cabecera OSIS,
            // ItemCode es único mientras que PPD_CORPPD puede repetirse.
            Dictionary<string, SalesOrderDetail> details = order.Details
                .ToDictionary(detail => detail.SourceDetailId, StringComparer.OrdinalIgnoreCase);
            HashSet<string> seenDetails = new(StringComparer.OrdinalIgnoreCase);
            foreach (OsisOrderDetailRecord sourceDetail in source.Details)
            {
                seenDetails.Add(sourceDetail.ItemCode);
                bool detailIsNew = !details.TryGetValue(sourceDetail.ItemCode, out SalesOrderDetail? detail);
                detail ??= new SalesOrderDetail { Id = Guid.NewGuid(), OrderId = order.Id, CreatedAt = synchronizedAt };
                bool detailChanged = detailIsNew || OsisOrderSyncMapper.HasDetailChanged(detail, sourceDetail);
                OsisOrderSyncMapper.ApplyDetail(
                    detail, sourceDetail, execution.Id, synchronizedAt, detailIsNew, detailChanged);
                if (detailIsNew) { insertDetails.Add(detail); execution.ChildRecordsCreated++; orderAffected = true; }
                else if (detailChanged) { updateDetails.Add(detail); execution.ChildRecordsChanged++; orderAffected = true; }
                else { touchDetails.Add(detail); execution.ChildRecordsUnchanged++; }
            }

            // Una actualización puntual solo desactiva líneas ausentes de esta cabecera consultada (no se
            // borran) y deja constancia en el historial.
            foreach (SalesOrderDetail missing in order.Details.Where(detail =>
                         detail.IsActive && !seenDetails.Contains(detail.SourceDetailId)))
            {
                missing.IsActive = false;
                missing.SyncExecutionId = execution.Id;
                missing.LastSynchronizedAt = synchronizedAt;
                missing.ModifiedAt = synchronizedAt;
                updateDetails.Add(missing);
                execution.ChildRecordsChanged++;
                orderAffected = true;
            }

            if (orderAffected) changes?.Include(order.DocDate);
        }

        await _orderSync.PersistPageAsync(insertOrders, updateOrders, touchOrders, insertDetails, updateDetails,
            touchDetails, execution, mode, cancellationToken);
    }

}
