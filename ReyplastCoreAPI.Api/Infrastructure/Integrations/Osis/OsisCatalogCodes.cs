namespace ReyplastCoreAPI.Infrastructure.Integrations.Osis;

/// <summary>Códigos funcionales verificados directamente en la base XIMESA de OSIS.</summary>
internal static class OsisCatalogCodes
{
    /// <summary>Compañía Reyplast usada en las tablas operativas de XIMESA.</summary>
    public const string ReyplastCompany = "01";

    /// <summary>Tipo de inventario 01: PRODUCTOS TERMINADOS, según TIPO_INVENTARIO_TIN.</summary>
    public const string FinishedProductInventory = "01";

    /// <summary>prd_indset 0: producto simple o parte; prd_prduni determina cuál.</summary>
    public const string Simple = "0";

    /// <summary>prd_indset 1: producto intermedio.</summary>
    public const string Intermediate = "1";

    /// <summary>prd_indset 2: producto terminado compuesto.</summary>
    public const string Compound = "2";

    /// <summary>prd_indset 3: producto terminado tipo set.</summary>
    public const string Set = "3";

    /// <summary>prd_prduni 1: el registro simple representa una unidad terminada.</summary>
    public const short UnitaryProduct = 1;
}
