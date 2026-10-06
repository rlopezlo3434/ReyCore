using ReyplastCoreAPI.Application.DTOs.Order;
using ReyplastCoreAPI.Application.DTOs.Product.Response;
using ReyplastCoreAPI.Domain.Orders.Entities;

namespace ReyplastCoreAPI.Application.Interfaces.Repositories;

public interface IOrderRepository
{
    Task<PaginatedResponse<SalesOrder>> GetPagedAsync(int pageNumber, int pageSize,
        IReadOnlyCollection<string> search, IReadOnlyCollection<string> situationCodes,
        IReadOnlyCollection<string> channelCodes, DateTime? dateFrom, DateTime? dateTo,
        IReadOnlyCollection<string> sourceSystems, CancellationToken cancellationToken);
    Task<SalesOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Listado por aplicación consumidora: filtra por origen, situación y los filtros opcionales
    /// que traiga <paramref name="filter"/>; proyecta en SQL solo los campos que pide
    /// <paramref name="projection"/>, sin cargar entidades ni líneas.</summary>
    Task<(IReadOnlyList<TItem> Items, int Total)> GetOrdersByApplicationCodeAsync<TItem>(AppOrderFilter filter,
        int pageNumber, int pageSize,
        System.Linq.Expressions.Expression<Func<SalesOrder, TItem>> projection, CancellationToken cancellationToken);
    Task<IReadOnlyList<SalesOrder>> GetCompleteByDocNumAsync(string docNum, string? sourceSystem,
        CancellationToken cancellationToken);
    /// <summary>Version batch de GetCompleteByDocNumAsync -- misma forma de datos (cabecera+detalle),
    /// una sola consulta con WHERE DocNum IN (...) en vez de N consultas individuales.</summary>
    Task<IReadOnlyList<SalesOrder>> GetCompleteByDocNumsAsync(IReadOnlyCollection<string> docNums,
        string? sourceSystem, CancellationToken cancellationToken);
    Task<PaginatedResponse<SalesOrderDetail>> GetDetailsAsync(Guid orderId, int pageNumber, int pageSize,
        CancellationToken cancellationToken);
    // includeDetails = false para la sincronización masiva (solo cabeceras).
    Task<List<SalesOrder>> GetByKeysAsync(IReadOnlyCollection<string> keys, bool includeDetails,
        CancellationToken cancellationToken);
    // Líneas y unidades a mostrar por pedido (regla única en Domain.Orders.OrderLineTotals).
    Task<Dictionary<Guid, ReyplastCoreAPI.Domain.Orders.OrderLineTotalsRow>> GetLineTotalsAsync(
        IReadOnlyCollection<Guid> orderIds, CancellationToken cancellationToken);
    Task<List<SalesOrder>> GetBySourceDocumentIdsAsync(string sourceSystem,
        IReadOnlyCollection<string> sourceDocumentIds, CancellationToken cancellationToken);

    /// <summary>Cantidad de pedidos por canal, respetando los mismos filtros que GetPagedAsync
    /// salvo channelCodes (el conteo es justamente para poblar ese selector).</summary>
    Task<List<(string? ChannelCode, string? ChannelName, int Count)>> GetChannelCountsAsync(
        IReadOnlyCollection<string> search, IReadOnlyCollection<string> situationCodes,
        DateTime? dateFrom, DateTime? dateTo, IReadOnlyCollection<string> sourceSystems,
        CancellationToken cancellationToken);
}
