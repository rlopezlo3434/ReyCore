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

    /// <summary>Cantidad máxima de documentos por consulta de líneas, siempre dentro de una sucursal.</summary>
    public int OrderDetailBatchSize { get; set; } = 100;

    /// <summary>Valor de PPC_SITPPC que significa pedido aprobado; actualmente 2.</summary>
    public string ApprovedSituationCode { get; set; } = "2";

    /// <summary>
    /// Sync horario de aprobados del día: solo busca pedidos creados en estos últimos días (aprobados hoy por
    /// PPC_FECAPR). Acota la consulta en OSIS; no sincroniza esos días completos. Con 90 días la lectura
    /// superaba los 120 s en las noches de semana (20:00–22:00) y la tarea fallaba por timeout.
    /// </summary>
    public int ApprovedDayCreationLookbackDays { get; set; } = 30;

    /// <summary>
    /// Timeout (segundos) de las consultas a OSIS del sync horario de aprobados. Mayor que el general (120 s)
    /// porque en horas de carga de OSIS la lectura puede tardar más; solo aplica a esa sincronización.
    /// </summary>
    public int ApprovedDayCommandTimeoutSeconds { get; set; } = 300;
}
