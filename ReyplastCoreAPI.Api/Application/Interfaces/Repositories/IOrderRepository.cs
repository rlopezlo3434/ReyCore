using ReyplastCoreAPI.Application.DTOs.Product.Response;
using ReyplastCoreAPI.Domain.Orders.Entities;

namespace ReyplastCoreAPI.Application.Interfaces.Repositories;

public interface IOrderRepository
{
    Task<PaginatedResponse<SalesOrder>> GetPagedAsync(int pageNumber, int pageSize,
        IReadOnlyCollection<string> search, IReadOnlyCollection<string> situationCodes,
        IReadOnlyCollection<string> channelCodes, DateTime? dateFrom, DateTime? dateTo,
        IReadOnlyCollection<string> sourceSystems, CancellationToken cancellationToken);
    Task<SalesOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<SalesOrder>> GetCompleteByDocNumAsync(string docNum,
        CancellationToken cancellationToken);
    Task<PaginatedResponse<SalesOrderDetail>> GetDetailsAsync(Guid orderId, int pageNumber, int pageSize,
        CancellationToken cancellationToken);
    Task<List<SalesOrder>> GetByKeysAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken);
}
