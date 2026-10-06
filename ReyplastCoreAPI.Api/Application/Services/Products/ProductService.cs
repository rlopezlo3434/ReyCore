using ReyplastCoreAPI.Application.DTOs.Product.Request;
using ReyplastCoreAPI.Application.DTOs.Product.Response;
using ReyplastCoreAPI.Application.Interfaces.Repositories;
using ReyplastCoreAPI.Application.Interfaces.Services;
using ReyplastCoreAPI.Domain.Catalog;
using ReyplastCoreAPI.Domain.Catalog.Entities;
using ReyplastCoreAPI.Shared.Exceptions;
using ReyplastCoreAPI.Shared.Helpers;
using ReyplastCoreAPI.Shared.Mappers;

namespace ReyplastCoreAPI.Application.Services.Products;

public sealed class ProductService : IProductService
{
    private readonly IProductRepository _repository;

    public ProductService(IProductRepository repository) => _repository = repository;

    public async Task<PaginatedResponse<ProductResponse>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? search,
        IReadOnlyCollection<string>? productTypes,
        bool? isActive,
        string? sourceSystem)
    {
        ValidationHelper.ThrowIfInvalidPage(pageNumber, pageSize);
        IReadOnlyCollection<string>? normalizedTypes = NormalizeProductTypes(productTypes);
        string? normalizedSource = NormalizeOptionalSourceSystem(sourceSystem);
        PaginatedResponse<Product> page = await _repository.GetPagedAsync(
            pageNumber, pageSize, search, normalizedTypes, isActive, normalizedSource);

        return new PaginatedResponse<ProductResponse>
        {
            Items = page.Items.Select(ProductMapper.ToResponse).ToList(),
            PageNumber = page.PageNumber,
            PageSize = page.PageSize,
            TotalRecords = page.TotalRecords,
            TotalPages = page.TotalPages
        };
    }

    public async Task<ProductResponse> GetByIdAsync(Guid id)
    {
        ValidationHelper.ThrowIfInvalidId(id, "El identificador del producto no es válido.");
        return ProductMapper.ToResponse(await GetRequiredByIdAsync(id, false));
    }

    public async Task<ProductResponse> GetByGenericCodeAsync(string genericCode)
    {
        string normalizedCode = TextHelper.Required(genericCode, "El código del producto es obligatorio.");
        Product? product = await _repository.GetByGenericCodeAsync(normalizedCode);
        if (product is null) throw new AppException("No se encontró el producto.", 404);
        return ProductMapper.ToResponse(product);
    }

    public async Task<ProductResponse> CreateAsync(ProductCreateRequest request)
    {
        ValidationHelper.ThrowIfNull(request, "La solicitud es obligatoria.");
        string type = NormalizeProductType(request.Type);
        string genericCode = TextHelper.Required(request.GenericCode, "El código del producto es obligatorio.");
        ValidateDistinctColorCodes(request.Colors);

        if (await _repository.GenericCodeExistsAsync(genericCode))
            throw new AppException("Ya existe un producto con el mismo código.", 409);

        Product product = new()
        {
            Id = Guid.NewGuid(),
            Type = type,
            GenericCode = genericCode,
            Description = TextHelper.TrimOrNull(request.Description),
            SourceSystem = SourceSystems.Manual,
            IsActive = true
        };

        foreach (ProductColorRequest colorRequest in request.Colors)
            product.Colors.Add(BuildAdministrativeColor(product.Id, colorRequest));

        await _repository.AddAsync(product);
        await _repository.SaveChangesAsync();
        return ProductMapper.ToResponse(product);
    }

    public async Task<ProductResponse> UpdateAsync(Guid id, ProductUpdateRequest request)
    {
        ValidationHelper.ThrowIfInvalidId(id, "El identificador del producto no es válido.");
        ValidationHelper.ThrowIfNull(request, "La solicitud es obligatoria.");
        string type = NormalizeProductType(request.Type);
        string genericCode = TextHelper.Required(request.GenericCode, "El código del producto es obligatorio.");
        ValidateDistinctColorCodes(request.Colors);

        Product product = await GetRequiredByIdAsync(id, true);
        EnsureManualProduct(product);
        if (await _repository.GenericCodeExistsAsync(genericCode, id))
            throw new AppException("Ya existe otro producto con el mismo código.", 409);

        product.Type = type;
        product.GenericCode = genericCode;
        product.Description = TextHelper.TrimOrNull(request.Description);
        ApplyAdministrativeColors(product, request.Colors);
        await _repository.SaveChangesAsync();
        return ProductMapper.ToResponse(product);
    }

    public async Task<StatusChangeResponse> UpdateStatusAsync(Guid id, StatusChangeRequest request)
    {
        ValidationHelper.ThrowIfInvalidId(id, "El identificador del producto no es válido.");
        ValidationHelper.ThrowIfNull(request, "La solicitud de estado es obligatoria.");
        Product product = await GetRequiredByIdAsync(id, true);
        EnsureManualProduct(product);
        product.IsActive = request.IsActive;
        await _repository.SaveChangesAsync();

        return new StatusChangeResponse
        {
            IsActive = product.IsActive,
            Message = product.IsActive ? "Producto habilitado correctamente." : "Producto inhabilitado correctamente."
        };
    }

    private async Task<Product> GetRequiredByIdAsync(Guid id, bool asTracking)
    {
        Product? product = await _repository.GetByIdAsync(id, asTracking);
        return product ?? throw new AppException("No se encontró el producto.", 404);
    }

    private static Color BuildAdministrativeColor(Guid productId, ProductColorRequest request) => new()
    {
        Id = Guid.NewGuid(),
        ProductId = productId,
        Code = TextHelper.Required(request.Code, "El código de color es obligatorio."),
        ColorDescription = TextHelper.TrimOrNull(request.ColorDescription),
        IsSaleEnabled = request.IsSaleEnabled,
        IsActive = true
    };

    private static void ApplyAdministrativeColors(Product product, IReadOnlyCollection<ProductColorRequest> requests)
    {
        HashSet<string> suppliedCodes = new(StringComparer.OrdinalIgnoreCase);
        foreach (ProductColorRequest request in requests)
        {
            string code = TextHelper.Required(request.Code, "El código de color es obligatorio.");
            suppliedCodes.Add(code);
            Color? color = product.Colors.FirstOrDefault(existing =>
                string.Equals(existing.Code, code, StringComparison.OrdinalIgnoreCase));
            if (color is null)
            {
                product.Colors.Add(BuildAdministrativeColor(product.Id, request));
                continue;
            }

            color.ColorDescription = TextHelper.TrimOrNull(request.ColorDescription);
            color.IsSaleEnabled = request.IsSaleEnabled;
            color.IsActive = true;
        }

        foreach (Color color in product.Colors.Where(color => !suppliedCodes.Contains(color.Code)))
            color.IsActive = false;
    }

    private static void ValidateDistinctColorCodes(IReadOnlyCollection<ProductColorRequest>? colors)
    {
        ValidationHelper.ThrowIfNull(colors, "La lista de colores es obligatoria.");
        string[] normalizedCodes = colors!
            .Select(color => TextHelper.Required(color.Code, "El código de color es obligatorio."))
            .ToArray();
        if (normalizedCodes.Contains(CatalogColorCodes.Assorted, StringComparer.OrdinalIgnoreCase))
            throw new AppException(
                $"El código {CatalogColorCodes.Assorted} representa SURTIDO y no es un color almacenable.");

        bool hasDuplicates = normalizedCodes
            .GroupBy(code => code, StringComparer.OrdinalIgnoreCase)
            .Any(group => group.Count() > 1);
        if (hasDuplicates) throw new AppException("Un producto no puede repetir el mismo código de color.");
    }



    private static string NormalizeProductType(string? value)
    {
        string normalized = TextHelper.Required(value, "El tipo de producto es obligatorio.").ToUpperInvariant();
        if (!ProductTypes.All.Contains(normalized))
            throw new AppException("El tipo de producto no es válido.");
        return normalized;
    }

    private static IReadOnlyCollection<string>? NormalizeProductTypes(
        IReadOnlyCollection<string>? values)
    {
        if (values is null || values.Count == 0) return null;

        string[] normalized = values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(NormalizeProductType)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return normalized.Length == 0 ? null : normalized;
    }

    private static string? NormalizeOptionalSourceSystem(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        string normalized = value.Trim().ToUpperInvariant();
        if (!SourceSystems.All.Contains(normalized))
            throw new AppException("El sistema de origen no es válido.");
        return normalized;
    }

    private static void EnsureManualProduct(Product product)
    {
        if (!string.Equals(product.SourceSystem, SourceSystems.Manual, StringComparison.OrdinalIgnoreCase))
            throw new AppException(
                "Solo se pueden modificar productos creados manualmente. Los productos sincronizados deben cambiarse en su sistema de origen.",
                409);
    }
}
