using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using ReyplastCoreAPI.Application.DTOs.Order;
using ReyplastCoreAPI.Application.DTOs.Product.Response;
using ReyplastCoreAPI.Application.Interfaces.Repositories;
using ReyplastCoreAPI.Domain.Catalog;
using ReyplastCoreAPI.Domain.Orders;
using ReyplastCoreAPI.Domain.Orders.Entities;
using ReyplastCoreAPI.Infrastructure.Persistence;
using ReyplastCoreAPI.Shared.Helpers;

namespace ReyplastCoreAPI.Infrastructure.Repositories;

public sealed class OrderRepository : IOrderRepository
{
    private readonly ReyplastCoreDbContext _context;
    public OrderRepository(ReyplastCoreDbContext context) => _context = context;

    public async Task<PaginatedResponse<SalesOrder>> GetPagedAsync(int pageNumber, int pageSize,
        IReadOnlyCollection<string> search, IReadOnlyCollection<string> situationCodes,
        IReadOnlyCollection<string> channelCodes, DateTime? dateFrom, DateTime? dateTo,
        IReadOnlyCollection<string> sourceSystems, CancellationToken cancellationToken)
    {
        IQueryable<SalesOrder> query = _context.Orders.AsNoTracking().Include(order => order.Situation);
        if (search.Count > 0) query = query.Where(BuildSearchPredicate(search));
        if (situationCodes.Count > 0)
            query = query.Where(order => situationCodes.Contains(order.SituationCode));
        if (channelCodes.Count > 0)
            query = query.Where(order => order.ChannelCode != null && channelCodes.Contains(order.ChannelCode));
        if (dateFrom.HasValue) query = query.Where(order => order.DocDate >= dateFrom.Value.Date);
        if (dateTo.HasValue)
        {
            DateTime toExclusive = DateHelper.EndExclusive(dateTo.Value);
            query = query.Where(order => order.DocDate < toExclusive);
        }
        if (sourceSystems.Count > 0)
            query = query.Where(order => sourceSystems.Contains(order.SourceSystem));

        int total = await query.CountAsync(cancellationToken);
        // Gestion de Pickers (el unico consumidor conocido de este endpoint) solo lista pedidos ya
        // Aprobados -- para esos, ordenar/mostrar por fecha de creacion no tenia sentido operativo
        // (la fecha relevante para un picker es cuando el pedido quedo habilitado, no cuando se
        // digito). Se usa ApprovalDate con fallback a DocDate solo por si algun pedido llegara sin
        // aprobar (no deberia darse con el filtro de situationCode, pero evita que un ApprovalDate
        // nulo lo mande al tope/fondo de la pagina de forma imprevisible).
        List<SalesOrder> items = await query.OrderByDescending(order => order.ApprovalDate ?? order.DocDate)
            .ThenByDescending(order => order.DocNum)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        // Una linea anulada en OSIS (PPD_INDEST inactivo) sigue en Details -- se necesita para poder
        // mostrarla marcada como anulada en el detalle -- pero no cuenta como parte real del pedido.
        Dictionary<Guid, OrderLineTotalsRow> totals = await GetLineTotalsAsync(items.Select(order => order.Id).ToArray(), cancellationToken);
        foreach (SalesOrder order in items)
            if (totals.TryGetValue(order.Id, out OrderLineTotalsRow? row))
            {
                order.TotalItems = row.TotalItems;
                order.TotalUnits = row.TotalUnits;
            }
        return Page(items, pageNumber, pageSize, total);
    }

    // Regla única: ver OrderLineTotals.
    public async Task<Dictionary<Guid, OrderLineTotalsRow>> GetLineTotalsAsync(IReadOnlyCollection<Guid> orderIds,
        CancellationToken cancellationToken)
    {
        if (orderIds.Count == 0) return new Dictionary<Guid, OrderLineTotalsRow>();
        Guid[] ids = orderIds.Distinct().ToArray();
        return await _context.Orders.AsNoTracking()
            .Where(order => ids.Contains(order.Id))
            .Select(OrderLineTotals.Projection)
            .ToDictionaryAsync(row => row.OrderId, cancellationToken);
    }

