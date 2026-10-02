using ReyplastCoreAPI.Domain.Catalog.Entities;

namespace ReyplastCoreAPI.Application.Interfaces.Repositories;

/// <summary>Persiste lotes de productos y colores durante una sincronización.</summary>
public interface IProductSyncRepository
{
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
}
