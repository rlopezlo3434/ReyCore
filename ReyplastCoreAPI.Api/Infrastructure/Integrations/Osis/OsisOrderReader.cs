using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ReyplastCoreAPI.Application.DTOs.Integrations.Osis;
using ReyplastCoreAPI.Application.Interfaces.Integrations;
using ReyplastCoreAPI.Infrastructure.Integrations.Osis.Persistence;

namespace ReyplastCoreAPI.Infrastructure.Integrations.Osis;

/// <summary>Lee cabeceras PPC y detalles PPD directamente desde XIMESA.</summary>
internal sealed class OsisOrderReader : IOsisOrderReader
{
    private readonly OsisDbContext _context;
    private readonly OsisOptions _options;
    private readonly ILogger<OsisOrderReader> _logger;

    public OsisOrderReader(OsisDbContext context, IOptions<OsisOptions> options,
        ILogger<OsisOrderReader> logger)
    {
        _context = context;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<OsisOrderPage> ReadRangePageAsync(DateTime dateFrom, DateTime dateTo,
        string? cursor, CancellationToken cancellationToken = default)
    {
        DateTime from = dateFrom.Date;
        DateTime exclusiveDateTo = dateTo.Date.AddDays(1);
        IQueryable<OsisOrderEntity> source = _context.Orders.AsNoTracking()
            .Where(order => order.CompanyCode == OsisCatalogCodes.ReyplastCompany
                && order.CreatedAt >= from
                && order.CreatedAt < exclusiveDateTo);
        return await ReadPageAsync(source, cursor, cancellationToken);
    }

    public async Task<OsisOrderPage> ReadApprovedDayPageAsync(DateTime date, string? cursor,
        CancellationToken cancellationToken = default)
    {
        string approved = _options.ApprovedSituationCode;
        // Timeout propio (por defecto 300 s): el DbContext es del scope del job, así que solo afecta a esta
        // sincronización (cabeceras y líneas), no a las demás consultas a OSIS.
        _context.Database.SetCommandTimeout(TimeSpan.FromSeconds(Math.Max(_options.ApprovedDayCommandTimeoutSeconds, 30)));
        DateTime from = date.Date;
        DateTime exclusiveTo = from.AddDays(1);
        // Aprobados ESE día por fecha de aprobación (PPC_FECAPR), no por fecha de creación: un pedido creado
        // ayer y aprobado hoy también entra. El límite de creación solo acota la búsqueda en OSIS (usa la
        // fecha de creación, igual que el sync por rango); no amplía lo que se sincroniza.
        DateTime createdFrom = from.AddDays(-Math.Max(_options.ApprovedDayCreationLookbackDays, 1));
        IQueryable<OsisOrderEntity> source = _context.Orders.AsNoTracking()
            .Where(order => order.CompanyCode == OsisCatalogCodes.ReyplastCompany
                && order.CreatedAt >= createdFrom
                && order.CreatedAt < exclusiveTo
                && order.ApprovalDate >= from
                && order.ApprovalDate < exclusiveTo
                && order.StatusCode == approved);
        return await ReadPageAsync(source, cursor, cancellationToken);
    }

    private async Task<OsisOrderPage> ReadPageAsync(IQueryable<OsisOrderEntity> source,
        string? cursor, CancellationToken cancellationToken)
    {
        (string? afterDocNum, string? afterBranch) = ParseCursor(cursor);
        if (afterDocNum is not null)
        {
            source = source.Where(order =>
                string.Compare(order.DocNum, afterDocNum) < 0
                || (order.DocNum == afterDocNum && string.Compare(order.BranchCode, afterBranch) < 0));
        }

        List<OsisOrderRecord> orders = await ProjectHeaders(source)
            .OrderByDescending(order => order.DocNum)
            .ThenByDescending(order => order.BranchCode)
            .Take(_options.OrderPageSize + 1)
            .ToListAsync(cancellationToken);

        bool hasMore = orders.Count > _options.OrderPageSize;
        if (hasMore) orders.RemoveAt(orders.Count - 1);
        if (orders.Count > 0) await LoadDetailsAsync(orders, cursor, cancellationToken);

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
        if (orders.Count > 0) await LoadDetailsAsync(orders, $"doc:{docNum}", cancellationToken);
        return orders;
    }

    private IQueryable<OsisOrderRecord> ProjectHeaders(IQueryable<OsisOrderEntity> source) =>
        from order in source
        join auxiliary in _context.Auxiliaries.AsNoTracking()
            on new { order.CompanyCode, order.CardCode }
            equals new { auxiliary.CompanyCode, auxiliary.CardCode } into auxiliaryJoin
        from auxiliary in auxiliaryJoin.DefaultIfEmpty()
        join priceList in _context.PriceLists.AsNoTracking()
            on new { order.CompanyCode, Code = order.TlpCode }
            equals new { priceList.CompanyCode, Code = (string?)priceList.Code } into priceListJoin
        from priceList in priceListJoin.DefaultIfEmpty()
        join paymentCondition in _context.PaymentConditions.AsNoTracking()
            on order.PaymentConditionCode equals paymentCondition.Code into paymentConditionJoin
        from paymentCondition in paymentConditionJoin.DefaultIfEmpty()
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
            ChannelCode = priceList == null ? null : priceList.Code.Trim(),
            ChannelName = priceList == null || priceList.Name == null
                ? null : priceList.Name.Trim(),
            PaymentConditionCode = order.PaymentConditionCode == null
                ? null : order.PaymentConditionCode!.Trim(),
            PaymentConditionName = paymentCondition == null || paymentCondition.Name == null
                ? null : paymentCondition.Name.Trim(),
            ModifiedAt = order.ModifiedAt,
            ApprovalDate = order.ApprovalDate,
            IsActive = order.ActiveCode == _options.ActiveStatusCode
        };