    public async Task<(IReadOnlyList<TItem> Items, int Total)> GetOrdersByApplicationCodeAsync<TItem>(AppOrderFilter filter,
        int pageNumber, int pageSize, Expression<Func<SalesOrder, TItem>> projection, CancellationToken cancellationToken)
    {
        IQueryable<SalesOrder> query = _context.Orders.AsNoTracking()
            .Where(order => order.SourceSystem == filter.SourceSystem && order.IsActive
                && order.SituationCode == filter.SituationCode);
        if (filter.ChannelCodes.Count > 0)
        {
            IReadOnlyCollection<string> channelCodes = filter.ChannelCodes;
            query = query.Where(order => order.ChannelCode != null && channelCodes.Contains(order.ChannelCode));
        }
        if (filter.DateFrom.HasValue)
        {
            DateTime from = filter.DateFrom.Value.Date;
            query = query.Where(order => order.DocDate >= from);
        }
        if (filter.DateTo.HasValue)
        {
            DateTime toExclusive = DateHelper.EndExclusive(filter.DateTo.Value);
            query = query.Where(order => order.DocDate < toExclusive);
        }
        // Prefijo: sargable sobre IX_Orders_Source_DocNum (un LIKE '%x%' recorreria toda la tabla).
        if (filter.DocNum is not null)
        {
            string docNumPrefix = filter.DocNum;
            query = query.Where(order => order.DocNum.StartsWith(docNumPrefix));
        }
        if (filter.CardCode is not null)
        {
            string cardCodePrefix = filter.CardCode;
            query = query.Where(order => order.CardCode.StartsWith(cardCodePrefix));
        }
        // Contiene: el nombre se escribe parcial ("TOTTUS"); el rango de fechas ya acota las filas.
        if (filter.CardName is not null)
        {
            string cardNameText = filter.CardName;
            query = query.Where(order => order.CardName.Contains(cardNameText));
        }
        int total = await query.CountAsync(cancellationToken);
        List<TItem> items = await query
            .OrderByDescending(order => order.DocDate).ThenByDescending(order => order.DocNum)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(projection)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<SalesOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        SalesOrder? order = await _context.Orders.AsNoTracking().Include(item => item.Situation)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (order is not null
            && (await GetLineTotalsAsync(new[] { id }, cancellationToken)).TryGetValue(id, out OrderLineTotalsRow? row))
        {
            order.TotalItems = row.TotalItems;
            order.TotalUnits = row.TotalUnits;
        }
        return order;
    }

