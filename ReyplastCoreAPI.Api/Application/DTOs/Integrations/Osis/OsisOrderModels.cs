using ReyplastCoreAPI.Shared.Helpers;

namespace ReyplastCoreAPI.Application.DTOs.Integrations.Osis;

public sealed class OsisOrderPage
{
    public bool HasMore { get; init; }
    public string? NextCursor { get; init; }
    public IReadOnlyList<OsisOrderRecord> Orders { get; init; } = Array.Empty<OsisOrderRecord>();
}

public sealed class OsisOrderRecord
{
    public string BranchCode { get; init; } = string.Empty;
    public string DocNum { get; init; } = string.Empty;
    public DateTime DocDate { get; init; }
    public string CardCode { get; init; } = string.Empty;
    public string CardName { get; init; } = string.Empty;
    public string SituationCode { get; init; } = string.Empty;
    public decimal DocTotal { get; init; }
    public string? ChannelCode { get; init; }
    public string? ChannelName { get; init; }
    public string? PaymentConditionCode { get; init; }
    public string? PaymentConditionName { get; init; }
    /// <summary>PPC_FECACT -- fecha de última actualización de la cabecera en OSIS.</summary>
    public DateTime ModifiedAt { get; init; }
    /// <summary>PPC_FECAPR -- fecha de aprobacion real (confirmado empiricamente, ver comentario en
    /// OsisOrderEntity.ApprovalDate). Null si el pedido todavia no fue aprobado.</summary>
    public DateTime? ApprovalDate { get; init; }
    public bool IsActive { get; init; }
    /// <summary>Resumen de PPD (líneas activas y unidades disponibles). En la sincronización masiva se
    /// obtiene con una consulta agrupada, sin traer las filas del detalle; null si no se calculó.</summary>
    public int? LineCount { get; set; }
    public decimal? TotalUnits { get; set; }
    public IReadOnlyList<OsisOrderDetailRecord> Details { get; set; } = Array.Empty<OsisOrderDetailRecord>();
}

/// <summary>
/// Única definición del resumen de un pedido, igual a como el listado cuenta el detalle guardado en Core:
/// líneas = líneas activas (PPD_INDEST activo); unidades = suma de (cantidad - suspendida) de esas líneas,
/// nunca negativa por línea. La consulta agrupada de la sincronización masiva (OsisOrderReader) aplica la
/// misma regla en SQL. Si cambia aquí, cambiar también allá y en el listado (OrderRepository/OrderService).
/// </summary>
public static class OsisLineSummary
{
    public static void ApplyFromDetails(OsisOrderRecord order)
    {
        List<OsisOrderDetailRecord> active = order.Details.Where(detail => detail.IsActive).ToList();
        order.LineCount = active.Count;
        order.TotalUnits = active.Sum(detail => QuantityHelper.Available(detail.Quantity, detail.SuspendedQuantity));
    }
}

public sealed class OsisOrderDetailRecord
{
    public string BranchCode { get; init; } = string.Empty;
    public string DocNum { get; init; } = string.Empty;
    public string LineNumber { get; init; } = string.Empty;
    public string ItemCode { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal Quantity { get; init; }
    public decimal SuspendedQuantity { get; init; }
    public decimal Price { get; init; }
    public decimal LineTotal { get; init; }
    public bool IsActive { get; init; }
}
