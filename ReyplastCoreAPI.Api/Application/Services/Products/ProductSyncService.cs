using ReyplastCoreAPI.Application.DTOs.Integrations.Osis;
using ReyplastCoreAPI.Application.DTOs.Product.Response;
using ReyplastCoreAPI.Application.Interfaces.Integrations;
using ReyplastCoreAPI.Application.Interfaces.Repositories;
using ReyplastCoreAPI.Application.Interfaces.Services;
using ReyplastCoreAPI.Domain.Catalog;
using ReyplastCoreAPI.Domain.Catalog.Entities;
using ReyplastCoreAPI.Shared.Exceptions;
using ReyplastCoreAPI.Shared.Helpers;
using ReyplastCoreAPI.Shared.Mappers;

namespace ReyplastCoreAPI.Application.Services.Products;

/// <summary>Sincroniza el catálogo local desde conectores externos y registra cada ejecución.</summary>
public sealed class ProductSyncService : IProductSyncService
{
    private readonly IProductRepository _productRepository;
    private readonly IProductSyncRepository _syncRepository;
    private readonly IOsisCatalogReader _catalogReader;
    private readonly IProductSyncQueue _syncQueue;
    private readonly ProductSynchronizationLock _synchronizationLock;

    public ProductSyncService(
        IProductRepository productRepository,
        IProductSyncRepository syncRepository,
        IOsisCatalogReader catalogReader,
        IProductSyncQueue syncQueue,
        ProductSynchronizationLock synchronizationLock)
    {
        _productRepository = productRepository;
        _syncRepository = syncRepository;
        _catalogReader = catalogReader;
        _syncQueue = syncQueue;
        _synchronizationLock = synchronizationLock;
    }

    /// <summary>Registra y encola una sincronización OSIS sin ligarla a la conexión HTTP.</summary>
    public async Task<ProductSyncAcceptedResponse> QueueOsisAsync(
        CancellationToken cancellationToken = default)
    {
        SyncExecution execution = new()
        {
            Id = Guid.NewGuid(),
            SourceSystem = SourceSystems.Osis,
            ResourceType = SyncResourceTypes.Products,
            StartedAt = CurrentTime(),
            Status = SyncStatuses.Queued
        };

        await _syncRepository.CreateExecutionAsync(execution, cancellationToken);
        if (!_syncQueue.TryEnqueueOsis(execution.Id))
        {
            await _syncRepository.MarkFailedAsync(
                execution.Id, "No fue posible agregar la ejecución a la cola.", CancellationToken.None);
            throw new AppException("No fue posible agregar la sincronización a la cola.", 503);
        }

        return new ProductSyncAcceptedResponse
        {
            ExecutionId = execution.Id,
            Status = execution.Status,
            StatusUrl = $"/api/products/sync/executions/{execution.Id}"
        };
    }

    /// <summary>Ejecuta desde el worker la lectura y persistencia de XIMESA/OSIS.</summary>
    public async Task ExecuteOsisAsync(
        Guid executionId,
        CancellationToken cancellationToken = default)
    {
        bool lockAcquired = false;
        SyncExecution? execution = null;
        ProductSyncResponse response = new()
        {
            ExecutionId = executionId,
            Status = SyncStatuses.Queued
        };

        try
        {
            await _synchronizationLock.WaitAsync(cancellationToken);
            lockAcquired = true;
            execution = await _syncRepository.MarkRunningAsync(
                executionId, CurrentTime(), cancellationToken);
            response.Status = SyncStatuses.Running;
            await _syncRepository.DeleteExcludedColorsAsync(
                new[] { CatalogColorCodes.Assorted }, cancellationToken);
            string? afterProductCode = null;

            while (true)
            {
                OsisCatalogPage page = await _catalogReader.ReadPageAsync(afterProductCode, cancellationToken);
                if (page.Products.Count == 0)
                {
                    if (response.PagesProcessed == 0)
                        throw new AppException("El catálogo OSIS no contiene productos.");
                    break;
                }

                response.Processed += page.Products.Count;
                response.PagesProcessed++;
                execution.RecordsRead = response.Processed;
                execution.LastCursor = page.NextProductCode;
                await PersistPageAsync(page, execution, response, cancellationToken);

                if (!page.HasMore) break;
                afterProductCode = page.NextProductCode
                    ?? throw new AppException("OSIS no devolvió el cursor de la siguiente página.");
            }

            await _syncRepository.DeactivateMissingAsync(
                execution.Id, CurrentTime(), execution, cancellationToken);
            response.Status = SyncStatuses.Completed;
        }
        catch (OperationCanceledException)
        {
            await _syncRepository.MarkCancelledAsync(
                executionId, "La sincronización fue cancelada.", CancellationToken.None);
        }
        catch (Exception exception)
        {
            try
            {
                await _syncRepository.MarkFailedAsync(
                    executionId, exception.Message, CancellationToken.None);
            }
            catch
            {
                // Se conserva la excepción original de la sincronización.
            }

            throw;
        }
        finally
        {
            if (lockAcquired) _synchronizationLock.Release();
        }
    }

