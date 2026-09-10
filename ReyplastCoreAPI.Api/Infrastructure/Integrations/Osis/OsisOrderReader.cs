using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ReyplastCoreAPI.Application.DTOs.Integrations.Osis;
using ReyplastCoreAPI.Application.Interfaces.Integrations;
using ReyplastCoreAPI.Infrastructure.Integrations.Osis.Persistence;

namespace ReyplastCoreAPI.Infrastructure.Integrations.Osis;

/// <summary>Lee cabeceras PPC y detalles PPD directamente desde XIMESA.</summary>
internal sealed class OsisOrderReader : IOsisOrderReader
{
    private const int SourcePageSize = 500;
    private readonly OsisDbContext _context;
    private readonly OsisOptions _options;

    public OsisOrderReader(OsisDbContext context, IOptions<OsisOptions> options)
    {
        _context = context;
        _options = options.Value;
    }

    public async Task<OsisOrderPage> ReadPageAsync(
        string? cursor,
        CancellationToken cancellationToken = default)
    {
        (string? afterDocNum, string? afterBranch) = ParseCursor(cursor);
        IQueryable<OsisOrderEntity> source = _context.Orders.AsNoTracking()
            .Where(order => order.CompanyCode == OsisCatalogCodes.ReyplastCompany);
        if (afterDocNum is not null)
        {
            source = source.Where(order =>
                string.Compare(order.DocNum, afterDocNum) < 0
                || (order.DocNum == afterDocNum && string.Compare(order.BranchCode, afterBranch) < 0));
        }

        List<OsisOrderRecord> orders = await ProjectHeaders(source)
            .OrderByDescending(order => order.DocNum)
            .ThenByDescending(order => order.BranchCode)
            .Take(SourcePageSize + 1)
            .ToListAsync(cancellationToken);

        bool hasMore = orders.Count > SourcePageSize;
        if (hasMore) orders.RemoveAt(orders.Count - 1);
        if (orders.Count > 0) await LoadDetailsAsync(orders, cancellationToken);

        OsisOrderRecord? last = orders.LastOrDefault();
        return new OsisOrderPage
        {
            HasMore = hasMore,
            NextCursor = last is null ? null : BuildCursor(last.DocNum, last.BranchCode),
            Orders = orders
        };
    }

    /// <summary>Busca exclusivamente un PPC y sus PPD para la carga bajo demanda.</summary>
    public async Task<IReadOnlyList<OsisOrderRecord>> ReadByDocNumAsync(
        string docNum,
        CancellationToken cancellationToken = default)
    {
        IQueryable<OsisOrderEntity> source = _context.Orders.AsNoTracking()
            .Where(order => order.CompanyCode == OsisCatalogCodes.ReyplastCompany
                && order.DocNum == docNum);
        List<OsisOrderRecord> orders = await ProjectHeaders(source)
            .OrderBy(order => order.BranchCode)
            .ToListAsync(cancellationToken);
        if (orders.Count > 0) await LoadDetailsAsync(orders, cancellationToken);
        return orders;
    }

    private IQueryable<OsisOrderRecord> ProjectHeaders(IQueryable<OsisOrderEntity> source) =>
        from order in source
        join auxiliary in _context.Auxiliaries.AsNoTracking()
            on new { order.CompanyCode, order.CardCode }
            equals new { auxiliary.CompanyCode, auxiliary.CardCode } into auxiliaryJoin
        from auxiliary in auxiliaryJoin.DefaultIfEmpty()
        join client in _context.ClientCompanies.AsNoTracking()
            on new { order.CompanyCode, order.CardCode }
            equals new { client.CompanyCode, client.CardCode } into clientJoin
        from client in clientJoin.DefaultIfEmpty()
        join channel in _context.Channels.AsNoTracking()
            on client.ChannelGroupId equals (int?)channel.Id into channelJoin
        from channel in channelJoin.DefaultIfEmpty()
        select new OsisOrderRecord
        {
            BranchCode = order.BranchCode.Trim(),
            DocNum = order.DocNum.Trim(),
            // DocDate fue definido funcionalmente como la fecha de creación PPC_FECCRE.
            DocDate = order.CreatedAt,
            CardCode = order.CardCode.Trim(),
            CardName = auxiliary == null ? string.Empty : auxiliary.CardName.Trim(),
            SituationCode = order.StatusCode.Trim(),
            DocTotal = order.DocTotal,
            ChannelCode = channel == null || channel.ChannelCode == null
                ? null : channel.ChannelCode.Trim(),
            ChannelName = channel == null || channel.ChannelName == null
                ? null : channel.ChannelName.Trim(),
            IsActive = order.ActiveCode == _options.ActiveStatusCode
        };

    private async Task LoadDetailsAsync(
        List<OsisOrderRecord> orders,
        CancellationToken cancellationToken)
    {
        string[] docNums = orders.Select(order => order.DocNum).Distinct().ToArray();
        HashSet<string> pageKeys = orders
            .Select(order => BuildOrderKey(order.BranchCode, order.DocNum))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        List<OsisOrderDetailRecord> details = await (
            from detail in _context.OrderDetails.AsNoTracking()
            join product in _context.Products.AsNoTracking()
                on new { detail.CompanyCode, detail.ItemCode }
                equals new { product.CompanyCode, ItemCode = product.ProductCode } into productJoin
            from product in productJoin.DefaultIfEmpty()
            where detail.CompanyCode == OsisCatalogCodes.ReyplastCompany
                  && docNums.Contains(detail.DocNum)
            orderby detail.DocNum, detail.BranchCode, detail.LineNumber
            select new OsisOrderDetailRecord
            {
                BranchCode = detail.BranchCode.Trim(),
                DocNum = detail.DocNum.Trim(),
                LineNumber = detail.LineNumber.Trim(),
                ItemCode = detail.ItemCode.Trim(),
                Description = product == null ? null : product.ProductDescription.Trim(),
                Quantity = detail.Quantity,
                Price = detail.Price,
                LineTotal = detail.LineTotal,
                IsActive = detail.ActiveCode == _options.ActiveStatusCode
            }).ToListAsync(cancellationToken);

        Dictionary<string, IReadOnlyList<OsisOrderDetailRecord>> byOrder = details
            .Where(detail => pageKeys.Contains(BuildOrderKey(detail.BranchCode, detail.DocNum)))
            .GroupBy(detail => BuildOrderKey(detail.BranchCode, detail.DocNum), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<OsisOrderDetailRecord>)group.ToList(),
                StringComparer.OrdinalIgnoreCase);
        foreach (OsisOrderRecord order in orders)
            order.Details = byOrder.TryGetValue(BuildOrderKey(order.BranchCode, order.DocNum), out var list)
                ? list : Array.Empty<OsisOrderDetailRecord>();
    }

    private static string BuildCursor(string docNum, string branchCode) => $"{docNum}|{branchCode}";
    private static string BuildOrderKey(string branchCode, string docNum) => $"{branchCode}|{docNum}";

    private static (string? DocNum, string? Branch) ParseCursor(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor)) return (null, null);
        string[] parts = cursor.Split('|', 2);
        return parts.Length == 2 ? (parts[0], parts[1]) : (cursor, string.Empty);
    }
}
