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
    public string? PaymentConditionCode { get; set; }
    public string? TlpCode { get; set; }
    /// <summary>PPC_FECACT: fecha de última actualización de la cabecera.
    /// CONFIRMADO (2026-09-21, sobre 704,932 pedidos aprobados reales): la aprobacion NO
    /// necesariamente actualiza este campo -- el 99.3% de los pedidos aprobados tienen
    /// PPC_FECACT anterior a PPC_FECAPR. PPC_FECACT queda fijado por un proceso automatico post-
    /// creacion (ocurre casi al mismo segundo que PPC_FECAPP, no relacionado a la aprobacion
    /// humana).</summary>
    public DateTime ModifiedAt { get; set; }
    /// <summary>PPC_FECAPR: fecha de aprobacion real del pedido -- CONFIRMADO empiricamente (no
    /// solo por el nombre): correlaciona 1 a 1 con PPC_USUAPR (usuario que aprobo) en los 5
    /// grupos de PPC_SITPPC muestreados, y siempre es NULL en pedidos sin aprobar (SITPPC=1).
    /// PPC_FECAPP (con doble P, existe como columna separada) NO es esto -- se llena a los pocos
    /// segundos de creado el pedido incluso en pedidos SIN aprobar, es un timestamp de
    /// procesamiento automatico distinto, no la aprobacion humana.</summary>
    public DateTime? ApprovalDate { get; set; }
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
    public decimal? SuspendedQuantity { get; set; }
    public decimal Price { get; set; }
    public decimal LineTotal { get; set; }
    public string ActiveCode { get; set; } = string.Empty;
    /// <summary>PPD_FECACT: fecha de última actualización de esta línea; es independiente de
    /// PPC_FECACT porque el trigger que la sella en PPD no toca la cabecera.</summary>
    public DateTime ModifiedAt { get; set; }
}

internal sealed class OsisAuxiliaryEntity
{
    public string CompanyCode { get; set; } = string.Empty;
    public string CardCode { get; set; } = string.Empty;
    public string CardName { get; set; } = string.Empty;
}

/// <summary>Catálogo XIMESA.dbo.tipo_lista_precio_tlp -- el canal real de negocio (el mismo que
/// expone [MASTERLOGIC].[ListarCanales] en la base CLOUD). PEDIDOS_VENTA_PPC.TLP_CODTLP referencia
/// directo a este catálogo por pedido; reemplaza al intento anterior de resolver el canal via
/// VENDEDOR_CLIENTE_GRUPOS (que es en realidad un agrupamiento vendedor-cliente, un codigo distinto
/// que no calzaba con el catálogo real -- canales como "11" nunca coincidían al filtrar).</summary>
internal sealed class OsisPriceListEntity
{
    public string CompanyCode { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Name { get; set; }
}

/// <summary>Catálogo XIMESA.dbo.cond_pago_cpa.</summary>
internal sealed class OsisPaymentConditionEntity
{
    public string Code { get; set; } = string.Empty;
    public string? Name { get; set; }
}
