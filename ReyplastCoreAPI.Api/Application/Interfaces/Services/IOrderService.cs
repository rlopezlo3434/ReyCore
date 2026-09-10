using ReyplastCoreAPI.Application.DTOs.Order;
using ReyplastCoreAPI.Application.DTOs.Product.Response;

namespace ReyplastCoreAPI.Application.Interfaces.Services;

public interface IOrderService
{
    Task<PaginatedResponse<OrderResponse>> GetPagedAsync(int pageNumber, int pageSize,
        IReadOnlyCollection<string>? search, IReadOnlyCollection<string>? situationCode,
        IReadOnlyCollection<string>? channelCode, DateTime? dateFrom, DateTime? dateTo,
        IReadOnlyCollection<string>? sourceSystem, CancellationToken cancellationToken = default);
    Task<OrderResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CompleteOrderResponse> GetCompleteByDocNumAsync(string docNum,
        CancellationToken cancellationToken = default);
    Task<PaginatedResponse<OrderDetailResponse>> GetDetailsAsync(Guid id, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default);
}
