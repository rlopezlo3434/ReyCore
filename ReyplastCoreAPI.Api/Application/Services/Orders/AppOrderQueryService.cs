using System.Linq.Expressions;
using Microsoft.Extensions.Options;
using ReyplastCoreAPI.Application.DTOs.Order;
using ReyplastCoreAPI.Application.Interfaces.Repositories;
using ReyplastCoreAPI.Application.Interfaces.Services;
using ReyplastCoreAPI.Domain.Catalog;
using ReyplastCoreAPI.Domain.Orders.Entities;
using ReyplastCoreAPI.Shared.Exceptions;

namespace ReyplastCoreAPI.Application.Services.Orders;

/// <summary>Listado de pedidos directo de ReyplastCoreDB con la forma que necesita cada aplicación
/// consumidora (AppCode de PadLock). Cada app define qué campos recibe; así la app no mapea ni lee más
/// de lo que usa. Agregar una app nueva es agregar su proyección en <see cref="GetAsync"/>.</summary>
public sealed class AppOrderQueryService : IAppOrderQueryService
{
    /// <summary>AppCode de SalesPlace en PadLock.</summary>
    public const string SalesPlaceAppCode = "SP";

    // Solo lineas activas; unidades = cantidad - suspendida (mismo criterio que el listado de pedidos).
    private static readonly Expression<Func<SalesOrder, SalesPlaceOrderListItem>> SalesPlaceProjection = order =>
        new SalesPlaceOrderListItem
        {
            Id = order.Id,
            DocNum = order.DocNum,
            CardCode = order.CardCode,
            CardName = order.CardName,
            ChannelCode = order.ChannelCode,
            ChannelName = order.ChannelName,
            ApprovalDate = order.ApprovalDate,
            TotalItems = order.Details.Count(detail => detail.IsActive),
            TotalUnits = order.Details.Where(detail => detail.IsActive)
                .Sum(detail => (decimal?)(detail.Quantity >= detail.SuspendedQuantity
                    ? detail.Quantity - detail.SuspendedQuantity : 0m)) ?? 0m
        };

    private readonly IOrderRepository _orders;
    private readonly AppOrderOptions _options;

    public AppOrderQueryService(IOrderRepository orders, IOptions<AppOrderOptions> options)
    {
        _orders = orders;
        _options = options.Value;
    }

    public async Task<object> GetAsync(AppOrderQuery query, CancellationToken cancellationToken)
    {
        string appCode = NormalizeRequired(query.AppCode, "Debe indicar el código de la aplicación (appCode).");
        string sourceSystem = ValidateSourceSystem(query.SourceSystem);
        // Solo pedidos aprobados: se usa la situación (no ApprovalDate, que no existe en pedidos
        // aprobados antes de que se empezara a guardar).
        string approvedSituationCode = OrderSituationCodes.ApprovedFor(sourceSystem)
            ?? throw new AppException($"El sistema de origen '{sourceSystem}' no tiene definido un estado de pedido aprobado.");
        // Por número de pedido basta appCode + sourceSystem: fechas y demás filtros se ignoran.
        string? docNum = TrimOrNull(query.DocNum);
        if (docNum is null) ValidateDateRange(query.DateFrom, query.DateTo);
        ValidatePaging(query.PageNumber, query.PageSize);

        return appCode switch
        {
            SalesPlaceAppCode => await PageAsync(
                BuildFilter(query, sourceSystem, approvedSituationCode, docNum, ChannelCodes(_options.SalesPlace)),
                query.PageNumber, query.PageSize, SalesPlaceProjection, cancellationToken),
            _ => throw new AppException(
                $"La aplicación '{appCode}' no tiene un listado de pedidos disponible. Aplicaciones habilitadas: {SalesPlaceAppCode}.")
        };
    }

    // El canal aplica siempre, también al buscar por número: un pedido de otro canal no existe para la app.
    private static AppOrderFilter BuildFilter(AppOrderQuery query, string sourceSystem, string situationCode,
        string? docNum, IReadOnlyCollection<string> channelCodes) =>
        docNum is null
            ? new AppOrderFilter(sourceSystem, situationCode, null, query.DateFrom, query.DateTo,
                TrimOrNull(query.CardCode), TrimOrNull(query.CardName), channelCodes)
            : new AppOrderFilter(sourceSystem, situationCode, docNum, null, null, null, null, channelCodes);

    private static IReadOnlyCollection<string> ChannelCodes(AppOrderAppOptions app) =>
        app.ChannelCodes.Select(code => code?.Trim()).OfType<string>().Where(code => code.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

    private async Task<AppOrderPage<TItem>> PageAsync<TItem>(AppOrderFilter filter, int pageNumber, int pageSize,
        Expression<Func<SalesOrder, TItem>> projection, CancellationToken cancellationToken)
    {
        (IReadOnlyList<TItem> items, int total) = await _orders.GetOrdersByApplicationCodeAsync(filter,
            pageNumber, pageSize, projection, cancellationToken);
        return new AppOrderPage<TItem>
        {
            Items = items,
            Page = pageNumber,
            PageSize = pageSize,
            TotalRecords = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };
    }

    private static string ValidateSourceSystem(string? value)
    {
        string sourceSystem = NormalizeRequired(value, "Debe indicar el sistema de origen (sourceSystem).");
        if (!SourceSystems.All.Contains(sourceSystem))
            throw new AppException(
                $"El sistema de origen '{sourceSystem}' no es válido. Valores permitidos: {SourceSystems.Osis} o {SourceSystems.Sap}.");
        return sourceSystem;
    }

    private static void ValidateDateRange(DateTime? dateFrom, DateTime? dateTo)
    {
        if (dateFrom is null) throw new AppException("Debe indicar la fecha inicial (dateFrom) o el número de pedido (docNum).");
        if (dateTo is null) throw new AppException("Debe indicar la fecha final (dateTo) o el número de pedido (docNum).");
        if (dateFrom.Value.Date > dateTo.Value.Date)
            throw new AppException("La fecha inicial (dateFrom) no puede ser posterior a la fecha final (dateTo).");
    }

    private static void ValidatePaging(int pageNumber, int pageSize)
    {
        if (pageNumber <= 0) throw new AppException("El número de página (pageNumber) debe ser mayor que cero.");
        if (pageSize <= 0) throw new AppException("La cantidad de registros por página (pageSize) debe ser mayor que cero.");
    }

    private static string NormalizeRequired(string? value, string message) =>
        string.IsNullOrWhiteSpace(value) ? throw new AppException(message) : value.Trim().ToUpperInvariant();

    private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
