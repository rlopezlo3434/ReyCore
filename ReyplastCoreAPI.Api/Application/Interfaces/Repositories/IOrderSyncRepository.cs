using ReyplastCoreAPI.Domain.Catalog.Entities;
using ReyplastCoreAPI.Domain.Orders.Entities;

namespace ReyplastCoreAPI.Application.Interfaces.Repositories;

/// <summary>Cómo se escribe una página de pedidos. Lo decide la OPERACIÓN, no un conteo de filas:
/// el refresco puntual de un pedido guarda con EF normal; los syncs por rango/delta, que traen
/// páginas de cientos o miles de pedidos, usan operaciones masivas (BulkExtensions).</summary>
public enum OrderPersistMode
{
    /// <summary>Sincronización por rango: operaciones masivas (tabla temporal + MERGE).</summary>
    Batch,
    /// <summary>Refresco de un único pedido: guardado EF normal, sin el costo fijo de Bulk.</summary>
    SingleDocument
}

public interface IOrderSyncRepository
{
    // ordersToTouch/detailsToTouch: filas SIN cambios de negocio, vistas de nuevo en esta
    // ejecucion -- solo se actualizan SyncExecutionId/LastSynchronizedAt, sin pagar el costo de
    // reescribir la fila completa (ver PropertiesToInclude en la implementacion).
    Task PersistPageAsync(IReadOnlyCollection<SalesOrder> ordersToInsert,
        IReadOnlyCollection<SalesOrder> ordersToUpdate,
        IReadOnlyCollection<SalesOrder> ordersToTouch,
        IReadOnlyCollection<SalesOrderDetail> detailsToInsert,
        IReadOnlyCollection<SalesOrderDetail> detailsToUpdate,
        IReadOnlyCollection<SalesOrderDetail> detailsToTouch,
        IReadOnlyCollection<SalesOrderDetailQuantityHistory> quantityHistory,
        SyncExecution execution, OrderPersistMode mode, CancellationToken cancellationToken);
    Task PersistSapBatchAsync(IReadOnlyCollection<SalesOrder> ordersToInsert,
        IReadOnlyCollection<SalesOrder> ordersToUpdate,
        IReadOnlyCollection<SalesOrderDetail> detailsToDelete,
        IReadOnlyCollection<SalesOrderDetail> detailsToInsert,
        IReadOnlyCollection<SalesOrderDetail> detailsToUpdate,
        SyncExecution execution, OrderPersistMode mode, CancellationToken cancellationToken);
}
