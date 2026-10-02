namespace ReyplastCoreAPI.Infrastructure.Integrations.Sap.Orders;

/// <summary>Configuración del API HTTP que expone los pedidos SAP.</summary>
public sealed class SapOrdersApiOptions
{
    public const string SectionName = "SapOrdersApi";
    public string BaseUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; }
    public int RangeWindowDays { get; set; }
}
