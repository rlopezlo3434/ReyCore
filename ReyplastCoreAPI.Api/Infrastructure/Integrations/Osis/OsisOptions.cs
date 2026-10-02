namespace ReyplastCoreAPI.Infrastructure.Integrations.Osis;

/// <summary>Configura códigos de estado cuyo significado depende del catálogo OSIS.</summary>
public sealed class OsisOptions
{
    public const string SectionName = "Osis";

    /// <summary>Valor de prd_indest, pco_indest y col_indest que significa activo; actualmente 1.</summary>
    public string ActiveStatusCode { get; set; } = string.Empty;

    /// <summary>Valor de PRODUCTOS_COLORES_PCO.pco_venta que habilita venta; actualmente S.</summary>
    public string EnabledSaleCode { get; set; } = string.Empty;

    /// <summary>Cantidad interna de cabeceras por lectura paginada de pedidos.</summary>
    public int OrderPageSize { get; set; }

    /// <summary>Valor de PPC_SITPPC que significa pedido aprobado; actualmente 2.</summary>
    public string ApprovedSituationCode { get; set; } = "2";

    /// <summary>
    /// Sync horario de aprobados del día: solo busca pedidos creados en estos últimos días (aprobados hoy por
    /// PPC_FECAPR). Acota la consulta en OSIS; no sincroniza esos días completos.
    /// </summary>
    public int ApprovedDayCreationLookbackDays { get; set; } = 90;
}
