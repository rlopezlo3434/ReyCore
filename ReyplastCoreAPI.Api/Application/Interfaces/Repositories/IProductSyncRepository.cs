using ReyplastCoreAPI.Application.DTOs.Product.Response;
using ReyplastCoreAPI.Domain.Catalog.Entities;

namespace ReyplastCoreAPI.Application.Interfaces.Repositories;

/// <summary>Persiste lotes y trazabilidad de sincronizaciones de productos.</summary>
public interface IProductSyncRepository
{
    Task<PaginatedResponse<SyncExecution>> GetExecutionsAsync(
        int pageNumber,
        int pageSize,
        string? sourceSystem,
        string? resourceType,
        CancellationToken cancellationToken);

    Task CreateExecutionAsync(SyncExecution execution, CancellationToken cancellationToken);

    Task<SyncExecution?> GetExecutionAsync(Guid executionId, CancellationToken cancellationToken);

    Task<SyncExecution> MarkRunningAsync(Guid executionId, DateTime startedAt, CancellationToken cancellationToken);

    Task DeleteExcludedColorsAsync(
        IReadOnlyCollection<string> colorCodes,
        CancellationToken cancellationToken);

    Task PersistPageAsync(
        IReadOnlyCollection<Product> productsToInsert,
        IReadOnlyCollection<Product> productsToUpdate,
        IReadOnlyCollection<Color> colorsToInsert,
        IReadOnlyCollection<Color> colorsToUpdate,
        SyncExecution execution,
        CancellationToken cancellationToken);

    Task DeactivateMissingAsync(
        Guid executionId,
        DateTime changedAt,
        SyncExecution execution,
        CancellationToken cancellationToken);

    Task MarkFailedAsync(Guid executionId, string errorMessage, CancellationToken cancellationToken);

    Task MarkCancelledAsync(Guid executionId, string message, CancellationToken cancellationToken);
}
