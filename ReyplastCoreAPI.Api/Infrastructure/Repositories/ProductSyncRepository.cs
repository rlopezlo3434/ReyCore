using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using ReyplastCoreAPI.Application.DTOs.Product.Response;
using ReyplastCoreAPI.Application.Interfaces.Repositories;
using ReyplastCoreAPI.Domain.Catalog.Entities;
using ReyplastCoreAPI.Infrastructure.Persistence;

namespace ReyplastCoreAPI.Infrastructure.Repositories;

/// <summary>Implementa persistencia masiva y trazabilidad para sincronizaciones del catálogo.</summary>
public sealed class ProductSyncRepository : IProductSyncRepository
{
    private readonly ReyplastCoreDbContext _context;

    public ProductSyncRepository(ReyplastCoreDbContext context) => _context = context;

    public async Task<PaginatedResponse<SyncExecution>> GetExecutionsAsync(
        int pageNumber,
        int pageSize,
        string? sourceSystem,
        string? resourceType,
        CancellationToken cancellationToken)
    {
        IQueryable<SyncExecution> query = _context.SyncExecutions.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(sourceSystem))
            query = query.Where(execution => execution.SourceSystem == sourceSystem);
        if (!string.IsNullOrWhiteSpace(resourceType))
            query = query.Where(execution => execution.ResourceType == resourceType);
        int totalRecords = await query.CountAsync(cancellationToken);
        List<SyncExecution> items = await query
            .OrderByDescending(execution => execution.StartedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new PaginatedResponse<SyncExecution>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalRecords = totalRecords,
            TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize)
        };
    }

    public async Task CreateExecutionAsync(
        SyncExecution execution,
        CancellationToken cancellationToken)
    {
        _context.SyncExecutions.Add(execution);
        await _context.SaveChangesAsync(cancellationToken);
        _context.Entry(execution).State = EntityState.Detached;
    }

    public Task<SyncExecution?> GetExecutionAsync(
        Guid executionId,
        CancellationToken cancellationToken) =>
        _context.SyncExecutions
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == executionId, cancellationToken);

    public async Task<SyncExecution> MarkRunningAsync(
        Guid executionId,
        DateTime startedAt,
        CancellationToken cancellationToken)
    {
        _context.ChangeTracker.Clear();
        SyncExecution execution = await _context.SyncExecutions
            .SingleAsync(item => item.Id == executionId, cancellationToken);
        execution.StartedAt = startedAt;
        execution.Status = SyncStatuses.Running;
        execution.CompletedAt = null;
        execution.ErrorMessage = null;
        await _context.SaveChangesAsync(cancellationToken);
        _context.Entry(execution).State = EntityState.Detached;
        return execution;
    }

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

        _context.SyncExecutions.Update(execution);
        await _context.SaveChangesAsync(cancellationToken);
        _context.Entry(execution).State = EntityState.Detached;
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
        _context.SyncExecutions.Update(execution);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(
        Guid executionId,
        string errorMessage,
        CancellationToken cancellationToken)
    {
        _context.ChangeTracker.Clear();
        SyncExecution? execution = await _context.SyncExecutions
            .SingleOrDefaultAsync(item => item.Id == executionId, cancellationToken);
        if (execution is null) return;

        execution.Status = SyncStatuses.Failed;
        execution.CompletedAt = DateTime.UtcNow.AddHours(-5);
        execution.ErrorMessage = errorMessage.Length <= 2000 ? errorMessage : errorMessage[..2000];
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkCancelledAsync(
        Guid executionId,
        string message,
        CancellationToken cancellationToken)
    {
        _context.ChangeTracker.Clear();
        SyncExecution? execution = await _context.SyncExecutions
            .SingleOrDefaultAsync(item => item.Id == executionId, cancellationToken);
        if (execution is null || execution.Status is SyncStatuses.Completed or SyncStatuses.Failed)
            return;

        execution.Status = SyncStatuses.Cancelled;
        execution.CompletedAt = DateTime.UtcNow.AddHours(-5);
        execution.ErrorMessage = message;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
