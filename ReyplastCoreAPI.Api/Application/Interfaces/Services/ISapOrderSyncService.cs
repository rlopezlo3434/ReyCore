using ReyplastCoreAPI.Application.DTOs.Order;

namespace ReyplastCoreAPI.Application.Interfaces.Services;

/// <summary>Orquesta exclusivamente la importación de pedidos del API SAP.</summary>
public interface ISapOrderSyncService
{
    Task<OrderDocumentSyncResult> SyncByDocNumAsync(string docNum, CancellationToken cancellationToken = default);
    Task<OrderSyncAcceptedResponse> QueueRangeAsync(DateTime dateFrom, DateTime dateTo,
        CancellationToken cancellationToken = default);
    /// <summary>Devuelve el resumen para notificar, o null si la ejecución fue cancelada.</summary>
    Task<OrdersSynchronizedEvent?> ExecuteRangeAsync(Guid executionId, DateTime dateFrom, DateTime dateTo,
        CancellationToken cancellationToken = default);
}
