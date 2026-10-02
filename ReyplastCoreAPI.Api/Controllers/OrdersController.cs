using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using ReyplastCoreAPI.Application.DTOs.Order;
using ReyplastCoreAPI.Application.DTOs.Product.Response;
using ReyplastCoreAPI.Application.Interfaces.Services;

namespace ReyplastCoreAPI.Api.Controllers;

[ApiController, AllowAnonymous, Route("api/orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly IOrderService _service;
    private readonly IAppOrderQueryService _appOrders;

    public OrdersController(IOrderService service, IAppOrderQueryService appOrders)
    {
        _service = service;
        _appOrders = appOrders;
    }

    /// <summary>Listado de pedidos directo de ReyplastCoreDB con los campos que necesita la aplicación
    /// indicada (AppCode de PadLock). Hoy solo "SP" (SalesPlace): id, docNum, cardCode, cardName,
    /// channelCode, channelName, approvalDate, totalItems, totalUnits. Solo devuelve pedidos aprobados
    /// (OSIS: situación 2 "PEDIDO APROBADO"). Si se envía docNum, solo se usan appCode y sourceSystem:
    /// las fechas no son obligatorias y ni ellas ni los demás filtros se aplican.</summary>
    /// <param name="appCode">Aplicación consumidora, por ejemplo SP.</param>
    /// <param name="sourceSystem">Origen de los pedidos: OSIS o SAP.</param>
    /// <param name="dateFrom">Obligatoria si no se envía docNum. Fecha documental inicial, inclusiva (yyyy-MM-dd).</param>
    /// <param name="dateTo">Obligatoria si no se envía docNum. Fecha documental final, inclusiva (yyyy-MM-dd).</param>
    /// <param name="docNum">Opcional. Número de pedido (coincidencia por prefijo); si se envía, se ignoran los demás filtros.</param>
    /// <param name="cardCode">Opcional. Código del cliente: RUC o DNI (coincidencia por el inicio).</param>
    /// <param name="cardName">Opcional. Nombre o razón social del cliente; basta una parte (por ejemplo, TOTTUS).</param>    /// <param name="pageNumber">Opcional. Número de página, mayor que cero. Predeterminado: 1.</param>
    /// <param name="pageSize">Opcional. Cantidad de pedidos por página, mayor que cero. Predeterminado: 20.</param>
    /// <param name="cancellationToken">Cancela la consulta si el cliente cierra la solicitud.</param>
    /// <response code="200">Página de pedidos con los campos de la aplicación (para SP, SalesPlaceOrderListItem).</response>
    /// <response code="400">Falta un filtro obligatorio, un valor no es válido o la aplicación no tiene listado.</response>
    [HttpGet("app/{appCode}")]
    [ProducesResponseType(typeof(AppOrderPage<SalesPlaceOrderListItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetForAppAsync(string appCode,
        [FromQuery, Required(ErrorMessage = "Debe indicar el sistema de origen (sourceSystem).")] string? sourceSystem,
        [FromQuery] DateTime? dateFrom = null, [FromQuery] DateTime? dateTo = null, [FromQuery] string? docNum = null,
        [FromQuery] string? cardCode = null, [FromQuery] string? cardName = null,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default) =>
        Ok(await _appOrders.GetAsync(new AppOrderQuery
        {
            AppCode = appCode,
            SourceSystem = sourceSystem,
            DateFrom = dateFrom,
            DateTo = dateTo,
            DocNum = docNum,
            CardCode = cardCode,
            CardName = cardName,
            PageNumber = pageNumber,
            PageSize = pageSize
        }, cancellationToken));

    /// <summary>Lista cabeceras de pedidos sin cargar sus detalles.</summary>
    /// <param name="pageNumber">Número de página, mayor que cero. Predeterminado: 1.</param>
    /// <param name="pageSize">Cantidad de cabeceras por página, mayor que cero y sin tope hardcodeado.</param>
    /// <param name="search">Uno o más textos para buscar por DocNum, CardCode o CardName.</param>
    /// <param name="situationCode">Uno o más códigos de situación del origen: OSIS usa 0-7 y SAP usa O/C.</param>
    /// <param name="channelCode">Uno o más códigos de canal comercial.</param>
    /// <param name="dateFrom">Fecha documental inicial inclusiva; en OSIS corresponde a PPC_FECCRE.</param>
    /// <param name="dateTo">Fecha documental final inclusiva; en OSIS corresponde a PPC_FECCRE.</param>
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

    /// <summary>Cantidad de pedidos por canal, con los mismos filtros que la lista paginada salvo
    /// channelCode -- para mostrar el conteo real junto a cada opción de un selector de canal.</summary>
    [HttpGet("channel-counts")]
    public async Task<ActionResult<List<OrderChannelCountResponse>>> GetChannelCountsAsync(
        [FromQuery] string[]? search = null, [FromQuery] string[]? situationCode = null,
        [FromQuery] DateTime? dateFrom = null, [FromQuery] DateTime? dateTo = null,
        [FromQuery] string[]? sourceSystem = null, CancellationToken cancellationToken = default) =>
        Ok(await _service.GetChannelCountsAsync(search, situationCode, dateFrom, dateTo, sourceSystem, cancellationToken));

    /// <summary>Obtiene la cabecera de un pedido por su identificador local.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderResponse>> GetByIdAsync(Guid id,
        CancellationToken cancellationToken = default) => Ok(await _service.GetByIdAsync(id, cancellationToken));

    /// <summary>Obtiene la cabecera y todas las líneas usando el número de pedido del origen.</summary>
    /// <param name="docNum">PPC_NUMPPC en OSIS o DocNum en SAP; por ejemplo, 26091153.</param>
    /// <param name="sourceSystem">Origen opcional para distinguir el mismo DocNum en OSIS y SAP.</param>
    /// <param name="cancellationToken">Cancela la consulta si el cliente cierra la solicitud.</param>
    [HttpGet("doc-number/{docNum}")]
    [HttpGet("{docNum}/complete")]
    public async Task<ActionResult<CompleteOrderResponse>> GetCompleteByDocNumAsync(string docNum,
        [FromQuery] string? sourceSystem = null,
        CancellationToken cancellationToken = default) =>
        Ok(await _service.GetCompleteByDocNumAsync(
            docNum, sourceSystem, cancellationToken));

    /// <summary>Obtiene cabecera y líneas de VARIOS pedidos por su DocNum en una sola consulta --
    /// version batch de {docNum}/complete para evitar N llamadas HTTP individuales cuando el
    /// llamador ya sabe que documentos necesita (por ejemplo la lista de pedidos asignados a un
    /// picker). Un docNum no encontrado o ambiguo simplemente no aparece en la respuesta.</summary>
    /// <param name="docNum">Uno o más DocNum a consultar.</param>
    /// <param name="sourceSystem">Origen opcional, igual que en {docNum}/complete.</param>
    /// <param name="cancellationToken">Cancela la consulta si el cliente cierra la solicitud.</param>
    [HttpGet("complete-batch")]
    public async Task<ActionResult<List<CompleteOrderResponse>>> GetCompleteBatchByDocNumAsync(
        [FromQuery] string[] docNum, [FromQuery] string? sourceSystem = null,
        CancellationToken cancellationToken = default) =>
        Ok(await _service.GetCompleteByDocNumsAsync(docNum, sourceSystem, cancellationToken));

    /// <summary>Obtiene todas las líneas de un pedido usando su DocNum y, opcionalmente, el origen.</summary>
    [HttpGet("doc-number/{docNum}/details")]
    public async Task<ActionResult<IReadOnlyList<OrderDetailResponse>>> GetDetailsByDocNumAsync(
        string docNum, [FromQuery] string? sourceSystem = null,
        CancellationToken cancellationToken = default)
    {
        CompleteOrderResponse order = await _service.GetCompleteByDocNumAsync(
            docNum, sourceSystem, cancellationToken);
        return Ok(order.Details);
    }

    /// <summary>Lista de forma paginada los productos pertenecientes al pedido.</summary>
    [HttpGet("{id:guid}/details")]
    public async Task<ActionResult<PaginatedResponse<OrderDetailResponse>>> GetDetailsAsync(Guid id,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await _service.GetDetailsAsync(id, pageNumber, pageSize, cancellationToken));
}
