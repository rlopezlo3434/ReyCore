namespace ReyplastCoreAPI.Domain.Catalog;

/// <summary>Códigos de situación de pedido (catálogo orders.OrderSituations) que tienen un significado
/// de negocio fijo.</summary>
public static class OrderSituationCodes
{
    /// <summary>OSIS: "PEDIDO APROBADO". Mismo criterio que usa Gestión de Pickers en MasterLogic.</summary>
    public const string OsisApproved = "2";

    /// <summary>Código de situación "aprobado" del origen, o null si ese origen no tiene uno definido
    /// (SAP solo maneja abierto/cerrado).</summary>
    public static string? ApprovedFor(string sourceSystem) =>
        string.Equals(sourceSystem, SourceSystems.Osis, StringComparison.OrdinalIgnoreCase) ? OsisApproved : null;
}
