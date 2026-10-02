using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReyplastCoreAPI.Application.DTOs.Order;
using ReyplastCoreAPI.Application.Interfaces.Services;
using ReyplastCoreAPI.Domain.Catalog;
using ReyplastCoreAPI.Shared.Exceptions;

namespace ReyplastCoreAPI.Api.Controllers;

/// <summary>Punto de entrada único para sincronizar pedidos por rango o documento
/// individual, sin importar el origen. El caller define la fuente (ver
/// <see cref="OrderSyncRangeRequest.SourceSystem"/> / query "sourceSystem") en vez de llamar a
/// una ruta distinta por cada origen -- así MasterLogicApi_V2 solo cambia su configuración
/// (appsettings ReyplastCoreApi:SourceSystem) para pasar de OSIS a SAP.</summary>
[ApiController, AllowAnonymous, Route("api/orders/sync")]
public sealed class OrderSynchronizationsController : ControllerBase
{
    private readonly IOsisOrderSyncService _osis;
    private readonly ISapOrderSyncService _sap;

    public OrderSynchronizationsController(IOsisOrderSyncService osis, ISapOrderSyncService sap)
    {
        _osis = osis;
        _sap = sap;
    }

    /// <summary>Encola en segundo plano la importación de pedidos dentro de un rango inclusivo,
    /// desde el origen indicado en <see cref="OrderSyncRangeRequest.SourceSystem"/>.</summary>
    [HttpPost("range")]
    public async Task<ActionResult<OrderSyncAcceptedResponse>> QueueRangeAsync(
        [FromBody] OrderSyncRangeRequest request, CancellationToken cancellationToken)
    {
        OrderSyncAcceptedResponse response = (request.SourceSystem ?? string.Empty).Trim().ToUpperInvariant() switch
        {
            SourceSystems.Osis => await _osis.QueueRangeAsync(request.DateFrom, request.DateTo, cancellationToken),
            SourceSystems.Sap => await _sap.QueueRangeAsync(request.DateFrom, request.DateTo, cancellationToken),
            _ => throw new AppException(
                $"Fuente de sincronización desconocida: '{request.SourceSystem}'. Use {SourceSystems.Osis} o {SourceSystems.Sap}.", 400)
        };
        return Accepted(response);
    }

    /// <summary>Relee y actualiza un único pedido por número de documento, desde el origen indicado.
    /// Responde 200 con <c>{ found, changed }</c>: <c>changed</c> indica si cambió algo real, para que
    /// el llamador solo vuelva a leer (y avise a otros usuarios) cuando hace falta.</summary>
    [HttpPost("document/{docNum}")]
    public async Task<ActionResult<OrderDocumentSyncResult>> SyncDocumentAsync(
        string docNum, [FromQuery] string sourceSystem, CancellationToken cancellationToken)
    {
        OrderDocumentSyncResult result = (sourceSystem ?? string.Empty).Trim().ToUpperInvariant() switch
        {
            SourceSystems.Osis => await _osis.SyncByDocNumAsync(docNum, cancellationToken),
            SourceSystems.Sap => await _sap.SyncByDocNumAsync(docNum, cancellationToken),
            _ => throw new AppException(
                $"Fuente de sincronización desconocida: '{sourceSystem}'. Use {SourceSystems.Osis} o {SourceSystems.Sap}.", 400)
        };
        return result.Found ? Ok(result) : NotFound(new { message = "No se encontró el pedido en el origen indicado." });
    }

}
