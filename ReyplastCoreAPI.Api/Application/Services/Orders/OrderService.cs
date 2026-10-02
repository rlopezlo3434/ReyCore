using ReyplastCoreAPI.Application.DTOs.Order;
using ReyplastCoreAPI.Application.DTOs.Product.Response;
using ReyplastCoreAPI.Application.Interfaces.Repositories;
using ReyplastCoreAPI.Application.Interfaces.Services;
using ReyplastCoreAPI.Domain.Catalog;
using ReyplastCoreAPI.Domain.Catalog.Entities;
using ReyplastCoreAPI.Domain.Orders.Entities;
using ReyplastCoreAPI.Shared.Exceptions;
using ReyplastCoreAPI.Shared.Helpers;
using ReyplastCoreAPI.Shared.Mappers;
using Microsoft.Extensions.Logging;

namespace ReyplastCoreAPI.Application.Services.Orders;

public sealed class OrderService : IOrderService
{
    // Tope explicito para complete-batch: sin uno, el binding de docNum como querystring repetido
    // (?docNum=A&docNum=B&...) puede exceder el limite de longitud de query string de IIS (2048
    // caracteres por defecto, ~110 docNums a este tamano) y empezar a fallar con 400 ANTES de
    // llegar aqui, sin ningun mensaje claro de por que. Se corta antes, con un error entendible.
    private const int MaxCompleteBatchSize = 100;

    private readonly IOrderRepository _repository;
    private readonly IChannelRepository _channelRepository;
    private readonly ILogger<OrderService> _logger;

    public OrderService(IOrderRepository repository, IChannelRepository channelRepository, ILogger<OrderService> logger)
    {
        _repository = repository;
        _channelRepository = channelRepository;
        _logger = logger;
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
        PaginatedResponse<OrderResponse> result = MapPage(page, OrderMapper.ToResponse);
        Dictionary<string, string> displayNames = await GetChannelDisplayNamesAsync(cancellationToken);
        foreach (OrderResponse item in result.Items) ApplyChannelDisplayName(item, displayNames);
        return result;
    }

