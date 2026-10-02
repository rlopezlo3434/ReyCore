using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using ReyplastCoreAPI.Application.Interfaces.Repositories;
using ReyplastCoreAPI.Domain.Catalog.Entities;
using ReyplastCoreAPI.Infrastructure.Persistence;

namespace ReyplastCoreAPI.Infrastructure.Repositories;

/// <summary>Implementa persistencia masiva y trazabilidad para sincronizaciones del catálogo.</summary>
public sealed class ProductSyncRepository : IProductSyncRepository
{
    private readonly ReyplastCoreDbContext _context;

    public ProductSyncRepository(ReyplastCoreDbContext context) => _context = context;

    public async Task DeleteExcludedColorsAsync(
        IReadOnlyCollection<string> colorCodes,
        CancellationToken cancellationToken)
    {
        if (colorCodes.Count == 0) return;

        List<Color> excludedColors = await _context.Colors
            .AsNoTracking()
            .Where(color => colorCodes.Contains(color.Code))
            .ToListAsync(cancellationToken);
        if (excludedColors.Count == 0) return;

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        await _context.BulkDeleteAsync(excludedColors, cancellationToken: cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task PersistPageAsync(
        IReadOnlyCollection<Product> productsToInsert,
        IReadOnlyCollection<Product> productsToUpdate,
        IReadOnlyCollection<Color> colorsToInsert,
        IReadOnlyCollection<Color> colorsToUpdate,
        SyncExecution execution,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        if (productsToInsert.Count > 0)
            await _context.BulkInsertAsync(productsToInsert.ToList(), cancellationToken: cancellationToken);
        if (productsToUpdate.Count > 0)
            await _context.BulkUpdateAsync(productsToUpdate.ToList(), cancellationToken: cancellationToken);
        if (colorsToInsert.Count > 0)
            await _context.BulkInsertAsync(colorsToInsert.ToList(), cancellationToken: cancellationToken);
        if (colorsToUpdate.Count > 0)
            await _context.BulkUpdateAsync(colorsToUpdate.ToList(), cancellationToken: cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task DeactivateMissingAsync(
        Guid executionId,
        DateTime changedAt,
        SyncExecution execution,
        CancellationToken cancellationToken)
    {
        List<Product> missingProducts = await _context.Products
            .AsNoTracking()
            .Where(product => product.SyncExecutionId != null
                              && product.SyncExecutionId != executionId
                              && product.SourceSystem == execution.SourceSystem
                              && product.IsActive)
            .ToListAsync(cancellationToken);
        foreach (Product product in missingProducts)
        {
            product.IsActive = false;
            product.LastSynchronizedAt = changedAt;
            product.ModifiedAt = changedAt;
        }

        List<Color> missingColors = await _context.Colors
            .AsNoTracking()
            .Where(color => color.SyncExecutionId != null
                            && color.SyncExecutionId != executionId
                            && color.Product.SourceSystem == execution.SourceSystem
                            && color.IsActive)
            .ToListAsync(cancellationToken);
        foreach (Color color in missingColors)
        {
            color.IsActive = false;
            color.LastSynchronizedAt = changedAt;
            color.ModifiedAt = changedAt;
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        if (missingProducts.Count > 0)
            await _context.BulkUpdateAsync(missingProducts, cancellationToken: cancellationToken);
        if (missingColors.Count > 0)
            await _context.BulkUpdateAsync(missingColors, cancellationToken: cancellationToken);

        execution.Status = SyncStatuses.Completed;
        execution.CompletedAt = changedAt;
        await transaction.CommitAsync(cancellationToken);
    }

}
