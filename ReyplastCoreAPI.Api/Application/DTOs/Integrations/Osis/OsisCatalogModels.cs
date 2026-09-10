namespace ReyplastCoreAPI.Application.DTOs.Integrations.Osis;

/// <summary>Página leída desde OSIS mediante cursor por código de producto.</summary>
public sealed class OsisCatalogPage
{
    public bool HasMore { get; init; }
    public string? NextProductCode { get; init; }
    public IReadOnlyList<OsisProductRecord> Products { get; init; } = Array.Empty<OsisProductRecord>();
}

/// <summary>Producto OSIS ya normalizado, independiente de nombres físicos de XIMESA.</summary>
public sealed class OsisProductRecord
{
    public string ProductCode { get; init; } = string.Empty;
    public string ProductDescription { get; init; } = string.Empty;
    public string ProductType { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime? CreatedAt { get; init; }
    public IReadOnlyList<OsisColorRecord> Colors { get; set; } = Array.Empty<OsisColorRecord>();
}

/// <summary>Color asociado a un producto y normalizado desde PCO y COL.</summary>
public sealed class OsisColorRecord
{
    public string ProductCode { get; init; } = string.Empty;
    public string ColorCode { get; init; } = string.Empty;
    public string ColorDescription { get; init; } = string.Empty;
    public bool IsSaleEnabled { get; init; }
    public bool IsActive { get; init; }
}
