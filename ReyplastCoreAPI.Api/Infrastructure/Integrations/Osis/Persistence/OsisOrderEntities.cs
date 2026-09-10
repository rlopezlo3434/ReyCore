namespace ReyplastCoreAPI.Infrastructure.Integrations.Osis.Persistence;

/// <summary>Cabecera XIMESA.dbo.PEDIDOS_VENTA_PPC.</summary>
internal sealed class OsisOrderEntity
{
    public string CompanyCode { get; set; } = string.Empty;
    public string BranchCode { get; set; } = string.Empty;
    public string DocNum { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string CardCode { get; set; } = string.Empty;
    public string StatusCode { get; set; } = string.Empty;
    public decimal DocTotal { get; set; }
    public string ActiveCode { get; set; } = string.Empty;
}

/// <summary>Detalle XIMESA.dbo.PEDIDOS_VENTA_PPD.</summary>
internal sealed class OsisOrderDetailEntity
{
    public string CompanyCode { get; set; } = string.Empty;
    public string BranchCode { get; set; } = string.Empty;
    public string DocNum { get; set; } = string.Empty;
    public string ItemCode { get; set; } = string.Empty;
    public string LineNumber { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal LineTotal { get; set; }
    public string ActiveCode { get; set; } = string.Empty;
}

internal sealed class OsisAuxiliaryEntity
{
    public string CompanyCode { get; set; } = string.Empty;
    public string CardCode { get; set; } = string.Empty;
    public string CardName { get; set; } = string.Empty;
}

internal sealed class OsisClientCompanyEntity
{
    public string CompanyCode { get; set; } = string.Empty;
    public string CardCode { get; set; } = string.Empty;
    public int? ChannelGroupId { get; set; }
}

internal sealed class OsisChannelEntity
{
    public int Id { get; set; }
    public string? ChannelCode { get; set; }
    public string? ChannelName { get; set; }
}
