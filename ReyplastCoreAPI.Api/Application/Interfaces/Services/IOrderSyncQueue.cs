namespace ReyplastCoreAPI.Application.Interfaces.Services;

/// <summary>Encola sincronizaciones de pedidos en Hangfire. Cada método devuelve el id del job,
/// o null si no se pudo encolar.</summary>
public interface IOrderSyncQueue
{
    Task<string?> EnqueueOsisRangeAsync(Guid executionId, DateTime dateFrom, DateTime dateTo, CancellationToken cancellationToken);
    Task<string?> EnqueueSapRangeAsync(Guid executionId, DateTime dateFrom, DateTime dateTo, CancellationToken cancellationToken);
}
