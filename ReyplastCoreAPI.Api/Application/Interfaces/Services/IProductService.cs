using ReyplastCoreAPI.Application.DTOs.Product.Request;
using ReyplastCoreAPI.Application.DTOs.Product.Response;

namespace ReyplastCoreAPI.Application.Interfaces.Services;

public interface IProductService
{
    Task<PaginatedResponse<ProductResponse>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? search,
        IReadOnlyCollection<string>? productTypes,
        bool? isActive,
        string? sourceSystem);
    Task<ProductResponse> GetByIdAsync(Guid id);
    Task<ProductResponse> GetByGenericCodeAsync(string genericCode);
    Task<ProductResponse> CreateAsync(ProductCreateRequest request);
    Task<ProductResponse> UpdateAsync(Guid id, ProductUpdateRequest request);
    Task<StatusChangeResponse> UpdateStatusAsync(Guid id, StatusChangeRequest request);
}
