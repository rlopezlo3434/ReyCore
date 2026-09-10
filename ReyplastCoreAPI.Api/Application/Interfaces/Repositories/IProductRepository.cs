using ReyplastCoreAPI.Application.DTOs.Product.Response;
using ReyplastCoreAPI.Domain.Catalog.Entities;

namespace ReyplastCoreAPI.Application.Interfaces.Repositories;

public interface IProductRepository
{
    Task<PaginatedResponse<Product>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? search,
        IReadOnlyCollection<string>? productTypes,
        bool? isActive,
        string? sourceSystem);
    Task<Product?> GetByIdAsync(Guid id, bool asTracking = false);
    Task<Product?> GetByGenericCodeAsync(string genericCode, bool asTracking = false);
    Task<List<Product>> GetByGenericCodesAsync(IReadOnlyCollection<string> genericCodes);
    Task<bool> GenericCodeExistsAsync(string genericCode, Guid? excludingId = null);
    Task AddAsync(Product product);
    Task SaveChangesAsync();
}
