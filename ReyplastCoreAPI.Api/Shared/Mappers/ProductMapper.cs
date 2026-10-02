using ReyplastCoreAPI.Application.DTOs.Product.Response;
using ReyplastCoreAPI.Domain.Catalog.Entities;

namespace ReyplastCoreAPI.Shared.Mappers;

public static class ProductMapper
{
    public static ProductResponse ToResponse(Product product) => new()
    {
        Id = product.Id,
        Type = product.Type,
        GenericCode = product.GenericCode,
        Description = product.Description,
        IsActive = product.IsActive,
        SourceSystem = product.SourceSystem,
        SyncExecutionId = product.SyncExecutionId,
        LastSynchronizedAt = product.LastSynchronizedAt,
        CreatedAt = product.CreatedAt,
        ModifiedAt = product.ModifiedAt,
        Colors = product.Colors.OrderBy(color => color.Code).Select(ToResponse).ToList()
    };

    public static ColorResponse ToResponse(Color color) => new()
    {
        Id = color.Id,
        Code = color.Code,
        ColorDescription = color.ColorDescription,
        IsSaleEnabled = color.IsSaleEnabled,
        IsActive = color.IsActive,
        SyncExecutionId = color.SyncExecutionId,
        LastSynchronizedAt = color.LastSynchronizedAt
    };
}
