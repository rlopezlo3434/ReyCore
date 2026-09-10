using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ReyplastCoreAPI.Application.DTOs.Integrations.Osis;
using ReyplastCoreAPI.Application.Interfaces.Integrations;
using ReyplastCoreAPI.Domain.Catalog;
using ReyplastCoreAPI.Infrastructure.Integrations.Osis.Persistence;

namespace ReyplastCoreAPI.Infrastructure.Integrations.Osis;

internal sealed class OsisCatalogReader : IOsisCatalogReader
{
    // 500 mantiene pequeñas las consultas de colores y evita cargar todo XIMESA en memoria.
    private const int SourcePageSize = 500;
    private readonly OsisDbContext _context;
    private readonly OsisOptions _options;

    public OsisCatalogReader(OsisDbContext context, IOptions<OsisOptions> options)
    {
        _context = context;
        _options = options.Value;
    }

    public async Task<OsisCatalogPage> ReadPageAsync(
        string? afterProductCode,
        CancellationToken cancellationToken = default)
    {
        IQueryable<OsisProductEntity> source = _context.Products
            .AsNoTracking()
            .Where(product => product.CompanyCode == OsisCatalogCodes.ReyplastCompany
                              && product.CatalogTypeCode == OsisCatalogCodes.FinishedProductInventory);

        if (!string.IsNullOrWhiteSpace(afterProductCode))
            source = source.Where(product => string.Compare(product.ProductCode, afterProductCode) > 0);

        List<OsisProductRecord> products = await source
            .OrderBy(product => product.ProductCode)
            .Select(product => new OsisProductRecord
            {
                ProductCode = product.ProductCode.Trim(),
                ProductDescription = product.ProductDescription.Trim(),
                ProductType = product.ProductTypeCode == OsisCatalogCodes.Simple
                    ? product.IsUnitary == OsisCatalogCodes.UnitaryProduct
                        ? ProductTypes.FinishedSimple
                        : ProductTypes.SimplePart
                    : product.ProductTypeCode == OsisCatalogCodes.Intermediate ? ProductTypes.Intermediate
                    : product.ProductTypeCode == OsisCatalogCodes.Compound ? ProductTypes.FinishedCompound
                    : product.ProductTypeCode == OsisCatalogCodes.Set ? ProductTypes.FinishedSet
                    : ProductTypes.Undefined,
                IsActive = product.ProductStatusCode == _options.ActiveStatusCode,
                CreatedAt = product.CreatedAt,
            })
            .Take(SourcePageSize + 1)
            .ToListAsync(cancellationToken);

        bool hasMore = products.Count > SourcePageSize;
        if (hasMore) products.RemoveAt(products.Count - 1);

        if (products.Count > 0)
            await LoadColorsAsync(products, cancellationToken);

        return new OsisCatalogPage
        {
            HasMore = hasMore,
            NextProductCode = products.Count == 0 ? null : products[^1].ProductCode,
            Products = products
        };
    }

    private async Task LoadColorsAsync(
        List<OsisProductRecord> products,
        CancellationToken cancellationToken)
    {
        string[] productCodes = products.Select(product => product.ProductCode).ToArray();

        List<OsisColorRecord> colors = await (
            from productColor in _context.ProductColors.AsNoTracking()
            join color in _context.Colors.AsNoTracking()
                on productColor.ColorCode equals color.ColorCode
            where productColor.CompanyCode == OsisCatalogCodes.ReyplastCompany
                  && productCodes.Contains(productColor.ProductCode)
                  && productColor.ColorCode != CatalogColorCodes.Assorted
            orderby productColor.ProductCode, productColor.ColorCode
            select new OsisColorRecord
            {
                ProductCode = productColor.ProductCode.Trim(),
                ColorCode = productColor.ColorCode.Trim(),
                ColorDescription = color.ColorDescription.Trim(),
                IsActive = productColor.StatusCode == _options.ActiveStatusCode
                    && color.StatusCode == _options.ActiveStatusCode,
                IsSaleEnabled = productColor.SaleCode == _options.EnabledSaleCode,
            }).ToListAsync(cancellationToken);

        Dictionary<string, IReadOnlyList<OsisColorRecord>> colorsByProduct = colors
            .GroupBy(color => color.ProductCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<OsisColorRecord>)group.ToList(),
                StringComparer.OrdinalIgnoreCase);

        foreach (OsisProductRecord product in products)
            product.Colors = colorsByProduct.TryGetValue(product.ProductCode, out IReadOnlyList<OsisColorRecord>? list)
                ? list
                : Array.Empty<OsisColorRecord>();
    }
}
