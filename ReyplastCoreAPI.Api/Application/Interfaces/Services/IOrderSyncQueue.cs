namespace ReyplastCoreAPI.Application.Interfaces.Services;

/// <summary>Encola sincronizaciones de pedidos en Hangfire. Si ya hay un job en cola, programado
/// (reintento) o corriendo para el mismo origen y rango, devuelve ese mismo job en vez de encolar otro.
/// Devuelve null si no se pudo encolar.</summary>
public interface IOrderSyncQueue
{
    Task<OrderSyncEnqueueResult?> EnqueueOsisRangeAsync(DateTime dateFrom, DateTime dateTo, CancellationToken cancellationToken);
    Task<OrderSyncEnqueueResult?> EnqueueSapRangeAsync(DateTime dateFrom, DateTime dateTo, CancellationToken cancellationToken);
}

/// <summary>Job de Hangfire que atiende el rango; <see cref="Reused"/> = ya existía uno para ese rango.</summary>
public sealed record OrderSyncEnqueueResult(string JobId, Guid ExecutionId, string Status, bool Reused);
