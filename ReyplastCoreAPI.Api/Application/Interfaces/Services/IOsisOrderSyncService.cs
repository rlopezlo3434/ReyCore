using ReyplastCoreAPI.Application.DTOs.Order;

namespace ReyplastCoreAPI.Application.Interfaces.Services;

/// <summary>
/// Orquesta exclusivamente la lectura y sincronización de pedidos provenientes de OSIS/XIMESA.
/// </summary>
public interface IOsisOrderSyncService
{
    Task<OrderDocumentSyncResult> SyncByDocNumAsync(string docNum, CancellationToken cancellationToken = default);
    Task<OrderSyncAcceptedResponse> QueueRangeAsync(DateTime dateFrom, DateTime dateTo,
        CancellationToken cancellationToken = default);
    /// <summary>Devuelve el resumen para notificar, o null si la ejecución fue cancelada.</summary>
    Task<OrdersSynchronizedEvent?> ExecuteRangeAsync(Guid executionId, DateTime dateFrom, DateTime dateTo,
        CancellationToken cancellationToken = default);
    /// <summary>
    /// Sincroniza solo los pedidos aprobados del día <paramref name="date"/>. Devuelve el resumen para
    /// notificar, o null si fue cancelada.
    /// </summary>
    Task<OrdersSynchronizedEvent?> ExecuteApprovedDayAsync(Guid executionId, DateTime date,
        CancellationToken cancellationToken = default);

}
