using ReyplastCoreAPI.Application.DTOs.Order;
using ReyplastCoreAPI.Application.DTOs.Product.Response;

namespace ReyplastCoreAPI.Application.Interfaces.Services;

public interface IOrderSyncService
{
    Task<bool> SyncOsisByDocNumAsync(string docNum, CancellationToken cancellationToken = default);
    Task<OrderSyncAcceptedResponse> QueueOsisAsync(CancellationToken cancellationToken = default);
    Task ExecuteOsisAsync(Guid executionId, CancellationToken cancellationToken = default);
    Task<PaginatedResponse<SyncExecutionResponse>> GetExecutionsAsync(int pageNumber, int pageSize,
        string? sourceSystem, CancellationToken cancellationToken = default);
    Task<SyncExecutionResponse> GetExecutionAsync(Guid executionId, CancellationToken cancellationToken = default);
    Task<SyncExecutionResponse> CancelExecutionAsync(Guid executionId, CancellationToken cancellationToken = default);
}