    /// <summary>Consulta el historial general, opcionalmente filtrado por sistema de origen.</summary>
    public async Task<PaginatedResponse<SyncExecutionResponse>> GetExecutionsAsync(
        int pageNumber,
        int pageSize,
        string? sourceSystem,
        CancellationToken cancellationToken = default)
    {
        ValidationHelper.ThrowIfInvalidPage(pageNumber, pageSize);
        string? normalizedSource = NormalizeOptionalSourceSystem(sourceSystem);
        PaginatedResponse<SyncExecution> page = await _syncRepository.GetExecutionsAsync(
            pageNumber, pageSize, normalizedSource, SyncResourceTypes.Products, cancellationToken);
        return new PaginatedResponse<SyncExecutionResponse>
        {
            Items = page.Items.Select(ProductMapper.ToResponse).ToList(),
            PageNumber = page.PageNumber,
            PageSize = page.PageSize,
            TotalRecords = page.TotalRecords,
            TotalPages = page.TotalPages
        };
    }

    public async Task<SyncExecutionResponse> GetExecutionAsync(
        Guid executionId,
        CancellationToken cancellationToken = default)
    {
        ValidationHelper.ThrowIfInvalidId(executionId, "El identificador de ejecución no es válido.");
        SyncExecution? execution = await _syncRepository.GetExecutionAsync(executionId, cancellationToken);
        if (execution is null || execution.ResourceType != SyncResourceTypes.Products)
            throw new AppException("No se encontró la ejecución de productos.", 404);
        return ProductMapper.ToResponse(execution);
    }

    public async Task<SyncExecutionResponse> CancelExecutionAsync(
        Guid executionId,
        CancellationToken cancellationToken = default)
    {
        SyncExecutionResponse execution = await GetExecutionAsync(executionId, cancellationToken);
        if (execution.Status is SyncStatuses.Completed or SyncStatuses.Failed or SyncStatuses.Cancelled)
            throw new AppException("La ejecución ya terminó y no puede cancelarse.", 409);
        if (!_syncQueue.TryCancel(executionId))
            throw new AppException("La ejecución ya no se encuentra activa en esta instancia.", 409);

        return await GetExecutionAsync(executionId, cancellationToken);
    }

    private async Task PersistPageAsync(
        OsisCatalogPage page,
        SyncExecution execution,
        ProductSyncResponse response,
        CancellationToken cancellationToken)
    {
        string[] productCodes = page.Products.Select(product => product.ProductCode).ToArray();
        List<Product> existingProducts = await _productRepository.GetByGenericCodesAsync(productCodes);
        Dictionary<string, Product> productsByCode = existingProducts
            .ToDictionary(product => product.GenericCode, StringComparer.OrdinalIgnoreCase);
        List<Product> productsToInsert = new();
        List<Product> productsToUpdate = new();
        List<Color> colorsToInsert = new();
        List<Color> colorsToUpdate = new();
        DateTime synchronizedAt = CurrentTime();

        foreach (OsisProductRecord source in page.Products)
        {
            bool isNew = !productsByCode.TryGetValue(source.ProductCode, out Product? product);
            product ??= new Product
            {
                Id = Guid.NewGuid(),
                CreatedAt = source.CreatedAt ?? synchronizedAt
            };
            bool productChanged = isNew || HasProductChanged(product, source);

            ApplyProductValues(product, source, execution.Id, synchronizedAt, isNew, productChanged);
            if (isNew)
            {
                productsToInsert.Add(product);
                response.ProductsCreated++;
            }
            else
            {
                productsToUpdate.Add(product);
                if (productChanged) response.ProductsChanged++;
                else response.ProductsUnchanged++;
            }

            CollectColors(
                product, source.Colors, execution.Id, synchronizedAt,
                colorsToInsert, colorsToUpdate, response);
        }

        CopyCounters(response, execution);
        await _syncRepository.PersistPageAsync(
            productsToInsert, productsToUpdate, colorsToInsert, colorsToUpdate,
            execution, cancellationToken);
    }

