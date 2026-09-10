using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReyplastCoreAPI.Application.DTOs.Order;
using ReyplastCoreAPI.Application.DTOs.Product.Response;
using ReyplastCoreAPI.Application.Interfaces.Services;

namespace ReyplastCoreAPI.Api.Controllers;

[ApiController, AllowAnonymous, Route("api/orders/sync")]
public sealed class OrderSyncController : ControllerBase
{
    private readonly IOrderSyncService _service;
    public OrderSyncController(IOrderSyncService service) => _service = service;

    /// <summary>Encola en segundo plano la sincronización de cabeceras PPC y detalles PPD.</summary>
    [HttpPost("osis")]
    public async Task<ActionResult<OrderSyncAcceptedResponse>> QueueOsisAsync(
        CancellationToken cancellationToken)
    {
        OrderSyncAcceptedResponse response = await _service.QueueOsisAsync(cancellationToken);
        return Accepted(response.StatusUrl, response);
    }

    [HttpGet("executions")]
    public async Task<ActionResult<PaginatedResponse<SyncExecutionResponse>>> GetExecutionsAsync(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? sourceSystem = null, CancellationToken cancellationToken = default) =>
        Ok(await _service.GetExecutionsAsync(pageNumber, pageSize, sourceSystem, cancellationToken));

    [HttpGet("executions/{executionId:guid}")]
    public async Task<ActionResult<SyncExecutionResponse>> GetExecutionAsync(Guid executionId,
        CancellationToken cancellationToken = default) =>
        Ok(await _service.GetExecutionAsync(executionId, cancellationToken));

    [HttpDelete("executions/{executionId:guid}")]
    public async Task<ActionResult<SyncExecutionResponse>> CancelExecutionAsync(Guid executionId,
        CancellationToken cancellationToken = default) =>
        Accepted(await _service.CancelExecutionAsync(executionId, cancellationToken));
}
