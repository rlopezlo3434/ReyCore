using ReyplastCoreAPI.Application.DTOs.Order;
using ReyplastCoreAPI.Application.DTOs.Product.Response;
using ReyplastCoreAPI.Application.Interfaces.Repositories;
using ReyplastCoreAPI.Application.Interfaces.Services;
using ReyplastCoreAPI.Domain.Catalog;
using ReyplastCoreAPI.Domain.Orders.Entities;
using ReyplastCoreAPI.Shared.Exceptions;
using ReyplastCoreAPI.Shared.Helpers;
using ReyplastCoreAPI.Shared.Mappers;

namespace ReyplastCoreAPI.Application.Services.Orders;

public sealed class OrderService : IOrderService
{
    private readonly IOrderRepository _repository;
    private readonly IOrderSyncService _syncService;

    public OrderService(IOrderRepository repository, IOrderSyncService syncService)
    {
        _repository = repository;
        _syncService = syncService;
    }

    public async Task<PaginatedResponse<OrderResponse>> GetPagedAsync(int pageNumber, int pageSize,
        IReadOnlyCollection<string>? search, IReadOnlyCollection<string>? situationCode,
        IReadOnlyCollection<string>? channelCode, DateTime? dateFrom, DateTime? dateTo,
        IReadOnlyCollection<string>? sourceSystem, CancellationToken cancellationToken = default)
    {
        ValidationHelper.ThrowIfInvalidPage(pageNumber, pageSize);
        string[] situations = NormalizeSituations(situationCode);
        string[] sources = NormalizeSources(sourceSystem);
        if (dateFrom.HasValue && dateTo.HasValue && dateFrom.Value.Date > dateTo.Value.Date)
            throw new AppException("dateFrom no puede ser mayor que dateTo.");
        PaginatedResponse<SalesOrder> page = await _repository.GetPagedAsync(pageNumber, pageSize,
            NormalizeValues(search), situations, NormalizeValues(channelCode), dateFrom, dateTo, sources,
            cancellationToken);
        return MapPage(page, OrderMapper.ToResponse);
    }

    public async Task<OrderResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ValidationHelper.ThrowIfInvalidId(id, "El identificador del pedido no es válido.");
        SalesOrder? order = await _repository.GetByIdAsync(id, cancellationToken);
        return order is null ? throw new AppException("No se encontró el pedido.", 404) : OrderMapper.ToResponse(order);
    }

    public async Task<CompleteOrderResponse> GetCompleteByDocNumAsync(string docNum,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(docNum))
            throw new AppException("El número de pedido es obligatorio.");

        IReadOnlyList<SalesOrder> matches = await _repository.GetCompleteByDocNumAsync(
            docNum.Trim(), cancellationToken);
        if (matches.Count == 0 && await _syncService.SyncOsisByDocNumAsync(docNum.Trim(), cancellationToken))
            matches = await _repository.GetCompleteByDocNumAsync(docNum.Trim(), cancellationToken);
        if (matches.Count == 0)
            throw new AppException("No se encontró el pedido.", 404);
        if (matches.Count > 1)
            throw new AppException(
                "El número de pedido existe en más de un origen o sucursal y no identifica un pedido único.", 409);

        SalesOrder order = matches[0];
        order.TotalItems = order.Details.Count;
        return OrderMapper.ToCompleteResponse(order);
    }

    public async Task<PaginatedResponse<OrderDetailResponse>> GetDetailsAsync(Guid id, int pageNumber,
        int pageSize, CancellationToken cancellationToken = default)
    {
        _ = await GetByIdAsync(id, cancellationToken);
        ValidationHelper.ThrowIfInvalidPage(pageNumber, pageSize);
        PaginatedResponse<SalesOrderDetail> page = await _repository.GetDetailsAsync(id, pageNumber, pageSize,
            cancellationToken);
        return MapPage(page, OrderMapper.ToResponse);
    }

    private static string[] NormalizeSituations(IEnumerable<string>? values)
    {
        string[] situations = NormalizeValues(values).Select(value => value.ToUpperInvariant()).ToArray();
        if (situations.Any(value => value.Length > 20))
            throw new AppException("Cada situationCode debe tener como máximo 20 caracteres.");
        return situations;
    }

    private static string[] NormalizeSources(IEnumerable<string>? values)
    {
        string[] sources = NormalizeValues(values).Select(value => value.ToUpperInvariant()).ToArray();
        if (sources.Any(source => !SourceSystems.All.Contains(source)))
            throw new AppException("Uno o más sistemas de origen no son válidos.");
        return sources;
    }

    // Acepta ?campo=A&campo=B y también ?campo=A,B sin duplicar valores.
    private static string[] NormalizeValues(IEnumerable<string>? values) => values?
        .SelectMany(value => value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray() ?? Array.Empty<string>();
    private static PaginatedResponse<TOut> MapPage<TIn, TOut>(PaginatedResponse<TIn> page, Func<TIn, TOut> map) => new()
    {
        Items = page.Items.Select(map).ToList(), PageNumber = page.PageNumber, PageSize = page.PageSize,
        TotalRecords = page.TotalRecords, TotalPages = page.TotalPages
    };
}
