using ReyplastCoreAPI.Application.DTOs.Integrations.Osis;
using ReyplastCoreAPI.Application.DTOs.Order;
using ReyplastCoreAPI.Application.DTOs.Product.Response;
using ReyplastCoreAPI.Application.Interfaces.Integrations;
using ReyplastCoreAPI.Application.Interfaces.Repositories;
using ReyplastCoreAPI.Application.Interfaces.Services;
using ReyplastCoreAPI.Domain.Catalog;
using ReyplastCoreAPI.Domain.Catalog.Entities;
using ReyplastCoreAPI.Domain.Orders.Entities;
using ReyplastCoreAPI.Shared.Exceptions;
using ReyplastCoreAPI.Shared.Helpers;
using ReyplastCoreAPI.Shared.Mappers;

namespace ReyplastCoreAPI.Application.Services.Orders;

public sealed class OrderSyncService : IOrderSyncService
{
    private static readonly SemaphoreSlim PersistenceLock = new(1, 1);
    private readonly IOrderRepository _orders;
    private readonly IOrderSyncRepository _orderSync;
    private readonly IProductSyncRepository _executions;
    private readonly IOsisOrderReader _reader;
    private readonly IOrderSyncQueue _queue;

    public OrderSyncService(IOrderRepository orders, IOrderSyncRepository orderSync,
        IProductSyncRepository executions, IOsisOrderReader reader, IOrderSyncQueue queue)
    {
        _orders = orders; _orderSync = orderSync; _executions = executions; _reader = reader; _queue = queue;
    }

    public async Task<OrderSyncAcceptedResponse> QueueOsisAsync(CancellationToken cancellationToken = default)
    {
        SyncExecution execution = new()
        {
            Id = Guid.NewGuid(), SourceSystem = SourceSystems.Osis, ResourceType = SyncResourceTypes.Orders,
            StartedAt = Now(), Status = SyncStatuses.Queued
        };
        await _executions.CreateExecutionAsync(execution, cancellationToken);
        if (!_queue.TryEnqueueOsis(execution.Id))
        {
            await _executions.MarkFailedAsync(execution.Id, "No fue posible agregar la ejecución a la cola.",
                CancellationToken.None);
            throw new AppException("No fue posible agregar la sincronización a la cola.", 503);
        }
        return new OrderSyncAcceptedResponse
        {
            ExecutionId = execution.Id, Status = execution.Status,
            StatusUrl = $"/api/orders/sync/executions/{execution.Id}"
        };
    }

    /// <summary>
    /// Importa un único pedido ausente en ReyplastCoreDB. No recorre el catálogo completo.
    /// </summary>
    public async Task<bool> SyncOsisByDocNumAsync(
        string docNum,
        CancellationToken cancellationToken = default)
    {
        await PersistenceLock.WaitAsync(cancellationToken);
        try
        {
            // Otra solicitud pudo importarlo mientras esta esperaba el bloqueo.
            if ((await _orders.GetCompleteByDocNumAsync(docNum, cancellationToken)).Count > 0)
                return true;

            IReadOnlyList<OsisOrderRecord> source = await _reader.ReadByDocNumAsync(docNum, cancellationToken);
            if (source.Count == 0) return false;
            if (source.Count > 1)
                throw new AppException(
                    "El número de pedido existe en más de una sucursal OSIS y no es único.", 409);

            DateTime startedAt = Now();
            SyncExecution execution = new()
            {
                Id = Guid.NewGuid(),
                SourceSystem = SourceSystems.Osis,
                ResourceType = SyncResourceTypes.Orders,
                StartedAt = startedAt,
                Status = SyncStatuses.Running
            };
            await _executions.CreateExecutionAsync(execution, cancellationToken);
            try
            {
                execution.RecordsRead = source.Count;
                execution.LastCursor = $"{source[0].DocNum}|{source[0].BranchCode}";
                execution.Status = SyncStatuses.Completed;
                execution.CompletedAt = Now();
                await PersistPageAsync(new OsisOrderPage { Orders = source }, execution, cancellationToken);
                return true;
            }
            catch (OperationCanceledException)
            {
                await _executions.MarkCancelledAsync(
                    execution.Id, "La sincronización bajo demanda fue cancelada.", CancellationToken.None);
                throw;
            }
            catch (Exception exception)
            {
                try { await _executions.MarkFailedAsync(execution.Id, exception.Message, CancellationToken.None); }
                catch { }
                throw;
            }
        }
        finally
        {
            PersistenceLock.Release();
        }
    }

