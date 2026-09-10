using ReyplastCoreAPI.Domain.Catalog.Entities;
using ReyplastCoreAPI.Domain.Orders.Entities;

namespace ReyplastCoreAPI.Application.Interfaces.Repositories;

public interface IOrderSyncRepository
{
    Task PersistPageAsync(IReadOnlyCollection<SalesOrder> ordersToInsert,
        IReadOnlyCollection<SalesOrder> ordersToUpdate,
        IReadOnlyCollection<SalesOrderDetail> detailsToInsert,
        IReadOnlyCollection<SalesOrderDetail> detailsToUpdate,
        SyncExecution execution, CancellationToken cancellationToken);
    Task DeactivateMissingAsync(Guid executionId, DateTime changedAt, SyncExecution execution,
        CancellationToken cancellationToken);
}
