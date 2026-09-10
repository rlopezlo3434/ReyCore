using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using ReyplastCoreAPI.Application.Interfaces.Repositories;
using ReyplastCoreAPI.Domain.Catalog.Entities;
using ReyplastCoreAPI.Domain.Orders.Entities;
using ReyplastCoreAPI.Infrastructure.Persistence;

namespace ReyplastCoreAPI.Infrastructure.Repositories;

public sealed class OrderSyncRepository : IOrderSyncRepository
{
    private readonly ReyplastCoreDbContext _context;
    public OrderSyncRepository(ReyplastCoreDbContext context) => _context = context;

    public async Task PersistPageAsync(IReadOnlyCollection<SalesOrder> ordersToInsert,
        IReadOnlyCollection<SalesOrder> ordersToUpdate, IReadOnlyCollection<SalesOrderDetail> detailsToInsert,
        IReadOnlyCollection<SalesOrderDetail> detailsToUpdate, SyncExecution execution,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        if (ordersToInsert.Count > 0)
            await _context.BulkInsertAsync(ordersToInsert.ToList(), cancellationToken: cancellationToken);
        if (ordersToUpdate.Count > 0)
            await _context.BulkUpdateAsync(ordersToUpdate.ToList(), cancellationToken: cancellationToken);
        if (detailsToInsert.Count > 0)
            await _context.BulkInsertAsync(detailsToInsert.ToList(), cancellationToken: cancellationToken);
        if (detailsToUpdate.Count > 0)
            await _context.BulkUpdateAsync(detailsToUpdate.ToList(), cancellationToken: cancellationToken);
        _context.SyncExecutions.Update(execution);
        await _context.SaveChangesAsync(cancellationToken);
        _context.Entry(execution).State = EntityState.Detached;
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task DeactivateMissingAsync(Guid executionId, DateTime changedAt,
        SyncExecution execution, CancellationToken cancellationToken)
    {
        List<SalesOrder> orders = await _context.Orders.AsNoTracking()
            .Where(order => order.SourceSystem == execution.SourceSystem && order.SyncExecutionId != null
                && order.SyncExecutionId != executionId && order.IsActive).ToListAsync(cancellationToken);
        foreach (SalesOrder order in orders)
        {
            order.IsActive = false; order.ModifiedAt = changedAt; order.LastSynchronizedAt = changedAt;
        }
        List<SalesOrderDetail> details = await _context.OrderDetails.AsNoTracking()
            .Where(detail => detail.Order.SourceSystem == execution.SourceSystem && detail.SyncExecutionId != null
                && detail.SyncExecutionId != executionId && detail.IsActive).ToListAsync(cancellationToken);
        foreach (SalesOrderDetail detail in details)
        {
            detail.IsActive = false; detail.ModifiedAt = changedAt; detail.LastSynchronizedAt = changedAt;
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        if (orders.Count > 0) await _context.BulkUpdateAsync(orders, cancellationToken: cancellationToken);
        if (details.Count > 0) await _context.BulkUpdateAsync(details, cancellationToken: cancellationToken);
        execution.Status = SyncStatuses.Completed;
        execution.CompletedAt = changedAt;
        _context.SyncExecutions.Update(execution);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
