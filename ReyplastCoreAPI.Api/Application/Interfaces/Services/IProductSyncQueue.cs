namespace ReyplastCoreAPI.Application.Interfaces.Services;

/// <summary>Encola sincronizaciones de productos en Hangfire.</summary>
public interface IProductSyncQueue
{
    /// <summary>Encola una ejecución OSIS. Devuelve el id del job, o null si no se pudo encolar.</summary>
    Task<string?> EnqueueOsisAsync(Guid executionId, CancellationToken cancellationToken);
}
