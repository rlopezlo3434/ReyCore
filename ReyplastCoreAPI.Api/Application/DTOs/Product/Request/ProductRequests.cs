namespace ReyplastCoreAPI.Application.DTOs.Product.Request;

/// <summary>Datos para crear un producto local con origen MANUAL.</summary>
public sealed class ProductCreateRequest
{
    /// <summary>PT-SIMPLE, SIMPLE-PARTE, INTERMEDIO, PT-COMPUESTO, PT-SET o NO-DEFINIDO.</summary>
    public string Type { get; set; } = string.Empty;
    /// <summary>Código único del producto local.</summary>
    public string GenericCode { get; set; } = string.Empty;
    /// <summary>Descripción comercial opcional.</summary>
    public string? Description { get; set; }
    /// <summary>Conjunto completo de colores que quedará asociado al producto.</summary>
    public List<ProductColorRequest> Colors { get; set; } = new();
}

/// <summary>Datos para reemplazar un producto MANUAL y sus colores.</summary>
public sealed class ProductUpdateRequest
{
    /// <summary>Nueva clasificación normalizada del producto MANUAL.</summary>
    public string Type { get; set; } = string.Empty;
    /// <summary>Nuevo código único del producto MANUAL.</summary>
    public string GenericCode { get; set; } = string.Empty;
    /// <summary>Nueva descripción comercial.</summary>
    public string? Description { get; set; }
    /// <summary>Conjunto completo de colores; los omitidos se inactivan.</summary>
    public List<ProductColorRequest> Colors { get; set; } = new();
}

/// <summary>Color administrado localmente para un producto MANUAL.</summary>
public sealed class ProductColorRequest
{
    /// <summary>Código de color único dentro del producto.</summary>
    public string? Code { get; set; }
    /// <summary>Descripción comercial del color.</summary>
    public string? ColorDescription { get; set; }
    /// <summary>Indica si la variante puede venderse.</summary>
    public bool IsSaleEnabled { get; set; }
}

/// <summary>Solicitud de habilitación o inhabilitación de un producto MANUAL.</summary>
public sealed class StatusChangeRequest
{
    /// <summary>true para habilitar el producto MANUAL; false para inhabilitarlo.</summary>
    public bool IsActive { get; set; }
}
