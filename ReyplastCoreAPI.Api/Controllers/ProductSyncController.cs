using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReyplastCoreAPI.Application.DTOs.Product.Response;
using ReyplastCoreAPI.Application.Interfaces.Services;

namespace ReyplastCoreAPI.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/products/sync")]
public sealed class ProductSyncController : ControllerBase
{
    private readonly IProductSyncService _service;

    public ProductSyncController(IProductSyncService service) => _service = service;

    /// <summary>Encola una sincronización de productos terminados y colores desde XIMESA/OSIS.</summary>
    /// <remarks>Devuelve 202 inmediatamente. El worker solo actúa por esta solicitud; no sincroniza al arrancar.</remarks>
    [HttpPost("osis")]
    public async Task<ActionResult<ProductSyncAcceptedResponse>> QueueOsisAsync(
        CancellationToken cancellationToken)
    {
        ProductSyncAcceptedResponse response = await _service.QueueOsisAsync(cancellationToken);
        return Accepted(response.StatusUrl, response);
    }

    /// <summary>Consulta el historial general de sincronizaciones.</summary>
    /// <param name="pageNumber">Página solicitada, empezando en 1.</param>
    /// <param name="pageSize">Cantidad de ejecuciones por página.</param>
    /// <param name="sourceSystem">Filtro opcional e insensible a mayúsculas: OSIS, SAP o MANUAL.</param>
    /// <param name="cancellationToken">Cancela la consulta si el cliente desconecta.</param>
    [HttpGet("executions")]
    public async Task<ActionResult<PaginatedResponse<SyncExecutionResponse>>> GetExecutionsAsync(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sourceSystem = null,
        CancellationToken cancellationToken = default) =>
        Ok(await _service.GetExecutionsAsync(pageNumber, pageSize, sourceSystem, cancellationToken));

    /// <summary>Consulta estado, avance, resultado y duración de una ejecución.</summary>
    [HttpGet("executions/{executionId:guid}")]
    public async Task<ActionResult<SyncExecutionResponse>> GetExecutionAsync(
        Guid executionId,
        CancellationToken cancellationToken = default) =>
        Ok(await _service.GetExecutionAsync(executionId, cancellationToken));

    /// <summary>Solicita cancelar una ejecución QUEUED o RUNNING.</summary>
    /// <remarks>La cancelación es cooperativa; el estado cambia a CANCELLED al detenerse la operación actual.</remarks>
    [HttpDelete("executions/{executionId:guid}")]
    public async Task<ActionResult<SyncExecutionResponse>> CancelExecutionAsync(
        Guid executionId,
        CancellationToken cancellationToken = default) =>
        Accepted(await _service.CancelExecutionAsync(executionId, cancellationToken));
}
