using System.Linq.Expressions;
using ReyplastCoreAPI.Domain.Orders.Entities;
using ReyplastCoreAPI.Shared.Helpers;

namespace ReyplastCoreAPI.Domain.Orders;

/// <summary>Líneas y unidades que se muestran de un pedido.</summary>
public sealed class OrderLineTotalsRow
{
    public Guid OrderId { get; set; }
    public int TotalItems { get; set; }
    public decimal TotalUnits { get; set; }
}

/// <summary>
/// Única regla de líneas y unidades de un pedido en Core (listado, detalle por id, pedido completo y listado
/// de SalesPlace): si el pedido ya tiene su detalle en Core, se cuenta desde el detalle (líneas activas;
/// unidades = cantidad - suspendida, nunca negativa por línea); si no, se usa el resumen que informó OSIS en
/// la sincronización de cabeceras (SalesOrder.SourceLineCount / SourceTotalUnits), calculado con la misma
/// regla (ver OsisLineSummary).
/// </summary>
public static class OrderLineTotals
{
    /// <summary>Para consultas EF: se traduce a SQL.</summary>
    public static readonly Expression<Func<SalesOrder, OrderLineTotalsRow>> Projection = order => new OrderLineTotalsRow
    {
        OrderId = order.Id,
        TotalItems = order.Details.Any()
            ? order.Details.Count(detail => detail.IsActive)
            : order.SourceLineCount ?? 0,
        TotalUnits = order.Details.Any()
            ? order.Details.Where(detail => detail.IsActive)
                .Sum(detail => (decimal?)(detail.Quantity >= detail.SuspendedQuantity
                    ? detail.Quantity - detail.SuspendedQuantity : 0m)) ?? 0m
            : order.SourceTotalUnits ?? 0m
    };

    /// <summary>Para un pedido ya cargado con su colección Details.</summary>
    public static void Apply(SalesOrder order)
    {
        if (order.Details.Count == 0)
        {
            order.TotalItems = order.SourceLineCount ?? 0;
            order.TotalUnits = order.SourceTotalUnits ?? 0m;
            return;
        }
        order.TotalItems = order.Details.Count(detail => detail.IsActive);
        order.TotalUnits = order.Details.Where(detail => detail.IsActive)
            .Sum(detail => QuantityHelper.Available(detail.Quantity, detail.SuspendedQuantity));
    }
}
