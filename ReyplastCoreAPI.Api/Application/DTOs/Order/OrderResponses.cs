namespace ReyplastCoreAPI.Application.DTOs.Order;

/// <summary>Cabecera de pedido; el detalle se consulta por separado.</summary>
public sealed class OrderResponse
{
    public Guid Id { get; set; }
    public string DocNum { get; set; } = string.Empty;
    public DateTime DocDate { get; set; }
    public string CardCode { get; set; } = string.Empty;
    public string CardName { get; set; } = string.Empty;
    public string SituationCode { get; set; } = string.Empty;
    public string SituationDescription { get; set; } = string.Empty;
    public decimal DocTotal { get; set; }
    public string? ChannelCode { get; set; }
    public string? ChannelName { get; set; }
    public int TotalItems { get; set; }
    public bool IsActive { get; set; }
    public string SourceSystem { get; set; } = string.Empty;
    public Guid? SyncExecutionId { get; set; }
    public DateTime? LastSynchronizedAt { get; set; }
}

/// <summary>Detalle paginable de un pedido.</summary>
public sealed class OrderDetailResponse
{
    public Guid Id { get; set; }
    /// <summary>Número informado por el origen; es null cuando el origen no lo proporciona.</summary>
    public string? LineNumber { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal LineTotal { get; set; }
    public bool IsActive { get; set; }
    public Guid? SyncExecutionId { get; set; }
    public DateTime? LastSynchronizedAt { get; set; }
}

/// <summary>Pedido completo compuesto por su cabecera y todas sus líneas.</summary>
public sealed class CompleteOrderResponse
{
    public OrderResponse Header { get; set; } = new();
    public IReadOnlyList<OrderDetailResponse> Details { get; set; } = Array.Empty<OrderDetailResponse>();
}

public sealed class OrderSyncAcceptedResponse
{
    public Guid ExecutionId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusUrl { get; set; } = string.Empty;
}