    public async Task ExecuteOsisAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        try
        {
            SyncExecution execution = await _executions.MarkRunningAsync(executionId, Now(), cancellationToken);
            string? cursor = null;
            do
            {
                OsisOrderPage page = await _reader.ReadPageAsync(cursor, cancellationToken);
                if (page.Orders.Count == 0)
                {
                    if (execution.RecordsRead == 0) throw new AppException("OSIS no contiene pedidos.");
                    break;
                }
                execution.RecordsRead += page.Orders.Count;
                execution.LastCursor = page.NextCursor;
                await PersistenceLock.WaitAsync(cancellationToken);
                try
                {
                    await PersistPageAsync(page, execution, cancellationToken);
                }
                finally
                {
                    PersistenceLock.Release();
                }
                if (!page.HasMore) break;
                cursor = page.NextCursor ?? throw new AppException("OSIS no devolvió el cursor del pedido.");
            } while (true);

            await _orderSync.DeactivateMissingAsync(executionId, Now(), execution, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            await _executions.MarkCancelledAsync(executionId, "La sincronización fue cancelada.", CancellationToken.None);
        }
        catch (Exception exception)
        {
            try { await _executions.MarkFailedAsync(executionId, exception.Message, CancellationToken.None); }
            catch { }
            throw;
        }
    }

    private async Task PersistPageAsync(OsisOrderPage page, SyncExecution execution,
        CancellationToken cancellationToken)
    {
        string[] keys = page.Orders.Select(Key).ToArray();
        List<SalesOrder> existing = await _orders.GetByKeysAsync(keys, cancellationToken);
        Dictionary<string, SalesOrder> byKey = existing.ToDictionary(Key, StringComparer.OrdinalIgnoreCase);
        List<SalesOrder> insertOrders = new();
        List<SalesOrder> updateOrders = new();
        List<SalesOrderDetail> insertDetails = new();
        List<SalesOrderDetail> updateDetails = new();
        DateTime synchronizedAt = Now();

        foreach (OsisOrderRecord source in page.Orders)
        {
            bool isNew = !byKey.TryGetValue(Key(source), out SalesOrder? order);
            order ??= new SalesOrder { Id = Guid.NewGuid(), CreatedAt = source.DocDate };
            bool changed = isNew || HasChanged(order, source);
            Apply(order, source, execution.Id, synchronizedAt, isNew, changed);
            if (isNew) { insertOrders.Add(order); execution.RecordsCreated++; }
            else { updateOrders.Add(order); if (changed) execution.RecordsChanged++; else execution.RecordsUnchanged++; }

            // La PK real de PPD termina en PRD_CODPRD: dentro de una cabecera OSIS,
            // ItemCode es único mientras que PPD_CORPPD puede repetirse.
            Dictionary<string, SalesOrderDetail> details = order.Details
                .ToDictionary(detail => detail.SourceDetailId, StringComparer.OrdinalIgnoreCase);
            foreach (OsisOrderDetailRecord sourceDetail in source.Details)
            {
                bool detailIsNew = !details.TryGetValue(sourceDetail.ItemCode, out SalesOrderDetail? detail);
                detail ??= new SalesOrderDetail { Id = Guid.NewGuid(), OrderId = order.Id, CreatedAt = synchronizedAt };
                bool detailChanged = detailIsNew || HasChanged(detail, sourceDetail);
                Apply(detail, sourceDetail, execution.Id, synchronizedAt, detailIsNew, detailChanged);
                if (detailIsNew) { insertDetails.Add(detail); execution.ChildRecordsCreated++; }
                else { updateDetails.Add(detail); if (detailChanged) execution.ChildRecordsChanged++;
                    else execution.ChildRecordsUnchanged++; }
            }
        }

        await _orderSync.PersistPageAsync(insertOrders, updateOrders, insertDetails, updateDetails,
            execution, cancellationToken);
    }

