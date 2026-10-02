using System.Globalization;
using Microsoft.Extensions.Options;
using ReyplastCoreAPI.Application.DTOs.Integrations.Sap;
using ReyplastCoreAPI.Application.DTOs.Order;
using ReyplastCoreAPI.Application.Interfaces.Integrations;
using ReyplastCoreAPI.Application.Interfaces.Repositories;
using ReyplastCoreAPI.Application.Interfaces.Services;
using ReyplastCoreAPI.Domain.Catalog;
using ReyplastCoreAPI.Domain.Catalog.Entities;
using ReyplastCoreAPI.Domain.Orders.Entities;
using ReyplastCoreAPI.Shared.Exceptions;

namespace ReyplastCoreAPI.Application.Services.Orders;

/// <summary>Sincroniza por rango los pedidos obtenidos del API SAP.</summary>
public sealed class SapOrderSyncService : ISapOrderSyncService
{
    private readonly ISapOrderReader _reader;
    private readonly IOrderRepository _orders;
    private readonly IOrderSyncRepository _orderSync;
    private readonly ISyncExecutionRepository _executions;
    private readonly IOrderSyncQueue _queue;
    private readonly OrderSynchronizationLock _synchronizationLock;
    private readonly OrderOnDemandSyncLock _onDemandLock;
    private readonly int _databaseBatchSize;

    public SapOrderSyncService(ISapOrderReader reader, IOrderRepository orders,
        IOrderSyncRepository orderSync, ISyncExecutionRepository executions,
        IOrderSyncQueue queue, OrderSynchronizationLock synchronizationLock, OrderOnDemandSyncLock onDemandLock,
        IOptions<SapOrderSyncOptions> options)
    {
        _reader = reader;
        _orders = orders;
        _orderSync = orderSync;
        _executions = executions;
        _queue = queue;
        _synchronizationLock = synchronizationLock;
        _onDemandLock = onDemandLock;
        _databaseBatchSize = options.Value.DatabaseBatchSize;
    }

