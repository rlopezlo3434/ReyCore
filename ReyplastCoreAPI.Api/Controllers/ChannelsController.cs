using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReyplastCoreAPI.Application.DTOs.Channel.Response;
using ReyplastCoreAPI.Application.Interfaces.Services;

namespace ReyplastCoreAPI.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/channels")]
public sealed class ChannelsController : ControllerBase
{
    private readonly IChannelService _service;

    public ChannelsController(IChannelService service) => _service = service;

    /// <summary>Lista el catálogo local de canales de venta.</summary>
    /// <param name="isActive">true para activos, false para inactivos; nulo devuelve ambos.</param>
    /// <param name="sourceSystem">Origen OSIS; no distingue mayúsculas.</param>
    [HttpGet]
    public async Task<ActionResult<List<ChannelResponse>>> GetAllAsync(
        [FromQuery] bool? isActive = null,
        [FromQuery] string? sourceSystem = null) =>
        Ok(await _service.GetAllAsync(isActive, sourceSystem));

    /// <summary>Sincroniza el catálogo de canales desde OSIS ([MASTERLOGIC].[ListarCanales] /
    /// tipo_lista_precio_tlp). Corre en la misma solicitud: el origen es pequeño y cerrado.</summary>
    [HttpPost("sync/osis")]
    public async Task<ActionResult<ChannelSyncResponse>> SyncFromOsisAsync(CancellationToken cancellationToken) =>
        Ok(await _service.SyncFromOsisAsync(cancellationToken));

    /// <summary>Agrupación normalizada de canales, curada a mano desde RP_ReyPlastCoreBD (no viene
    /// de OSIS/SAP) -- permite consolidar varios canales finos bajo un mismo nombre limpio.</summary>
    [HttpGet("groups")]
    public async Task<ActionResult<List<ChannelGroupResponse>>> GetGroupsAsync(CancellationToken cancellationToken) =>
        Ok(await _service.GetGroupsAsync(cancellationToken));

    [HttpPost("groups")]
    public async Task<ActionResult<ChannelGroupResponse>> CreateGroupAsync(
        [FromBody] ChannelGroupCreateRequest request, CancellationToken cancellationToken) =>
        Ok(await _service.CreateGroupAsync(request, cancellationToken));

    /// <summary>Asigna (ChannelGroupId con valor) o quita (ChannelGroupId nulo) la agrupación de un canal.</summary>
    [HttpPatch("{channelId:guid}/group")]
    public async Task<ActionResult<ChannelResponse>> AssignGroupAsync(
        Guid channelId, [FromBody] ChannelGroupAssignmentRequest request, CancellationToken cancellationToken) =>
        Ok(await _service.AssignGroupAsync(channelId, request, cancellationToken));
}
