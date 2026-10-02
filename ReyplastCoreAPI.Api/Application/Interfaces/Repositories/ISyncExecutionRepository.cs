using ReyplastCoreAPI.Domain.Catalog.Entities;

namespace ReyplastCoreAPI.Application.Interfaces.Repositories;

/// <summary>Estado transitorio de una ejecución mientras corre su job. No es historial: Hangfire es
/// la fuente de verdad de ejecución, reintentos y errores.</summary>
public interface ISyncExecutionRepository
{
    Task CreateAsync(SyncExecution execution, CancellationToken cancellationToken);
    Task<SyncExecution> MarkRunningAsync(Guid executionId, DateTime startedAt,
        CancellationToken cancellationToken);
    Task MarkFailedAsync(Guid executionId, string errorMessage, CancellationToken cancellationToken);
    Task MarkCancelledAsync(Guid executionId, string message, CancellationToken cancellationToken);
}