    private static void ApplyProductValues(
        Product product,
        OsisProductRecord source,
        Guid executionId,
        DateTime synchronizedAt,
        bool isNew,
        bool productChanged)
    {
        product.Type = source.ProductType;
        product.GenericCode = source.ProductCode;
        product.Description = NormalizeOptional(source.ProductDescription);
        product.SourceSystem = SourceSystems.Osis;
        product.IsActive = source.IsActive;
        if (source.CreatedAt.HasValue) product.CreatedAt = source.CreatedAt.Value;
        product.SyncExecutionId = executionId;
        product.LastSynchronizedAt = synchronizedAt;
        if (!productChanged) return;

        if (!isNew) product.ModifiedAt = synchronizedAt;
    }

    private static void CollectColors(
        Product product,
        IReadOnlyList<OsisColorRecord> sourceColors,
        Guid executionId,
        DateTime synchronizedAt,
        ICollection<Color> colorsToInsert,
        ICollection<Color> colorsToUpdate,
        ProductSyncResponse response)
    {
        Dictionary<string, Color> colorsByCode = product.Colors
            .ToDictionary(color => color.Code, StringComparer.OrdinalIgnoreCase);

        foreach (OsisColorRecord source in sourceColors)
        {
            bool isNew = !colorsByCode.TryGetValue(source.ColorCode, out Color? color);
            color ??= new Color
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Code = source.ColorCode,
                CreatedAt = synchronizedAt
            };
            bool colorChanged = isNew || HasColorChanged(color, source);

            ApplyColorValues(color, source, executionId, synchronizedAt, isNew, colorChanged);
            if (isNew)
            {
                colorsToInsert.Add(color);
                response.ColorsCreated++;
            }
            else
            {
                colorsToUpdate.Add(color);
                if (colorChanged) response.ColorsChanged++;
                else response.ColorsUnchanged++;
            }
        }
    }

    private static void ApplyColorValues(
        Color color,
        OsisColorRecord source,
        Guid executionId,
        DateTime synchronizedAt,
        bool isNew,
        bool colorChanged)
    {
        color.ColorDescription = NormalizeOptional(source.ColorDescription);
        color.IsSaleEnabled = source.IsSaleEnabled;
        color.IsActive = source.IsActive;
        color.SyncExecutionId = executionId;
        color.LastSynchronizedAt = synchronizedAt;
        if (!colorChanged) return;

        if (!isNew) color.ModifiedAt = synchronizedAt;
    }

    private static bool HasProductChanged(Product product, OsisProductRecord source) =>
        !string.Equals(product.SourceSystem, SourceSystems.Osis, StringComparison.Ordinal)
        || !string.Equals(product.Type, source.ProductType, StringComparison.Ordinal)
        || !string.Equals(product.Description, NormalizeOptional(source.ProductDescription), StringComparison.Ordinal)
        || product.IsActive != source.IsActive
        || (source.CreatedAt.HasValue && product.CreatedAt != source.CreatedAt.Value);

    private static bool HasColorChanged(Color color, OsisColorRecord source) =>
        !string.Equals(color.ColorDescription, NormalizeOptional(source.ColorDescription), StringComparison.Ordinal)
        || color.IsSaleEnabled != source.IsSaleEnabled
        || color.IsActive != source.IsActive;

    private static void CopyCounters(ProductSyncResponse response, SyncExecution execution)
    {
        execution.RecordsCreated = response.ProductsCreated;
        execution.RecordsChanged = response.ProductsChanged;
        execution.RecordsUnchanged = response.ProductsUnchanged;
        execution.ChildRecordsCreated = response.ColorsCreated;
        execution.ChildRecordsChanged = response.ColorsChanged;
        execution.ChildRecordsUnchanged = response.ColorsUnchanged;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeOptionalSourceSystem(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        string normalized = value.Trim().ToUpperInvariant();
        if (!SourceSystems.All.Contains(normalized))
            throw new AppException("El sistema de origen no es válido.");
        return normalized;
    }

    private static DateTime CurrentTime() => DateTime.UtcNow.AddHours(-5);
}