    private static void Apply(SalesOrder target, OsisOrderRecord source, Guid syncId, DateTime at,
        bool isNew, bool changed)
    {
        target.SourceDocumentId = $"{source.BranchCode}|{source.DocNum}";
        target.BranchCode = source.BranchCode; target.DocNum = source.DocNum; target.DocDate = source.DocDate;
        target.CardCode = source.CardCode; target.CardName = source.CardName;
        target.SituationCode = source.SituationCode;
        target.DocTotal = source.DocTotal; target.ChannelCode = source.ChannelCode; target.ChannelName = source.ChannelName;
        target.IsActive = source.IsActive; target.SourceSystem = SourceSystems.Osis;
        target.SyncExecutionId = syncId; target.LastSynchronizedAt = at;
        if (changed && !isNew) target.ModifiedAt = at;
    }

    private static void Apply(SalesOrderDetail target, OsisOrderDetailRecord source, Guid syncId, DateTime at,
        bool isNew, bool changed)
    {
        target.SourceDetailId = source.ItemCode;
        target.LineNumber = source.LineNumber; target.ItemCode = source.ItemCode;
        target.Description = Normalize(source.Description); target.Quantity = source.Quantity;
        target.Price = source.Price; target.LineTotal = source.LineTotal; target.IsActive = source.IsActive;
        target.SyncExecutionId = syncId; target.LastSynchronizedAt = at;
        if (changed && !isNew) target.ModifiedAt = at;
    }

    private static bool HasChanged(SalesOrder x, OsisOrderRecord y) => x.DocDate != y.DocDate
        || x.CardCode != y.CardCode || x.CardName != y.CardName || x.SituationCode != y.SituationCode
        || x.DocTotal != y.DocTotal || x.ChannelCode != y.ChannelCode || x.ChannelName != y.ChannelName
        || x.IsActive != y.IsActive || x.SourceSystem != SourceSystems.Osis;
    private static bool HasChanged(SalesOrderDetail x, OsisOrderDetailRecord y) =>
        x.LineNumber != y.LineNumber || x.Description != Normalize(y.Description) || x.Quantity != y.Quantity
        || x.Price != y.Price || x.LineTotal != y.LineTotal || x.IsActive != y.IsActive;
    private static string Key(SalesOrder x) => $"{x.BranchCode}|{x.DocNum}";
    private static string Key(OsisOrderRecord x) => $"{x.BranchCode}|{x.DocNum}";
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public async Task<PaginatedResponse<SyncExecutionResponse>> GetExecutionsAsync(int pageNumber, int pageSize,
        string? sourceSystem, CancellationToken cancellationToken = default)
    {
        ValidationHelper.ThrowIfInvalidPage(pageNumber, pageSize);
        string? source = NormalizeOptionalSourceSystem(sourceSystem);
        PaginatedResponse<SyncExecution> page = await _executions.GetExecutionsAsync(pageNumber, pageSize,
            source, SyncResourceTypes.Orders, cancellationToken);
        return new PaginatedResponse<SyncExecutionResponse>
        {
            Items = page.Items.Select(ProductMapper.ToResponse).ToList(), PageNumber = page.PageNumber,
            PageSize = page.PageSize, TotalRecords = page.TotalRecords, TotalPages = page.TotalPages
        };
    }

    public async Task<SyncExecutionResponse> GetExecutionAsync(Guid executionId,
        CancellationToken cancellationToken = default)
    {
        SyncExecution? execution = await _executions.GetExecutionAsync(executionId, cancellationToken);
        if (execution is null || execution.ResourceType != SyncResourceTypes.Orders)
            throw new AppException("No se encontró la ejecución de pedidos.", 404);
        return ProductMapper.ToResponse(execution);
    }

    public async Task<SyncExecutionResponse> CancelExecutionAsync(Guid executionId,
        CancellationToken cancellationToken = default)
    {
        SyncExecutionResponse execution = await GetExecutionAsync(executionId, cancellationToken);
        if (execution.Status is SyncStatuses.Completed or SyncStatuses.Failed or SyncStatuses.Cancelled)
            throw new AppException("La ejecución ya terminó.", 409);
        if (!_queue.TryCancel(executionId)) throw new AppException("La ejecución no está activa en esta instancia.", 409);
        return execution;
    }

    private static DateTime Now() => DateTime.UtcNow.AddHours(-5);

    private static string? NormalizeOptionalSourceSystem(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        string normalized = value.Trim().ToUpperInvariant();
        if (!SourceSystems.All.Contains(normalized))
            throw new AppException("El sistema de origen no es válido.");
        return normalized;
    }
}
