namespace ReyplastCoreAPI.Infrastructure.Integrations.Osis.Persistence;

/// <summary>Proyección mínima de XIMESA.dbo.PRODUCTOS_PRD usada por la integración.</summary>
internal sealed class OsisProductEntity
{
    /// <summary>cia_codcia; 01 identifica la compañía Reyplast.</summary>
    public string CompanyCode { get; set; } = string.Empty;
    /// <summary>prd_codprd; código único del producto dentro de la compañía.</summary>
    public string ProductCode { get; set; } = string.Empty;
    /// <summary>prd_desesp; descripción comercial en español.</summary>
    public string ProductDescription { get; set; } = string.Empty;
    /// <summary>tin_codtin; 01 significa PRODUCTOS TERMINADOS.</summary>
    public string CatalogTypeCode { get; set; } = string.Empty;
    /// <summary>prd_indset; 0 simple, 1 intermedio, 2 compuesto y 3 set.</summary>
    public string? ProductTypeCode { get; set; }
    /// <summary>prd_indest; 1 significa activo con la configuración vigente.</summary>
    public string ProductStatusCode { get; set; } = string.Empty;
    /// <summary>prd_prduni; 1 distingue un PT-SIMPLE de una SIMPLE-PARTE.</summary>
    public short? IsUnitary { get; set; }
    /// <summary>prd_feccre; fecha original de creación en OSIS.</summary>
    public DateTime? CreatedAt { get; set; }
}

/// <summary>Proyección de XIMESA.dbo.PRODUCTOS_COLORES_PCO.</summary>
internal sealed class OsisProductColorEntity
{
    /// <summary>cia_codcia; debe ser 01 para Reyplast.</summary>
    public string CompanyCode { get; set; } = string.Empty;
    /// <summary>pco_codprd; enlaza con PRODUCTOS_PRD.prd_codprd.</summary>
    public string ProductCode { get; set; } = string.Empty;
    /// <summary>pco_codcol; enlaza con COLORES_COL.col_codcol.</summary>
    public string ColorCode { get; set; } = string.Empty;
    /// <summary>pco_indest; estado de la asociación producto-color.</summary>
    public string StatusCode { get; set; } = string.Empty;
    /// <summary>pco_venta; S significa que el color está habilitado para venta.</summary>
    public string? SaleCode { get; set; }
}

/// <summary>Proyección de XIMESA.dbo.COLORES_COL.</summary>
internal sealed class OsisColorEntity
{
    /// <summary>col_codcol; código maestro del color.</summary>
    public string ColorCode { get; set; } = string.Empty;
    /// <summary>col_descol; descripción maestra del color.</summary>
    public string ColorDescription { get; set; } = string.Empty;
    /// <summary>col_indest; 1 significa color activo con la configuración vigente.</summary>
    public string StatusCode { get; set; } = string.Empty;
}
