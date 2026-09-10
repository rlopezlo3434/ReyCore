using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReyplastCoreAPI.Application.DTOs.Product.Request;
using ReyplastCoreAPI.Application.DTOs.Product.Response;
using ReyplastCoreAPI.Application.Interfaces.Services;

namespace ReyplastCoreAPI.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/products")]
public sealed class ProductsController : ControllerBase
{
    private readonly IProductService _service;

    public ProductsController(IProductService service) => _service = service;

    /// <summary>Lista productos locales con paginación y filtros combinables.</summary>
    /// <param name="pageNumber">Página solicitada, empezando en 1.</param>
    /// <param name="pageSize">Cantidad de productos por página.</param>
    /// <param name="search">Texto opcional para código, descripción o color.</param>
    /// <param name="productTypes">Uno o varios tipos. Se repite productType en la URL.</param>
    /// <param name="isActive">true para activos, false para inactivos; nulo devuelve ambos.</param>
    /// <param name="sourceSystem">Origen OSIS, SAP o MANUAL; no distingue mayúsculas.</param>
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<ProductResponse>>> GetPagedAsync(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery(Name = "productType")] string[]? productTypes = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? sourceSystem = null) =>
        Ok(await _service.GetPagedAsync(
            pageNumber, pageSize, search, productTypes, isActive, sourceSystem));

    /// <summary>Obtiene un producto y sus colores por identificador.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductResponse>> GetByIdAsync(Guid id) =>
        Ok(await _service.GetByIdAsync(id));

    /// <summary>Obtiene un producto y sus colores por código genérico.</summary>
    [HttpGet("code/{genericCode}")]
    public async Task<ActionResult<ProductResponse>> GetByGenericCodeAsync(string genericCode) =>
        Ok(await _service.GetByGenericCodeAsync(genericCode));

    /// <summary>Crea un producto local con sourceSystem MANUAL.</summary>
    [HttpPost]
    public async Task<ActionResult<ProductResponse>> CreateAsync([FromBody] ProductCreateRequest request)
    {
        ProductResponse response = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetByIdAsync), new { id = response.Id }, response);
    }

    /// <summary>Actualiza únicamente productos MANUAL; OSIS y SAP deben editarse en el origen.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductResponse>> UpdateAsync(Guid id, [FromBody] ProductUpdateRequest request) =>
        Ok(await _service.UpdateAsync(id, request));

    /// <summary>Cambia el estado únicamente de un producto MANUAL.</summary>
    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<StatusChangeResponse>> UpdateStatusAsync(
        Guid id, [FromBody] StatusChangeRequest request) =>
        Ok(await _service.UpdateStatusAsync(id, request));

}
