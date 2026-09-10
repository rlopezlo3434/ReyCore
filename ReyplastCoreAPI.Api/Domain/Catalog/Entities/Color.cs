using ReyplastCoreAPI.Domain.Common;

namespace ReyplastCoreAPI.Domain.Catalog.Entities;

/// <summary>Variante de color asociada a un producto.</summary>
public class Color : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? ColorDescription { get; set; }
    /// <summary>En OSIS es true cuando PRODUCTOS_COLORES_PCO.pco_venta vale S.</summary>
    public bool IsSaleEnabled { get; set; }
    /// <summary>Última ejecución externa que observó la relación producto-color.</summary>
    public Guid? SyncExecutionId { get; set; }
    /// <summary>Fecha de la observación más reciente en el origen.</summary>
    public DateTime? LastSynchronizedAt { get; set; }
    public Product Product { get; set; } = null!;
}
