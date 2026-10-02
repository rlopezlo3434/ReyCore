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
    Task<CompleteOrderResponse> GetCompleteByDocNumAsync(string docNum, string? sourceSystem,
        CancellationToken cancellationToken = default);
    /// <summary>Version batch de GetCompleteByDocNumAsync. A diferencia de la version individual,
    /// un docNum no encontrado o ambiguo (mas de un origen/sucursal) simplemente se omite del
    /// resultado en vez de lanzar 404/409 -- el llamador ya sabe manejar "no vino" por documento
    /// individualmente, y un solo docNum con problemas no debe tumbar el batch completo.</summary>
    Task<List<CompleteOrderResponse>> GetCompleteByDocNumsAsync(IReadOnlyCollection<string> docNums,
        string? sourceSystem, CancellationToken cancellationToken = default);
    Task<PaginatedResponse<OrderDetailResponse>> GetDetailsAsync(Guid id, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>Cantidad de pedidos por canal, con los mismos filtros que GetPagedAsync salvo
    /// channelCode -- para poblar un selector de canal con su conteo real.</summary>
    Task<List<OrderChannelCountResponse>> GetChannelCountsAsync(
        IReadOnlyCollection<string>? search, IReadOnlyCollection<string>? situationCode,
        DateTime? dateFrom, DateTime? dateTo, IReadOnlyCollection<string>? sourceSystem,
        CancellationToken cancellationToken = default);
}
