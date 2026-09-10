namespace ReyplastCoreAPI.Domain.Catalog;

/// <summary>Códigos de color con un tratamiento funcional especial en el catálogo.</summary>
public static class CatalogColorCodes
{
    /// <summary>
    /// 505A representa SURTIDO: no es un color concreto y por eso no se almacena
    /// como variante en ReyplastCoreDB.
    /// </summary>
    public const string Assorted = "505A";
}
