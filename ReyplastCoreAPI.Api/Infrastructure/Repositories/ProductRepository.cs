using Microsoft.EntityFrameworkCore;
using ReyplastCoreAPI.Application.DTOs.Product.Response;
using ReyplastCoreAPI.Application.Interfaces.Repositories;
using ReyplastCoreAPI.Domain.Catalog.Entities;
using ReyplastCoreAPI.Infrastructure.Persistence;

namespace ReyplastCoreAPI.Infrastructure.Repositories;

public sealed class ProductRepository : IProductRepository
{
    private readonly ReyplastCoreDbContext _context;

    public ProductRepository(ReyplastCoreDbContext context) => _context = context;

    public async Task<PaginatedResponse<Product>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? search,
        IReadOnlyCollection<string>? productTypes,
        bool? isActive,
        string? sourceSystem)
    {
        IQueryable<Product> query = _context.Products.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            string pattern = $"%{search.Trim()}%";
            query = query.Where(product =>
                EF.Functions.Like(product.GenericCode, pattern)
                || (product.Description != null && EF.Functions.Like(product.Description, pattern))
                || product.Colors.Any(color =>
                    (color.Code != null && EF.Functions.Like(color.Code, pattern))
                    || (color.ColorDescription != null && EF.Functions.Like(color.ColorDescription, pattern))));
        }
        if (productTypes is { Count: > 0 })
            query = query.Where(product => productTypes.Contains(product.Type));
        if (isActive.HasValue)
            query = query.Where(product => product.IsActive == isActive.Value);
        if (!string.IsNullOrWhiteSpace(sourceSystem))
            query = query.Where(product => product.SourceSystem == sourceSystem);

        int totalRecords = await query.CountAsync();
        List<Product> items = await query
            .OrderBy(product => product.GenericCode)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Include(product => product.Colors)
            .AsSplitQuery()
            .ToListAsync();

        return new PaginatedResponse<Product>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalRecords = totalRecords,
            TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize)
        };
    }

    public Task<Product?> GetByIdAsync(Guid id, bool asTracking = false) =>
        BuildProductQuery(asTracking).FirstOrDefaultAsync(product => product.Id == id);

    public Task<Product?> GetByGenericCodeAsync(string genericCode, bool asTracking = false) =>
        BuildProductQuery(asTracking).FirstOrDefaultAsync(product => product.GenericCode == genericCode);

    public Task<List<Product>> GetByGenericCodesAsync(IReadOnlyCollection<string> genericCodes) =>
        _context.Products
            .AsNoTracking()
            .Where(product => genericCodes.Contains(product.GenericCode))
            .Include(product => product.Colors)
            .AsSplitQuery()
            .ToListAsync();

    public Task<bool> GenericCodeExistsAsync(string genericCode, Guid? excludingId = null) =>
        _context.Products.AnyAsync(product =>
            product.GenericCode == genericCode
            && (!excludingId.HasValue || product.Id != excludingId.Value));

    public Task AddAsync(Product product) => _context.Products.AddAsync(product).AsTask();

    public Task SaveChangesAsync() => _context.SaveChangesAsync();

    private IQueryable<Product> BuildProductQuery(bool asTracking)
    {
        IQueryable<Product> query = _context.Products.Include(product => product.Colors).AsSplitQuery();
        return asTracking ? query : query.AsNoTracking();
    }
}