    public async Task<OrderResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ValidationHelper.ThrowIfInvalidId(id, "El identificador del pedido no es válido.");
        SalesOrder? order = await _repository.GetByIdAsync(id, cancellationToken);
        if (order is null) throw new AppException("No se encontró el pedido.", 404);
        OrderResponse response = OrderMapper.ToResponse(order);
        ApplyChannelDisplayName(response, await GetChannelDisplayNamesAsync(cancellationToken));
        return response;
    }

    public async Task<CompleteOrderResponse> GetCompleteByDocNumAsync(string docNum, string? sourceSystem,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(docNum))
            throw new AppException("El número de pedido es obligatorio.");

        string? source = NormalizeSources(string.IsNullOrWhiteSpace(sourceSystem)
            ? null : new[] { sourceSystem }).SingleOrDefault();
        IReadOnlyList<SalesOrder> matches = await _repository.GetCompleteByDocNumAsync(
            docNum.Trim(), source, cancellationToken);

        if (matches.Count == 0)
            throw new AppException("No se encontró el pedido en RP_ReyPlastCoreBD. Ejecute la sincronización por rango correspondiente.", 404);
        if (matches.Count > 1)
            throw new AppException(
                "El número de pedido existe en más de un origen o sucursal y no identifica un pedido único.", 409);

        SalesOrder order = matches[0];
        // Una linea anulada en OSIS (PPD_INDEST inactivo) se sigue sincronizando -- se necesita
        // para poder mostrarla marcada como anulada -- pero no debe contar como parte real del
        // pedido (ver TotalItems en OrderRepository.GetPagedAsync/GetByIdAsync, mismo criterio).
        order.TotalItems = order.Details.Count(detail => detail.IsActive);
        order.TotalUnits = order.Details.Where(detail => detail.IsActive)
            .Sum(detail => Math.Max(0, detail.Quantity - detail.SuspendedQuantity));
        CompleteOrderResponse response = OrderMapper.ToCompleteResponse(order);
        ApplyChannelDisplayName(response.Header, await GetChannelDisplayNamesAsync(cancellationToken));
        return response;
    }

    public async Task<List<CompleteOrderResponse>> GetCompleteByDocNumsAsync(
        IReadOnlyCollection<string> docNums, string? sourceSystem, CancellationToken cancellationToken = default)
    {
        string[] cleanDocNums = docNums.Where(docNum => !string.IsNullOrWhiteSpace(docNum))
            .Select(docNum => docNum.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (cleanDocNums.Length == 0) return new List<CompleteOrderResponse>();
        if (cleanDocNums.Length > MaxCompleteBatchSize)
            throw new AppException($"complete-batch admite como maximo {MaxCompleteBatchSize} docNum por llamada; se recibieron {cleanDocNums.Length}.");

        string? source = NormalizeSources(string.IsNullOrWhiteSpace(sourceSystem)
            ? null : new[] { sourceSystem }).SingleOrDefault();
        IReadOnlyList<SalesOrder> matches = await _repository.GetCompleteByDocNumsAsync(
            cleanDocNums, source, cancellationToken);

        Dictionary<string, List<SalesOrder>> byDocNum = matches
            .GroupBy(order => order.DocNum, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string> displayNames = await GetChannelDisplayNamesAsync(cancellationToken);

        List<CompleteOrderResponse> results = new(cleanDocNums.Length);
        foreach (string docNum in cleanDocNums)
        {
            if (!byDocNum.TryGetValue(docNum, out List<SalesOrder>? group)) continue;
            // Mismo criterio de ambiguedad que la version individual (ver GetCompleteByDocNumAsync),
            // pero aqui un grupo ambiguo se descarta en vez de tumbar el batch completo -- antes
            // esto pasaba en silencio, sin ningun rastro de que un docNum se hubiera caido del
            // resultado ni de por que.
            if (group.Count > 1)
            {
                _logger.LogWarning(
                    "complete-batch: docNum {DocNum} descartado por ambiguo -- coincide con {MatchCount} origenes/sucursales distintos.",
                    docNum, group.Count);
                continue;
            }
            SalesOrder order = group[0];
            order.TotalItems = order.Details.Count(detail => detail.IsActive);
            order.TotalUnits = order.Details.Where(detail => detail.IsActive)
                .Sum(detail => Math.Max(0, detail.Quantity - detail.SuspendedQuantity));
            CompleteOrderResponse response = OrderMapper.ToCompleteResponse(order);
            ApplyChannelDisplayName(response.Header, displayNames);
            results.Add(response);
        }
        return results;
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

    public async Task<List<OrderChannelCountResponse>> GetChannelCountsAsync(
        IReadOnlyCollection<string>? search, IReadOnlyCollection<string>? situationCode,
        DateTime? dateFrom, DateTime? dateTo, IReadOnlyCollection<string>? sourceSystem,
        CancellationToken cancellationToken = default)
    {
        string[] situations = NormalizeSituations(situationCode);
        string[] sources = NormalizeSources(sourceSystem);
        var counts = await _repository.GetChannelCountsAsync(
            NormalizeValues(search), situations, dateFrom, dateTo, sources, cancellationToken);
        return counts
            .Select(item => new OrderChannelCountResponse
            { ChannelCode = item.ChannelCode, ChannelName = item.ChannelName, Count = item.Count })
            .OrderByDescending(item => item.Count)
            .ToList();
    }

    // El nombre que trae cada pedido (ChannelName) es un string desnormalizado copiado del sync de
    // OSIS/SAP -- no pasa por la agrupacion curada a mano en RP_ReyPlastCoreBD (ChannelGroup), que hasta
    // ahora solo se usaba para armar las opciones del filtro "Canal de pedido" (ver
    // PickerOrderService.GetChannelsAsync en MasterLogicApi_V2). Se resuelve aqui, del lado de
    // ReyplastCoreAPI, para que CUALQUIER consumidor reciba ya el nombre agrupado sin tener que
    // reimplementar el mismo criterio por su cuenta.
    private async Task<Dictionary<string, string>> GetChannelDisplayNamesAsync(CancellationToken cancellationToken)
    {
        List<Channel> channels = await _channelRepository.GetAllAsync(isActive: null, sourceSystem: null, cancellationToken);
        return channels
            .Where(channel => !string.IsNullOrWhiteSpace(channel.Code))
            .GroupBy(channel => channel.Code, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.First().ChannelGroup?.Name ?? group.First().Name,
                StringComparer.OrdinalIgnoreCase);
    }

    // Un canal sin ChannelGroupId asignado (ChannelGroup nulo) no tiene entrada agrupada -- el
    // ChannelCode original nunca se toca, solo el ChannelName que se muestra.
    private static void ApplyChannelDisplayName(OrderResponse response, IReadOnlyDictionary<string, string> displayNames)
    {
        if (response.ChannelCode != null && displayNames.TryGetValue(response.ChannelCode, out string? displayName))
            response.ChannelName = displayName;
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
