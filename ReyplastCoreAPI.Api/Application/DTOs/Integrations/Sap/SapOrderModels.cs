namespace ReyplastCoreAPI.Application.DTOs.Integrations.Sap;

/// <summary>Configuración de persistencia masiva SAP; no se recibe desde el endpoint.</summary>
public sealed class SapOrderSyncOptions
{
    public const string SectionName = "SapOrdersApi";
    public int DatabaseBatchSize { get; set; }
}

/// <summary>Pedido SAP normalizado antes de persistirlo en RP_ReyPlastCoreBD.</summary>
public sealed class SapOrderRecord
{
    public string SourceDocumentId { get; init; } = string.Empty;
    public string DocNum { get; init; } = string.Empty;
    public DateTime DocDate { get; init; }
    public string CardCode { get; init; } = string.Empty;
    public string CardName { get; init; } = string.Empty;
    public string SituationCode { get; init; } = string.Empty;
    public decimal DocTotal { get; init; }
    public string? ChannelCode { get; init; }
    public string? ChannelName { get; init; }
    public IReadOnlyList<SapOrderDetailRecord> Details { get; init; } = Array.Empty<SapOrderDetailRecord>();
}

/// <summary>Línea SAP; el contrato externo actual no proporciona LineNum.</summary>
public sealed class SapOrderDetailRecord
{
    public string ItemCode { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal Quantity { get; init; }
    public decimal Price { get; init; }
    public decimal LineTotal { get; init; }
}
