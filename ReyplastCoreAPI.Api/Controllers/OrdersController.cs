using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReyplastCoreAPI.Application.DTOs.Order;
using ReyplastCoreAPI.Application.DTOs.Product.Response;
using ReyplastCoreAPI.Application.Interfaces.Services;

namespace ReyplastCoreAPI.Api.Controllers;

[ApiController, AllowAnonymous, Route("api/orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly IOrderService _service;
    public OrdersController(IOrderService service) => _service = service;

    /// <summary>Lista cabeceras de pedidos sin cargar sus detalles.</summary>
    /// <param name="pageNumber">Número de página, mayor que cero. Predeterminado: 1.</param>
    /// <param name="pageSize">Cantidad de cabeceras por página, mayor que cero y sin tope hardcodeado.</param>
    /// <param name="search">Uno o más textos para buscar por DocNum, CardCode o CardName.</param>
    /// <param name="situationCode">Uno o más códigos de situación del origen: OSIS usa 0-7 y SAP usa O/C.</param>
    /// <param name="channelCode">Uno o más códigos de canal comercial.</param>
    /// <param name="dateFrom">Fecha inicial inclusiva aplicada sobre PPC_FECCRE.</param>
    /// <param name="dateTo">Fecha final inclusiva aplicada sobre PPC_FECCRE.</param>
    /// <param name="sourceSystem">Uno o más sistemas de origen, por ejemplo OSIS o SAP.</param>
    /// <param name="cancellationToken">Cancela la consulta si el cliente cierra la solicitud.</param>
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<OrderResponse>>> GetPagedAsync(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string[]? search = null, [FromQuery] string[]? situationCode = null,
        [FromQuery] string[]? channelCode = null, [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null, [FromQuery] string[]? sourceSystem = null,
        CancellationToken cancellationToken = default) =>
        Ok(await _service.GetPagedAsync(pageNumber, pageSize, search, situationCode, channelCode,
            dateFrom, dateTo, sourceSystem, cancellationToken));

    /// <summary>Obtiene la cabecera de un pedido por su identificador local.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderResponse>> GetByIdAsync(Guid id,
        CancellationToken cancellationToken = default) => Ok(await _service.GetByIdAsync(id, cancellationToken));

    /// <summary>Obtiene la cabecera y todas las líneas usando el número de pedido OSIS.</summary>
    /// <param name="docNum">PPC_NUMPPC; por ejemplo, 26091153.</param>
    /// <param name="cancellationToken">Cancela la consulta si el cliente cierra la solicitud.</param>
    [HttpGet("doc-number/{docNum}")]
    [HttpGet("{docNum}/complete")]
    public async Task<ActionResult<CompleteOrderResponse>> GetCompleteByDocNumAsync(string docNum,
        CancellationToken cancellationToken = default) =>
        Ok(await _service.GetCompleteByDocNumAsync(docNum, cancellationToken));

    /// <summary>Obtiene todas las líneas de un pedido usando PPC_NUMPPC.</summary>
    [HttpGet("doc-number/{docNum}/details")]
    public async Task<ActionResult<IReadOnlyList<OrderDetailResponse>>> GetDetailsByDocNumAsync(
        string docNum, CancellationToken cancellationToken = default)
    {
        CompleteOrderResponse order = await _service.GetCompleteByDocNumAsync(docNum, cancellationToken);
        return Ok(order.Details);
    }

    /// <summary>Lista de forma paginada los productos pertenecientes al pedido.</summary>
    [HttpGet("{id:guid}/details")]
    public async Task<ActionResult<PaginatedResponse<OrderDetailResponse>>> GetDetailsAsync(Guid id,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await _service.GetDetailsAsync(id, pageNumber, pageSize, cancellationToken));
}