    public async Task<IReadOnlyList<SalesOrder>> GetCompleteByDocNumAsync(string docNum,
        string? sourceSystem, CancellationToken cancellationToken)
    {
        IQueryable<SalesOrder> query = _context.Orders.AsNoTracking()
            .Include(order => order.Situation)
            .Include(order => order.Details)
            .AsSplitQuery()
            .Where(order => order.DocNum == docNum);
        if (!string.IsNullOrWhiteSpace(sourceSystem))
            query = query.Where(order => order.SourceSystem == sourceSystem);
        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SalesOrder>> GetCompleteByDocNumsAsync(
        IReadOnlyCollection<string> docNums, string? sourceSystem, CancellationToken cancellationToken)
    {
        if (docNums.Count == 0) return Array.Empty<SalesOrder>();
        IQueryable<SalesOrder> query = _context.Orders.AsNoTracking()
            .Include(order => order.Situation)
            .Include(order => order.Details)
            .AsSplitQuery()
            .Where(order => docNums.Contains(order.DocNum));
        if (!string.IsNullOrWhiteSpace(sourceSystem))
            query = query.Where(order => order.SourceSystem == sourceSystem);
        return await query.ToListAsync(cancellationToken);
    }

    public async Task<PaginatedResponse<SalesOrderDetail>> GetDetailsAsync(Guid orderId, int pageNumber,
        int pageSize, CancellationToken cancellationToken)
    {
        IQueryable<SalesOrderDetail> query = _context.OrderDetails.AsNoTracking()
            .Where(detail => detail.OrderId == orderId);
        int total = await query.CountAsync(cancellationToken);
        List<SalesOrderDetail> items = await query.OrderBy(detail => detail.LineNumber)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return Page(items, pageNumber, pageSize, total);
    }

    public async Task<List<SalesOrder>> GetByKeysAsync(IReadOnlyCollection<string> keys,
        bool includeDetails, CancellationToken cancellationToken)
    {
        string[] docNums = keys.Select(OrderSourceKey.DocNum).Distinct().ToArray();
        IQueryable<SalesOrder> query = _context.Orders.AsNoTracking()
            // Esta llave BranchCode|DocNum pertenece al contrato OSIS. El filtro evita que
            // una coincidencia de DocNum de otro origen participe en el upsert de XIMESA.
            .Where(order => order.SourceSystem == SourceSystems.Osis
                && docNums.Contains(order.DocNum));
        // La sincronización masiva solo compara cabeceras: cargar las líneas guardadas sería trabajo perdido.
        if (includeDetails) query = query.Include(order => order.Details).AsSplitQuery();
        List<SalesOrder> candidates = await query.ToListAsync(cancellationToken);
        HashSet<string> requested = keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return candidates.Where(order => requested.Contains($"{order.BranchCode}|{order.DocNum}")).ToList();
    }

    public async Task<List<SalesOrder>> GetBySourceDocumentIdsAsync(string sourceSystem,
        IReadOnlyCollection<string> sourceDocumentIds, CancellationToken cancellationToken)
    {
        string[] ids = sourceDocumentIds.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        return await _context.Orders.AsNoTracking()
            .Where(order => order.SourceSystem == sourceSystem && order.SourceDocumentId != null
                && ids.Contains(order.SourceDocumentId))
            .Include(order => order.Details)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<(string? ChannelCode, string? ChannelName, int Count)>> GetChannelCountsAsync(
        IReadOnlyCollection<string> search, IReadOnlyCollection<string> situationCodes,
        DateTime? dateFrom, DateTime? dateTo, IReadOnlyCollection<string> sourceSystems,
        CancellationToken cancellationToken)
    {
        IQueryable<SalesOrder> query = _context.Orders.AsNoTracking();
        if (search.Count > 0) query = query.Where(BuildSearchPredicate(search));
        if (situationCodes.Count > 0)
            query = query.Where(order => situationCodes.Contains(order.SituationCode));
        if (dateFrom.HasValue) query = query.Where(order => order.DocDate >= dateFrom.Value.Date);
        if (dateTo.HasValue)
        {
            DateTime toExclusive = DateHelper.EndExclusive(dateTo.Value);
            query = query.Where(order => order.DocDate < toExclusive);
        }
        if (sourceSystems.Count > 0)
            query = query.Where(order => sourceSystems.Contains(order.SourceSystem));

        var counts = await query
            .GroupBy(order => new { order.ChannelCode, order.ChannelName })
            .Select(group => new { group.Key.ChannelCode, group.Key.ChannelName, Count = group.Count() })
            .ToListAsync(cancellationToken);
        return counts.Select(item => (item.ChannelCode, item.ChannelName, item.Count)).ToList();
    }

    private static PaginatedResponse<T> Page<T>(List<T> items, int pageNumber, int pageSize, int total) => new()
    {
        Items = items, PageNumber = pageNumber, PageSize = pageSize, TotalRecords = total,
        TotalPages = (int)Math.Ceiling((double)total / pageSize)
    };

    private static Expression<Func<SalesOrder, bool>> BuildSearchPredicate(IEnumerable<string> search)
    {
        ParameterExpression order = Expression.Parameter(typeof(SalesOrder), "order");
        Expression predicate = Expression.Constant(false);
        foreach (string term in search)
        {
            Expression value = Expression.Constant(term);
            Expression matches = Expression.OrElse(
                Expression.Call(Expression.Property(order, nameof(SalesOrder.DocNum)), nameof(string.Contains), null, value),
                Expression.OrElse(
                    Expression.Call(Expression.Property(order, nameof(SalesOrder.CardCode)), nameof(string.Contains), null, value),
                    Expression.Call(Expression.Property(order, nameof(SalesOrder.CardName)), nameof(string.Contains), null, value)));
            predicate = Expression.OrElse(predicate, matches);
        }
        return Expression.Lambda<Func<SalesOrder, bool>>(predicate, order);
    }
}
