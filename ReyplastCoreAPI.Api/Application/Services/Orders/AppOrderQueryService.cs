using System.Linq.Expressions;
using Microsoft.Extensions.Options;
using ReyplastCoreAPI.Application.DTOs.Order;
using ReyplastCoreAPI.Application.Interfaces.Repositories;
using ReyplastCoreAPI.Application.Interfaces.Services;
using ReyplastCoreAPI.Domain.Catalog;
using ReyplastCoreAPI.Domain.Orders;
using ReyplastCoreAPI.Domain.Orders.Entities;
using ReyplastCoreAPI.Shared.Exceptions;
using ReyplastCoreAPI.Shared.Helpers;

namespace ReyplastCoreAPI.Application.Services.Orders;

/// <summary>Listado de pedidos directo de ReyplastCoreDB con la forma que necesita cada aplicación
/// consumidora (AppCode de PadLock). Cada app define qué campos recibe; así la app no mapea ni lee más
/// de lo que usa. Agregar una app nueva es agregar su proyección en <see cref="GetAsync"/>.</summary>
public sealed class AppOrderQueryService : IAppOrderQueryService
{
    /// <summary>AppCode de SalesPlace en PadLock.</summary>
    public const string SalesPlaceAppCode = "SP";

    // Líneas y unidades se completan después con la regla única (OrderLineTotals), igual que el listado.
    private static readonly Expression<Func<SalesOrder, SalesPlaceOrderListItem>> SalesPlaceProjection = order =>
        new SalesPlaceOrderListItem
        {
            Id = order.Id,
            DocNum = order.DocNum,
            CardCode = order.CardCode,
            CardName = order.CardName,
            ChannelCode = order.ChannelCode,
            ChannelName = order.ChannelName,
            ApprovalDate = order.ApprovalDate
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
        string appCode = TextHelper.RequiredUpper(query.AppCode, "Debe indicar el código de la aplicación (appCode).");
        string sourceSystem = ValidateSourceSystem(query.SourceSystem);
        // Solo pedidos aprobados: se usa la situación (no ApprovalDate, que no existe en pedidos
        // aprobados antes de que se empezara a guardar).
        string approvedSituationCode = OrderSituationCodes.ApprovedFor(sourceSystem)
            ?? throw new AppException($"El sistema de origen '{sourceSystem}' no tiene definido un estado de pedido aprobado.");
        // Por número de pedido basta appCode + sourceSystem: fechas y demás filtros se ignoran.
        string? docNum = TextHelper.TrimOrNull(query.DocNum);
        if (docNum is null) ValidateDateRange(query.DateFrom, query.DateTo);
        ValidationHelper.ThrowIfInvalidPage(query.PageNumber, query.PageSize);

        return appCode switch
        {
            SalesPlaceAppCode => await SalesPlacePageAsync(
                BuildFilter(query, sourceSystem, approvedSituationCode, docNum, ChannelCodes(_options.SalesPlace)),
                query.PageNumber, query.PageSize, cancellationToken),
            _ => throw new AppException(
                $"La aplicación '{appCode}' no tiene un listado de pedidos disponible. Aplicaciones habilitadas: {SalesPlaceAppCode}.")
        };
    }

    // El canal aplica siempre, también al buscar por número: un pedido de otro canal no existe para la app.
    private static AppOrderFilter BuildFilter(AppOrderQuery query, string sourceSystem, string situationCode,
        string? docNum, IReadOnlyCollection<string> channelCodes) =>
        docNum is null
            ? new AppOrderFilter(sourceSystem, situationCode, null, query.DateFrom, query.DateTo,
                TextHelper.TrimOrNull(query.CardCode), TextHelper.TrimOrNull(query.CardName), channelCodes)
            : new AppOrderFilter(sourceSystem, situationCode, docNum, null, null, null, null, channelCodes);

    private static IReadOnlyCollection<string> ChannelCodes(AppOrderAppOptions app) =>
        app.ChannelCodes.Select(code => code?.Trim()).OfType<string>().Where(code => code.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

    private async Task<AppOrderPage<SalesPlaceOrderListItem>> SalesPlacePageAsync(AppOrderFilter filter, int pageNumber,
        int pageSize, CancellationToken cancellationToken)
    {
        AppOrderPage<SalesPlaceOrderListItem> page = await PageAsync(filter, pageNumber, pageSize, SalesPlaceProjection, cancellationToken);
        Dictionary<Guid, OrderLineTotalsRow> totals = await _orders.GetLineTotalsAsync(
            page.Items.Select(item => item.Id).ToArray(), cancellationToken);
        foreach (SalesPlaceOrderListItem item in page.Items)
            if (totals.TryGetValue(item.Id, out OrderLineTotalsRow? row))
            {
                item.TotalItems = row.TotalItems;
                item.TotalUnits = row.TotalUnits;
            }
        return page;
    }

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
        string sourceSystem = TextHelper.RequiredUpper(value, "Debe indicar el sistema de origen (sourceSystem).");
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
}
