using ReyplastCoreAPI.Domain.Common;
using ReyplastCoreAPI.Domain.Catalog;

namespace ReyplastCoreAPI.Domain.Catalog.Entities;

/// <summary>Producto del catálogo local, sincronizado o creado manualmente.</summary>
public class Product : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Clasificación normalizada definida en ProductTypes.</summary>
    public string Type { get; set; } = string.Empty;
    /// <summary>Código único procedente de prd_codprd o asignado manualmente.</summary>
    public string GenericCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    /// <summary>OSIS, SAP o MANUAL.</summary>
    public string SourceSystem { get; set; } = SourceSystems.Manual;
    /// <summary>Última ejecución externa que observó el producto; nulo para MANUAL.</summary>
    public Guid? SyncExecutionId { get; set; }
    /// <summary>Fecha en que fue observado por última vez en el origen.</summary>
    public DateTime? LastSynchronizedAt { get; set; }
    public ICollection<Color> Colors { get; set; } = new List<Color>();
}
