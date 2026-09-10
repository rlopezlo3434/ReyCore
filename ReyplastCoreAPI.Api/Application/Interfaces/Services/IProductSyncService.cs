using ReyplastCoreAPI.Application.DTOs.Product.Response;

namespace ReyplastCoreAPI.Application.Interfaces.Services;

/// <summary>Orquesta sincronizaciones del catálogo desde sistemas externos.</summary>
public interface IProductSyncService
{
    Task<ProductSyncAcceptedResponse> QueueOsisAsync(CancellationToken cancellationToken = default);

    Task ExecuteOsisAsync(Guid executionId, CancellationToken cancellationToken = default);

    Task<PaginatedResponse<SyncExecutionResponse>> GetExecutionsAsync(
        int pageNumber,
        int pageSize,
        string? sourceSystem,
        CancellationToken cancellationToken = default);

    Task<SyncExecutionResponse> GetExecutionAsync(
        Guid executionId,
        CancellationToken cancellationToken = default);

    Task<SyncExecutionResponse> CancelExecutionAsync(
        Guid executionId,
        CancellationToken cancellationToken = default);
}