    public async Task<OrderDocumentSyncResult> SyncByDocNumAsync(string docNum,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(docNum)) throw new AppException("El número de pedido es obligatorio.");
        string normalizedDocNum = docNum.Trim();
        SyncExecution execution = new()
        {
            Id = Guid.NewGuid(),
            SourceSystem = SourceSystems.Sap,
            ResourceType = SyncResourceTypes.Orders,
            StartedAt = Now(),
            Status = SyncStatuses.Running,
            LastCursor = normalizedDocNum
        };
        await _executions.CreateAsync(execution, cancellationToken);
        try
        {
            SapOrderRecord? record = await _reader.ReadByDocNumAsync(normalizedDocNum, cancellationToken);
            execution.RecordsRead = record is null ? 0 : 1;
            // OrderOnDemandSyncLock (por pedido), no el de Range -- ver comentario en la clase y en
            // OsisOrderSyncService.SyncByDocNumAsync (mismo motivo).
            OrderSyncChangeWindow changes = new();
            using (await _onDemandLock.AcquireAsync(normalizedDocNum, cancellationToken))
            {
                await PersistAsync(record is null ? Array.Empty<SapOrderRecord>() : new[] { record },
                    execution, true, changes, OrderPersistMode.SingleDocument, cancellationToken);
            }
            return record is null
                ? OrderDocumentSyncResult.NotFound
                : OrderDocumentSyncResult.Synced(changed: changes.From is not null);
        }
        catch (OperationCanceledException)
        {
            await _executions.MarkCancelledAsync(execution.Id,
                "La sincronización puntual SAP fue cancelada.", CancellationToken.None);
            throw;
        }
        catch (Exception exception)
        {
            try { await _executions.MarkFailedAsync(execution.Id, exception.Message, CancellationToken.None); }
            catch { }
            throw;
        }
    }

    public async Task<OrderSyncAcceptedResponse> QueueRangeAsync(DateTime dateFrom, DateTime dateTo,
        CancellationToken cancellationToken = default)
    {
        DateTime from = ValidateDate(dateFrom, nameof(dateFrom));
        DateTime to = ValidateDate(dateTo, nameof(dateTo));
        if (from > to) throw new AppException("dateFrom no puede ser mayor que dateTo.");

        SyncExecution execution = new()
        {
            Id = Guid.NewGuid(),
            SourceSystem = SourceSystems.Sap,
            ResourceType = SyncResourceTypes.Orders,
            StartedAt = Now(),
            Status = SyncStatuses.Queued,
            LastCursor = RangeCursor(from, to)
        };
        await _executions.CreateAsync(execution, cancellationToken);
        string? jobId = await _queue.EnqueueSapRangeAsync(execution.Id, from, to, cancellationToken);
        if (jobId is null)
        {
            await _executions.MarkFailedAsync(execution.Id,
                "No fue posible agregar la ejecución SAP a la cola.", CancellationToken.None);
            throw new AppException("No fue posible agregar la sincronización SAP a la cola.", 503);
        }

        return new OrderSyncAcceptedResponse { ExecutionId = execution.Id, Status = execution.Status, JobId = jobId };
    }

    public async Task<OrdersSynchronizedEvent?> ExecuteRangeAsync(Guid executionId, DateTime dateFrom, DateTime dateTo,
        CancellationToken cancellationToken = default)
    {
        try
        {
            SyncExecution execution = await _executions.MarkRunningAsync(executionId, Now(), cancellationToken);
            OrderSyncChangeWindow changes = new();
            IReadOnlyList<SapOrderRecord> records = await _reader.ReadRangeAsync(
                dateFrom.Date, dateTo.Date, cancellationToken);
            EnsureUniqueDocuments(records);
            execution.RecordsRead = records.Count;
            execution.LastCursor = RangeCursor(dateFrom, dateTo);

            await _synchronizationLock.Gate.WaitAsync(cancellationToken);
            try
            {
                if (records.Count == 0)
                {
                    await PersistAsync(records, execution, true, changes, OrderPersistMode.Batch, cancellationToken);
                }
                else
                {
                    SapOrderRecord[][] batches = records.Chunk(_databaseBatchSize).ToArray();
                    for (int index = 0; index < batches.Length; index++)
                        await PersistAsync(batches[index], execution, index == batches.Length - 1,
                            changes, OrderPersistMode.Batch, cancellationToken);
                }
            }
            finally
            {
                _synchronizationLock.Gate.Release();
            }
            return changes.ToEvent(execution, OrderSyncKinds.Range, dateFrom, dateTo);
        }
        catch (OperationCanceledException)
        {
            await _executions.MarkCancelledAsync(executionId,
                "La sincronización SAP fue cancelada.", CancellationToken.None);
            return null;
        }
        catch (Exception exception)
        {
            try { await _executions.MarkFailedAsync(executionId, exception.Message, CancellationToken.None); }
            catch { }
            throw;
        }
    }

    private async Task PersistAsync(IReadOnlyList<SapOrderRecord> source,
        SyncExecution execution, bool isLastBatch, OrderSyncChangeWindow? changes,
        OrderPersistMode mode, CancellationToken cancellationToken)
    {
        List<SalesOrder> existing = source.Count == 0
            ? new List<SalesOrder>()
            : await _orders.GetBySourceDocumentIdsAsync(SourceSystems.Sap,
                source.Select(order => order.SourceDocumentId).ToArray(), cancellationToken);
        Dictionary<string, SalesOrder> bySourceId = existing.ToDictionary(
            order => order.SourceDocumentId!, StringComparer.OrdinalIgnoreCase);
        List<SalesOrder> insertOrders = new();
        List<SalesOrder> updateOrders = new();
        List<SalesOrderDetail> deleteDetails = new();
        List<SalesOrderDetail> insertDetails = new();
        List<SalesOrderDetail> updateDetails = new();
        DateTime synchronizedAt = Now();

        foreach (SapOrderRecord record in source)
        {
            bool isNew = !bySourceId.TryGetValue(record.SourceDocumentId, out SalesOrder? order);
            order ??= new SalesOrder { Id = Guid.NewGuid(), CreatedAt = record.DocDate };
            bool detailsChanged = isNew || !DetailsEqual(order.Details, record.Details);
            bool changed = isNew || HeaderChanged(order, record) || detailsChanged;
            ApplyHeader(order, record, execution.Id, synchronizedAt, isNew, changed);
            // En SAP "changed" ya incluye cambios de lineas (detailsChanged).
            if (changed) changes?.Include(order.DocDate);

            if (isNew)
            {
                insertOrders.Add(order);
                execution.RecordsCreated++;
            }
            else
            {
                updateOrders.Add(order);
                if (changed) execution.RecordsChanged++;
                else execution.RecordsUnchanged++;
            }

            if (detailsChanged)
            {
                if (!isNew) deleteDetails.AddRange(order.Details);
                List<SalesOrderDetail> replacements = BuildDetails(
                    order.Id, record.Details, execution.Id, synchronizedAt);
                insertDetails.AddRange(replacements);
                if (isNew) execution.ChildRecordsCreated += replacements.Count;
                else execution.ChildRecordsChanged += replacements.Count;
            }
            else
            {
                foreach (SalesOrderDetail detail in order.Details)
                {
                    detail.SyncExecutionId = execution.Id;
                    detail.LastSynchronizedAt = synchronizedAt;
                    updateDetails.Add(detail);
                    execution.ChildRecordsUnchanged++;
                }
            }
        }

        if (isLastBatch)
        {
            execution.Status = SyncStatuses.Completed;
            execution.CompletedAt = Now();
        }
        await _orderSync.PersistSapBatchAsync(insertOrders, updateOrders, deleteDetails,
            insertDetails, updateDetails, execution, mode, cancellationToken);
    }

    private static List<SalesOrderDetail> BuildDetails(Guid orderId,
        IReadOnlyList<SapOrderDetailRecord> source, Guid executionId, DateTime synchronizedAt) =>
        source.Select((detail, index) => new SalesOrderDetail
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            // El ordinal es solo una identidad técnica local; no se publica como LineNumber.
            SourceDetailId = (index + 1).ToString("D10", CultureInfo.InvariantCulture),
            LineNumber = null,
            ItemCode = detail.ItemCode,
            Description = detail.Description,
            Quantity = detail.Quantity,
            Price = detail.Price,
            LineTotal = detail.LineTotal,
            IsActive = true,
            SyncExecutionId = executionId,
            LastSynchronizedAt = synchronizedAt,
            CreatedAt = synchronizedAt
        }).ToList();

    private static void ApplyHeader(SalesOrder target, SapOrderRecord source,
        Guid executionId, DateTime synchronizedAt, bool isNew, bool changed)
    {
        target.SourceDocumentId = source.SourceDocumentId;
        target.BranchCode = null;
        target.DocNum = source.DocNum;
        target.DocDate = source.DocDate;
        target.CardCode = source.CardCode;
        target.CardName = source.CardName;
        target.SituationCode = source.SituationCode;
        target.DocTotal = source.DocTotal;
        target.ChannelCode = source.ChannelCode;
        target.ChannelName = source.ChannelName;
        target.SourceSystem = SourceSystems.Sap;
        target.IsActive = true;
        target.SyncExecutionId = executionId;
        target.LastSynchronizedAt = synchronizedAt;
        if (changed && !isNew) target.ModifiedAt = synchronizedAt;
    }

    private static bool HeaderChanged(SalesOrder target, SapOrderRecord source) =>
        target.DocNum != source.DocNum || target.DocDate != source.DocDate
        || target.CardCode != source.CardCode || target.CardName != source.CardName
        || target.SituationCode != source.SituationCode || target.DocTotal != source.DocTotal
        || target.ChannelCode != source.ChannelCode || target.ChannelName != source.ChannelName
        || !target.IsActive || target.SourceSystem != SourceSystems.Sap;

    private static bool DetailsEqual(ICollection<SalesOrderDetail> current,
        IReadOnlyList<SapOrderDetailRecord> source)
    {
        if (current.Count != source.Count) return false;
        SalesOrderDetail[] ordered = current.OrderBy(detail => detail.SourceDetailId).ToArray();
        for (int index = 0; index < ordered.Length; index++)
        {
            SalesOrderDetail target = ordered[index];
            SapOrderDetailRecord incoming = source[index];
            if (target.ItemCode != incoming.ItemCode || target.Description != incoming.Description
                || target.Quantity != incoming.Quantity || target.Price != incoming.Price
                || target.LineTotal != incoming.LineTotal || !target.IsActive)
                return false;
        }
        return true;
    }

    private static void EnsureUniqueDocuments(IEnumerable<SapOrderRecord> records)
    {
        string? duplicate = records.GroupBy(order => order.SourceDocumentId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1)?.Key;
        if (duplicate is not null)
            throw new InvalidDataException($"SAP devolvió DocEntry duplicado: {duplicate}.");
    }

    private static DateTime ValidateDate(DateTime value, string name) => value == default
        ? throw new AppException($"{name} es obligatorio.")
        : value.Date;
    private static string RangeCursor(DateTime from, DateTime to) =>
        $"{from:yyyy-MM-dd}|{to:yyyy-MM-dd}";
    private static DateTime Now() => DateTime.UtcNow.AddHours(-5);
}
