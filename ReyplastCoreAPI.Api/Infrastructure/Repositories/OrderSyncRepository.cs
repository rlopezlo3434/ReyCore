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

    // Columnas que "tocan" una fila sin cambios de negocio -- solo SyncExecutionId y
    // LastSynchronizedAt. Antes TODA fila (cambiada o no) se reescribia completa via BulkUpdate
    // porque ApplyHeader/ApplyDetail sellaban esos campos sin condicion; acotar las columnas de
    // esas filas reduce drasticamente cuanto dato viaja y se escribe.
    // Arrays inmutables a proposito: BulkExtensions MODIFICA la lista que recibe en
    // PropertiesToInclude (le agrega la llave "Id"). Con una List estatica compartida, tras el primer
    // BulkUpdate la lista quedaba con "Id" y el guardado EF de lotes chicos intentaba marcar la llave
    // como modificada (InvalidOperationException). A Bulk siempre se le pasa una copia nueva.
    private static readonly string[] TouchOnlyOrderColumns =
        { nameof(SalesOrder.SyncExecutionId), nameof(SalesOrder.LastSynchronizedAt) };
    private static readonly string[] TouchOnlyDetailColumns =
        { nameof(SalesOrderDetail.SyncExecutionId), nameof(SalesOrderDetail.LastSynchronizedAt) };

    // El modo lo decide quien llama segun la operacion (ver OrderPersistMode). Cada operacion
    // Bulk crea una tabla temporal, copia las filas con SqlBulkCopy y hace un MERGE contra la
    // tabla real: rinde con paginas de cientos/miles de pedidos, pero para un pedido suelto
    // (refresco al abrir "Asignar") ese costo fijo se repetia por cada lista.
    public async Task PersistPageAsync(IReadOnlyCollection<SalesOrder> ordersToInsert,
        IReadOnlyCollection<SalesOrder> ordersToUpdate, IReadOnlyCollection<SalesOrder> ordersToTouch,
        IReadOnlyCollection<SalesOrderDetail> detailsToInsert, IReadOnlyCollection<SalesOrderDetail> detailsToUpdate,
        IReadOnlyCollection<SalesOrderDetail> detailsToTouch,
        SyncExecution execution,
        OrderPersistMode mode, CancellationToken cancellationToken)
    {
        if (mode == OrderPersistMode.SingleDocument)
        {
            await SaveTrackedAsync(tracker =>
            {
                tracker.MarkAll(ordersToInsert, EntityState.Added);
                tracker.MarkAll(ordersToUpdate, EntityState.Modified);
                tracker.MarkColumns(ordersToTouch, TouchOnlyOrderColumns);
                tracker.MarkAll(detailsToInsert, EntityState.Added);
                tracker.MarkAll(detailsToUpdate, EntityState.Modified);
                tracker.MarkColumns(detailsToTouch, TouchOnlyDetailColumns);
            }, cancellationToken);
            return;
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        if (ordersToInsert.Count > 0)
            await _context.BulkInsertAsync(ordersToInsert.ToList(), cancellationToken: cancellationToken);
        if (ordersToUpdate.Count > 0)
            await _context.BulkUpdateAsync(ordersToUpdate.ToList(), cancellationToken: cancellationToken);
        if (ordersToTouch.Count > 0)
            await _context.BulkUpdateAsync(ordersToTouch.ToList(),
                new BulkConfig { PropertiesToInclude = TouchOnlyOrderColumns.ToList() }, cancellationToken: cancellationToken);
        if (detailsToInsert.Count > 0)
            await _context.BulkInsertAsync(detailsToInsert.ToList(), cancellationToken: cancellationToken);
        if (detailsToUpdate.Count > 0)
            await _context.BulkUpdateAsync(detailsToUpdate.ToList(), cancellationToken: cancellationToken);
        if (detailsToTouch.Count > 0)
            await _context.BulkUpdateAsync(detailsToTouch.ToList(),
                new BulkConfig { PropertiesToInclude = TouchOnlyDetailColumns.ToList() }, cancellationToken: cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task PersistSapBatchAsync(IReadOnlyCollection<SalesOrder> ordersToInsert,
        IReadOnlyCollection<SalesOrder> ordersToUpdate,
        IReadOnlyCollection<SalesOrderDetail> detailsToDelete,
        IReadOnlyCollection<SalesOrderDetail> detailsToInsert,
        IReadOnlyCollection<SalesOrderDetail> detailsToUpdate,
        SyncExecution execution, OrderPersistMode mode, CancellationToken cancellationToken)
    {
        if (mode == OrderPersistMode.SingleDocument)
        {
            // Mismo orden logico que la ruta Bulk: EF agrupa todo en una sola transaccion.
            await SaveTrackedAsync(tracker =>
            {
                tracker.MarkAll(detailsToDelete, EntityState.Deleted);
                tracker.MarkAll(ordersToInsert, EntityState.Added);
                tracker.MarkAll(ordersToUpdate, EntityState.Modified);
                tracker.MarkAll(detailsToInsert, EntityState.Added);
                tracker.MarkAll(detailsToUpdate, EntityState.Modified);
            }, cancellationToken);
            return;
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        if (detailsToDelete.Count > 0)
            await _context.BulkDeleteAsync(detailsToDelete.ToList(), cancellationToken: cancellationToken);
        if (ordersToInsert.Count > 0)
            await _context.BulkInsertAsync(ordersToInsert.ToList(), cancellationToken: cancellationToken);
        if (ordersToUpdate.Count > 0)
            await _context.BulkUpdateAsync(ordersToUpdate.ToList(), cancellationToken: cancellationToken);
        if (detailsToInsert.Count > 0)
            await _context.BulkInsertAsync(detailsToInsert.ToList(), cancellationToken: cancellationToken);
        if (detailsToUpdate.Count > 0)
            await _context.BulkUpdateAsync(detailsToUpdate.ToList(), cancellationToken: cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    // Guardado EF normal para lotes chicos. Las entidades llegan desconectadas (se leyeron con
    // AsNoTracking), asi que se marca el estado de CADA una explicitamente y se apaga
    // AutoDetectChanges: si no, EF recorreria la navegacion Order.Details y adoptaria detalles
    // que no estan en ninguna lista. SaveChanges ya usa una transaccion propia.
    private async Task SaveTrackedAsync(Action<ExplicitTracker> mark, CancellationToken cancellationToken)
    {
        bool autoDetectChanges = _context.ChangeTracker.AutoDetectChangesEnabled;
        _context.ChangeTracker.AutoDetectChangesEnabled = false;
        try
        {
            mark(new ExplicitTracker(_context));
            await _context.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            _context.ChangeTracker.Clear();
            _context.ChangeTracker.AutoDetectChangesEnabled = autoDetectChanges;
        }
    }

    private sealed class ExplicitTracker
    {
        private readonly ReyplastCoreDbContext _context;
        public ExplicitTracker(ReyplastCoreDbContext context) => _context = context;

        public void MarkAll<TEntity>(IEnumerable<TEntity> entities, EntityState state) where TEntity : class
        {
            foreach (TEntity entity in entities) _context.Entry(entity).State = state;
        }

        // "Touch": solo se escriben las columnas indicadas (mismo efecto que PropertiesToInclude).
        public void MarkColumns<TEntity>(IEnumerable<TEntity> entities, IReadOnlyList<string> columns) where TEntity : class
        {
            foreach (TEntity entity in entities)
            {
                Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<TEntity> entry = _context.Entry(entity);
                entry.State = EntityState.Unchanged;
                foreach (string column in columns) entry.Property(column).IsModified = true;
            }
        }
    }
}
