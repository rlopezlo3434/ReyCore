namespace ReyplastCoreAPI.Shared.Helpers;

/// <summary>Cantidades de línea de pedido.</summary>
public static class QuantityHelper
{
    /// <summary>Unidades disponibles de una línea: cantidad pedida - suspendida (PPD_CANSUS), nunca negativa.
    /// En consultas EF la misma regla va escrita en la expresión (OrderLineTotals.Projection,
    /// OsisOrderReader.LineSummaryQuery), porque EF no traduce métodos propios a SQL.</summary>
    public static decimal Available(decimal quantity, decimal suspendedQuantity) =>
        Math.Max(0m, quantity - suspendedQuantity);
}
