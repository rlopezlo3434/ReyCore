using ReyplastCoreAPI.Domain.Common;

namespace ReyplastCoreAPI.Domain.Orders.Entities;

/// <summary>Cabecera local de un pedido de venta sincronizado.</summary>
public sealed class SalesOrder : AuditableEntity
{
    public Guid Id { get; set; }
    /// <summary>Identificador técnico del documento en el sistema de origen. No forma parte del contrato público.</summary>
    public string? SourceDocumentId { get; set; }
    /// <summary>Sucursal de OSIS; puede ser null para orígenes que no proporcionan este dato, como SAP.</summary>
    public string? BranchCode { get; set; }
    public string DocNum { get; set; } = string.Empty;
    public DateTime DocDate { get; set; }
    public string CardCode { get; set; } = string.Empty;
    public string CardName { get; set; } = string.Empty;
    public string SituationCode { get; set; } = string.Empty;
    public decimal DocTotal { get; set; }
    public string? ChannelCode { get; set; }
    public string? ChannelName { get; set; }
    public string SourceSystem { get; set; } = string.Empty;
    public Guid? SyncExecutionId { get; set; }
    public DateTime? LastSynchronizedAt { get; set; }
    /// <summary>Cantidad calculada de líneas; no se persiste en la cabecera.</summary>
    public int TotalItems { get; set; }
    public OrderSituation Situation { get; set; } = null!;
    public ICollection<SalesOrderDetail> Details { get; set; } = new List<SalesOrderDetail>();
}

/// <summary>Catálogo de situaciones de pedidos separado por sistema de origen.</summary>
public sealed class OrderSituation
{
    public string SourceSystem { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ICollection<SalesOrder> Orders { get; set; } = new List<SalesOrder>();
}

/// <summary>Línea local perteneciente a una cabecera de pedido.</summary>
public sealed class SalesOrderDetail : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    /// <summary>Identificador técnico de la línea en el origen. No se expone en el contrato público.</summary>
    public string SourceDetailId { get; set; } = string.Empty;
    /// <summary>Número de línea cuando el origen lo proporciona; el API SAP actual no lo incluye.</summary>
    public string? LineNumber { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal LineTotal { get; set; }
    public Guid? SyncExecutionId { get; set; }
    public DateTime? LastSynchronizedAt { get; set; }
    public SalesOrder Order { get; set; } = null!;
}