    private async Task LoadDetailsAsync(
        List<OsisOrderRecord> orders,
        string? cursor,
        CancellationToken cancellationToken)
    {
        HashSet<string> pageKeys = orders
            .Select(order => BuildOrderKey(order.BranchCode, order.DocNum))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        List<OsisOrderDetailRecord> details = [];

        foreach (IGrouping<string, OsisOrderRecord> branch in orders.GroupBy(
                     order => order.BranchCode,
                     StringComparer.OrdinalIgnoreCase))
        {
            string[] branchDocNums = branch
                .Select(order => order.DocNum)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            foreach (string[] docNumBatch in branchDocNums.Chunk(_options.OrderDetailBatchSize))
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                _logger.LogInformation(
                    "Consultando líneas OSIS. Sucursal {BranchCode}; documentos {DocumentCount}; cursor {Cursor}.",
                    branch.Key,
                    docNumBatch.Length,
                    cursor ?? "inicio");

                try
                {
                    List<OsisOrderDetailRecord> batchDetails = await (
                        from detail in _context.OrderDetails.AsNoTracking()
                        join product in _context.Products.AsNoTracking()
                            on new { detail.CompanyCode, detail.ItemCode }
                            equals new { product.CompanyCode, ItemCode = product.ProductCode } into productJoin
                        from product in productJoin.DefaultIfEmpty()
                        where detail.CompanyCode == OsisCatalogCodes.ReyplastCompany
                              && detail.BranchCode == branch.Key
                              && docNumBatch.Contains(detail.DocNum)
                        select new OsisOrderDetailRecord
                        {
                            BranchCode = detail.BranchCode.Trim(),
                            DocNum = detail.DocNum.Trim(),
                            LineNumber = detail.LineNumber.Trim(),
                            ItemCode = detail.ItemCode.Trim(),
                            Description = product == null ? null : product.ProductDescription.Trim(),
                            Quantity = detail.Quantity,
                            SuspendedQuantity = detail.SuspendedQuantity ?? 0m,
                            Price = detail.Price,
                            LineTotal = detail.LineTotal,
                            IsActive = detail.ActiveCode == _options.ActiveStatusCode
                        }).ToListAsync(cancellationToken);

                    details.AddRange(batchDetails);
                    _logger.LogInformation(
                        "Líneas OSIS consultadas. Sucursal {BranchCode}; documentos {DocumentCount}; líneas {LineCount}; cursor {Cursor}; duración {ElapsedMilliseconds} ms.",
                        branch.Key,
                        docNumBatch.Length,
                        batchDetails.Count,
                        cursor ?? "inicio",
                        stopwatch.ElapsedMilliseconds);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    _logger.LogError(
                        exception,
                        "Falló la consulta de líneas OSIS. Sucursal {BranchCode}; documentos {DocumentCount}; cursor {Cursor}; duración {ElapsedMilliseconds} ms.",
                        branch.Key,
                        docNumBatch.Length,
                        cursor ?? "inicio",
                        stopwatch.ElapsedMilliseconds);
                    throw;
                }
            }
        }

        Dictionary<string, IReadOnlyList<OsisOrderDetailRecord>> byOrder = details
            .Where(detail => pageKeys.Contains(BuildOrderKey(detail.BranchCode, detail.DocNum)))
            .GroupBy(detail => BuildOrderKey(detail.BranchCode, detail.DocNum), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<OsisOrderDetailRecord>)group
                    .OrderBy(detail => detail.LineNumber, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(detail => detail.ItemCode, StringComparer.OrdinalIgnoreCase)
                    .ToList(),
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
