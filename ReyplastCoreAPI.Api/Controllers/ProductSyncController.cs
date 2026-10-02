using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReyplastCoreAPI.Application.DTOs.Product.Response;
using ReyplastCoreAPI.Application.Interfaces.Services;
using ReyplastCoreAPI.Domain.Catalog;
using ReyplastCoreAPI.Shared.Exceptions;

namespace ReyplastCoreAPI.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/products/sync")]
public sealed class ProductSyncController : ControllerBase
{
    private readonly IProductSyncService _service;

    public ProductSyncController(IProductSyncService service) => _service = service;

    /// <summary>Encola una sincronización del catálogo (productos terminados y colores) desde el
    /// origen indicado. El caller define la fuente en el body en vez de llamar a una ruta
    /// distinta por cada origen -- hoy solo OSIS/XIMESA está implementado; SAP todavía no
    /// tiene lector de catálogo propio.</summary>
    /// <remarks>Devuelve 202 inmediatamente. El worker solo actúa por esta solicitud; no sincroniza al arrancar.</remarks>
    [HttpPost]
    public async Task<ActionResult<ProductSyncAcceptedResponse>> QueueAsync(
        [FromBody] ProductSyncRequest request, CancellationToken cancellationToken)
    {
        ProductSyncAcceptedResponse response = (request.SourceSystem ?? string.Empty).Trim().ToUpperInvariant() switch
        {
            SourceSystems.Osis => await _service.QueueOsisAsync(cancellationToken),
            SourceSystems.Sap => throw new AppException(
                "La sincronización de productos desde SAP aún no está implementada.", 501),
            _ => throw new AppException(
                $"Fuente de sincronización desconocida: '{request.SourceSystem}'. Use {SourceSystems.Osis} o {SourceSystems.Sap}.", 400)
        };
        return Accepted(response);
    }
}
