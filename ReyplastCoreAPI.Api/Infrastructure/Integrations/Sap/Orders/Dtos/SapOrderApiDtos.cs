using System.Text.Json.Serialization;

namespace ReyplastCoreAPI.Infrastructure.Integrations.Sap.Orders.Dtos;

/// <summary>Contrato exacto retornado por GET api/pedidos/completo.</summary>
public sealed class SapOrderApiDto
{
    [JsonPropertyName("DocEntry")]
    public long DocEntry { get; set; }
    [JsonPropertyName("DocNum")]
    public long DocNum { get; set; }
    [JsonPropertyName("DocDate")]
    public DateTime DocDate { get; set; }
    [JsonPropertyName("CardCode")]
    public string? CardCode { get; set; }
    [JsonPropertyName("CardName")]
    public string? CardName { get; set; }
    [JsonPropertyName("DocStatus")]
    public string? DocStatus { get; set; }
    [JsonPropertyName("DocTotal")]
    public decimal DocTotal { get; set; }
    [JsonPropertyName("CodCanal")]
    public string? ChannelCode { get; set; }
    [JsonPropertyName("NombreCanal")]
    public string? ChannelName { get; set; }
    [JsonPropertyName("TotalItems")]
    public int TotalItems { get; set; }
    [JsonPropertyName("Detalle")]
    public List<SapOrderDetailApiDto>? Details { get; set; }
}

public sealed class SapOrderDetailApiDto
{
    [JsonPropertyName("DocEntry")]
    public long DocEntry { get; set; }
    [JsonPropertyName("DocNum")]
    public long DocNum { get; set; }
    [JsonPropertyName("ItemCode")]
    public string? ItemCode { get; set; }
    [JsonPropertyName("Dscription")]
    public string? Description { get; set; }
    [JsonPropertyName("Quantity")]
    public decimal Quantity { get; set; }
    [JsonPropertyName("Price")]
    public decimal Price { get; set; }
    [JsonPropertyName("LineTotal")]
    public decimal LineTotal { get; set; }
}
