namespace ReyplastCoreAPI.Domain.Catalog;

/// <summary>Sistemas admitidos como procedencia de un producto.</summary>
public static class SourceSystems
{
    /// <summary>Producto leído desde la base XIMESA de OSIS.</summary>
    public const string Osis = "OSIS";
    /// <summary>Producto proveniente de una futura integración SAP.</summary>
    public const string Sap = "SAP";
    /// <summary>Producto creado directamente mediante la API.</summary>
    public const string Manual = "MANUAL";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(
        new[] { Osis, Sap, Manual },
        StringComparer.OrdinalIgnoreCase);
}
