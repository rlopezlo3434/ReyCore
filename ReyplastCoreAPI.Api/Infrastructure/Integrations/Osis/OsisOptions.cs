namespace ReyplastCoreAPI.Infrastructure.Integrations.Osis;

/// <summary>Configura códigos de estado cuyo significado depende del catálogo OSIS.</summary>
public sealed class OsisOptions
{
    public const string SectionName = "Osis";

    /// <summary>Valor de prd_indest, pco_indest y col_indest que significa activo; actualmente 1.</summary>
    public string ActiveStatusCode { get; set; } = string.Empty;

    /// <summary>Valor de PRODUCTOS_COLORES_PCO.pco_venta que habilita venta; actualmente S.</summary>
    public string EnabledSaleCode { get; set; } = string.Empty;
}
