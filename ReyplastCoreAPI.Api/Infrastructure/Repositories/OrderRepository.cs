using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using ReyplastCoreAPI.Application.DTOs.Product.Response;
using ReyplastCoreAPI.Application.Interfaces.Repositories;
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
        var pageItems = await query.OrderByDescending(order => order.DocDate)
            .ThenByDescending(order => order.DocNum)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(order => new { Order = order, TotalItems = order.Details.Count })
            .ToListAsync(cancellationToken);
        List<SalesOrder> items = pageItems.Select(item =>
        {
            item.Order.TotalItems = item.TotalItems;
            return item.Order;
        }).ToList();
        return Page(items, pageNumber, pageSize, total);
    }

    public async Task<SalesOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        SalesOrder? order = await _context.Orders.AsNoTracking().Include(item => item.Situation)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (order is not null)
            order.TotalItems = await _context.OrderDetails.AsNoTracking()
                .CountAsync(detail => detail.OrderId == id, cancellationToken);
        return order;
    }

    public async Task<IReadOnlyList<SalesOrder>> GetCompleteByDocNumAsync(string docNum,
        CancellationToken cancellationToken) =>
        await _context.Orders.AsNoTracking()
            .Include(order => order.Situation)
            .Include(order => order.Details)
            .AsSplitQuery()
            .Where(order => order.DocNum == docNum)
            .ToListAsync(cancellationToken);

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
            .Where(order => docNums.Contains(order.DocNum)).Include(order => order.Details)
            .AsSplitQuery().ToListAsync(cancellationToken);
        HashSet<string> requested = keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return candidates.Where(order => requested.Contains($"{order.BranchCode}|{order.DocNum}")).ToList();
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
