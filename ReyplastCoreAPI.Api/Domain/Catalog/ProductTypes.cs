namespace ReyplastCoreAPI.Domain.Catalog;

/// <summary>Clasificación normalizada derivada de PRODUCTOS_PRD.prd_indset y prd_prduni.</summary>
public static class ProductTypes
{
    /// <summary>Producto simple unitario: prd_indset=0 y prd_prduni=1.</summary>
    public const string FinishedSimple = "PT-SIMPLE";
    /// <summary>Parte simple no unitaria: prd_indset=0 y prd_prduni distinto de 1.</summary>
    public const string SimplePart = "SIMPLE-PARTE";
    /// <summary>Producto intermedio: prd_indset=1.</summary>
    public const string Intermediate = "INTERMEDIO";
    /// <summary>Producto terminado compuesto: prd_indset=2.</summary>
    public const string FinishedCompound = "PT-COMPUESTO";
    /// <summary>Producto terminado tipo set: prd_indset=3.</summary>
    public const string FinishedSet = "PT-SET";
    /// <summary>Valor de prd_indset no reconocido por la integración.</summary>
    public const string Undefined = "NO-DEFINIDO";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(
        new[] { FinishedSimple, SimplePart, Intermediate, FinishedCompound, FinishedSet, Undefined },
        StringComparer.OrdinalIgnoreCase);
}
