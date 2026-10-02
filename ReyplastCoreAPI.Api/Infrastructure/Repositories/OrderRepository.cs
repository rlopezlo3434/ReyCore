using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using ReyplastCoreAPI.Application.DTOs.Order;
using ReyplastCoreAPI.Application.DTOs.Product.Response;
using ReyplastCoreAPI.Application.Interfaces.Repositories;
using ReyplastCoreAPI.Domain.Catalog;
using ReyplastCoreAPI.Domain.Orders.Entities;
using ReyplastCoreAPI.Infrastructure.Persistence;

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
        if (dateTo.HasValue) query = query.Where(order => order.DocDate < dateTo.Value.Date.AddDays(1));
        if (sourceSystems.Count > 0)
            query = query.Where(order => sourceSystems.Contains(order.SourceSystem));

        int total = await query.CountAsync(cancellationToken);
        // Gestion de Pickers (el unico consumidor conocido de este endpoint) solo lista pedidos ya
        // Aprobados -- para esos, ordenar/mostrar por fecha de creacion no tenia sentido operativo
        // (la fecha relevante para un picker es cuando el pedido quedo habilitado, no cuando se
        // digito). Se usa ApprovalDate con fallback a DocDate solo por si algun pedido llegara sin
        // aprobar (no deberia darse con el filtro de situationCode, pero evita que un ApprovalDate
        // nulo lo mande al tope/fondo de la pagina de forma imprevisible).
        var pageItems = await query.OrderByDescending(order => order.ApprovalDate ?? order.DocDate)
            .ThenByDescending(order => order.DocNum)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            // Una linea anulada en OSIS (PPD_INDEST inactivo) sigue en Details -- se necesita para
            // poder mostrarla marcada como anulada en el detalle -- pero no cuenta como parte real
            // del pedido en el listado (antes inflaba "Lineas" con lineas que ya no aplican).
            .Select(order => new {
                Order = order,
                TotalItems = order.Details.Count(detail => detail.IsActive),
                TotalUnits = order.Details.Where(detail => detail.IsActive)
                    .Sum(detail => (decimal?)(detail.Quantity >= detail.SuspendedQuantity
                        ? detail.Quantity - detail.SuspendedQuantity : 0m)) ?? 0m
            })
            .ToListAsync(cancellationToken);
        List<SalesOrder> items = pageItems.Select(item =>
        {
            item.Order.TotalItems = item.TotalItems;
            item.Order.TotalUnits = item.TotalUnits;
            return item.Order;
        }).ToList();
        return Page(items, pageNumber, pageSize, total);
    }

    public async Task<(IReadOnlyList<TItem> Items, int Total)> GetOrdersByApplicationCodeAsync<TItem>(AppOrderFilter filter,
        int pageNumber, int pageSize, Expression<Func<SalesOrder, TItem>> projection, CancellationToken cancellationToken)
    {
        IQueryable<SalesOrder> query = _context.Orders.AsNoTracking()
            .Where(order => order.SourceSystem == filter.SourceSystem && order.IsActive
                && order.SituationCode == filter.SituationCode);
        if (filter.DateFrom.HasValue)
        {
            DateTime from = filter.DateFrom.Value.Date;
            query = query.Where(order => order.DocDate >= from);
        }
        if (filter.DateTo.HasValue)
        {
            DateTime toExclusive = filter.DateTo.Value.Date.AddDays(1);
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
        if (order is not null)
            order.TotalItems = await _context.OrderDetails.AsNoTracking()
                .CountAsync(detail => detail.OrderId == id && detail.IsActive, cancellationToken);
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
        CancellationToken cancellationToken)
    {
        string[] docNums = keys.Select(key => key.Split('|', 2)[1]).Distinct().ToArray();
        List<SalesOrder> candidates = await _context.Orders.AsNoTracking()
            // Esta llave BranchCode|DocNum pertenece al contrato OSIS. El filtro evita que
            // una coincidencia de DocNum de otro origen participe en el upsert de XIMESA.
            .Where(order => order.SourceSystem == SourceSystems.Osis
                && docNums.Contains(order.DocNum))
            .Include(order => order.Details)
            .AsSplitQuery().ToListAsync(cancellationToken);
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
        if (dateTo.HasValue) query = query.Where(order => order.DocDate < dateTo.Value.Date.AddDays(1));
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
