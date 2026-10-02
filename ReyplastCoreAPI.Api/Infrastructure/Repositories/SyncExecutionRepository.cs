using ReyplastCoreAPI.Application.Interfaces.Repositories;
using ReyplastCoreAPI.Domain.Catalog;
using ReyplastCoreAPI.Domain.Catalog.Entities;

namespace ReyplastCoreAPI.Infrastructure.Repositories;

/// <summary>
/// Sin estado a propósito: no guarda ejecuciones en memoria ni en BD. Hangfire es la fuente de
/// verdad de ejecución, reintentos, errores y cancelación. Antes esto era un diccionario estático
/// que agregaba una entrada por sincronización y nunca la quitaba (crecía hasta reiniciar el API)
/// -- y nadie leía lo que guardaba. El SyncExecution que devuelve MarkRunningAsync solo vive
/// mientras corre el job, para acumular sus contadores.
/// </summary>
public sealed class SyncExecutionRepository : ISyncExecutionRepository
{
    public Task CreateAsync(SyncExecution execution, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<SyncExecution> MarkRunningAsync(Guid executionId, DateTime startedAt,
        CancellationToken cancellationToken) =>
        Task.FromResult(new SyncExecution { Id = executionId, StartedAt = startedAt, Status = SyncStatuses.Running });

    public Task MarkFailedAsync(Guid executionId, string errorMessage, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task MarkCancelledAsync(Guid executionId, string message, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
