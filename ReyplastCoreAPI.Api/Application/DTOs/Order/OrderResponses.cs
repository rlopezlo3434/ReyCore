namespace ReyplastCoreAPI.Application.DTOs.Order;

/// <summary>Cabecera de pedido; el detalle se consulta por separado.</summary>
public sealed class OrderResponse
{
    public Guid Id { get; set; }
    public string DocNum { get; set; } = string.Empty;
    public DateTime DocDate { get; set; }
    /// <summary>Fecha de aprobación real del pedido en el origen (OSIS: PPC_FECAPR). Null si el
    /// pedido todavía no fue aprobado.</summary>
    public DateTime? ApprovalDate { get; set; }
    public string CardCode { get; set; } = string.Empty;
    public string CardName { get; set; } = string.Empty;
    public string SituationCode { get; set; } = string.Empty;
    public string SituationDescription { get; set; } = string.Empty;
    public decimal DocTotal { get; set; }
    public string? ChannelCode { get; set; }
    public string? ChannelName { get; set; }
    public string? PaymentConditionCode { get; set; }
    public string? PaymentConditionName { get; set; }
    public int TotalItems { get; set; }
    public decimal? TotalUnits { get; set; }
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
    public decimal SuspendedQuantity { get; set; }
    public decimal Price { get; set; }
    public decimal LineTotal { get; set; }
    public bool IsActive { get; set; }
    public Guid? SyncExecutionId { get; set; }
    public DateTime? LastSynchronizedAt { get; set; }
}

public sealed class OrderChannelCountResponse
{
    public string? ChannelCode { get; set; }
    public string? ChannelName { get; set; }
    public int Count { get; set; }
}

/// <summary>Pedido completo compuesto por su cabecera y todas sus líneas.</summary>
public sealed class CompleteOrderResponse
{
    public OrderResponse Header { get; set; } = new();
    public IReadOnlyList<OrderDetailResponse> Details { get; set; } = Array.Empty<OrderDetailResponse>();
}

/// <summary>Resultado del refresco de UN pedido contra su origen. <see cref="Changed"/> es true solo
/// si cambió algo real (cabecera o líneas); un pedido idéntico solo se "toca".</summary>
public sealed record OrderDocumentSyncResult(bool Found, bool Changed)
{
    public static OrderDocumentSyncResult NotFound { get; } = new(false, false);
    public static OrderDocumentSyncResult Synced(bool changed) => new(true, changed);
}

public sealed class OrderSyncAcceptedResponse
{
    public Guid ExecutionId { get; set; }
    public string Status { get; set; } = string.Empty;
    /// <summary>Id del job en Hangfire -- su avance se consulta en /hangfire/jobs/details/{JobId}.</summary>
    public string JobId { get; set; } = string.Empty;
}

/// <summary>Rango inclusivo solicitado para importar pedidos desde un origen.</summary>
public sealed class OrderSyncRangeRequest
{
    /// <summary>OSIS o SAP (ver <see cref="ReyplastCoreAPI.Domain.Catalog.SourceSystems"/>). El caller
    /// (ej. MasterLogicApi_V2) define esto en su propia configuración -- no hace falta una ruta
    /// distinta por origen.</summary>
    public string SourceSystem { get; set; } = ReyplastCoreAPI.Domain.Catalog.SourceSystems.Osis;
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
}
