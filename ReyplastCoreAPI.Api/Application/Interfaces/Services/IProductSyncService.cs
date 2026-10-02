using ReyplastCoreAPI.Application.DTOs.Product.Response;

namespace ReyplastCoreAPI.Application.Interfaces.Services;

/// <summary>Orquesta sincronizaciones del catálogo desde sistemas externos.</summary>
public interface IProductSyncService
{
    Task<ProductSyncAcceptedResponse> QueueOsisAsync(CancellationToken cancellationToken = default);

    Task ExecuteOsisAsync(Guid executionId, CancellationToken cancellationToken = default);
}
