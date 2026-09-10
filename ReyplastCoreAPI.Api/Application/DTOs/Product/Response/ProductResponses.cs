namespace ReyplastCoreAPI.Application.DTOs.Product.Response;

/// <summary>Producto local junto con sus colores y trazabilidad.</summary>
public sealed class ProductResponse
{
    /// <summary>Identificador interno de ReyplastCoreDB; no procede de OSIS.</summary>
    public Guid Id { get; set; }
    /// <summary>Clasificación normalizada a partir de prd_indset y prd_prduni.</summary>
    public string Type { get; set; } = string.Empty;
    /// <summary>Código del producto; para OSIS procede de PRODUCTOS_PRD.prd_codprd.</summary>
    public string GenericCode { get; set; } = string.Empty;
    /// <summary>Descripción; para OSIS procede de PRODUCTOS_PRD.prd_desesp.</summary>
    public string? Description { get; set; }
    /// <summary>Estado; para OSIS es true cuando prd_indest coincide con ActiveStatusCode.</summary>
    public bool IsActive { get; set; }
    /// <summary>Sistema propietario del registro: OSIS, SAP o MANUAL.</summary>
    public string SourceSystem { get; set; } = string.Empty;
    /// <summary>Última ejecución que encontró este producto en el origen.</summary>
    public Guid? SyncExecutionId { get; set; }
    /// <summary>Fecha local de la última sincronización del registro.</summary>
    public DateTime? LastSynchronizedAt { get; set; }
    /// <summary>Para OSIS procede de prd_feccre; si está vacío se usa la primera importación.</summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>Fecha local de la última modificación detectada.</summary>
    public DateTime? ModifiedAt { get; set; }
    /// <summary>Colores asociados en PRODUCTOS_COLORES_PCO y descritos por COLORES_COL.</summary>
    public List<ColorResponse> Colors { get; set; } = new();
}

/// <summary>Color asociado; su sistema de origen es el mismo del producto padre.</summary>
public sealed class ColorResponse
{
    /// <summary>Identificador interno de ReyplastCoreDB.</summary>
    public Guid Id { get; set; }
    /// <summary>Código de color procedente de pco_codcol.</summary>
    public string Code { get; set; } = string.Empty;
    /// <summary>Descripción procedente de COLORES_COL.col_descol.</summary>
    public string? ColorDescription { get; set; }
    /// <summary>true cuando PRODUCTOS_COLORES_PCO.pco_venta coincide con EnabledSaleCode (S).</summary>
    public bool IsSaleEnabled { get; set; }
    /// <summary>true cuando tanto la asociación producto-color como el color están activos.</summary>
    public bool IsActive { get; set; }
    /// <summary>Última ejecución que encontró esta asociación en el origen.</summary>
    public Guid? SyncExecutionId { get; set; }
    /// <summary>Fecha local de la última sincronización de la asociación.</summary>
    public DateTime? LastSynchronizedAt { get; set; }
}

/// <summary>Respuesta paginada común de la API.</summary>
public sealed class PaginatedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalRecords { get; set; }
    public int TotalPages { get; set; }
}

/// <summary>Resultado de un cambio de estado local.</summary>
public sealed class StatusChangeResponse
{
    public bool IsActive { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>Resultado de una solicitud de sincronización de productos.</summary>
public sealed class ProductSyncResponse
{
    /// <summary>Identificador del intento registrado en SyncExecutions.</summary>
    public Guid ExecutionId { get; set; }
    /// <summary>Estado final: COMPLETED o FAILED.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>Total de productos leídos desde el origen.</summary>
    public long Processed { get; set; }
    /// <summary>Número de páginas internas procesadas.</summary>
    public int PagesProcessed { get; set; }
    public int ProductsCreated { get; set; }
    public int ProductsChanged { get; set; }
    public int ProductsUnchanged { get; set; }
    public int ColorsCreated { get; set; }
    public int ColorsChanged { get; set; }
    public int ColorsUnchanged { get; set; }
}

/// <summary>Confirma que una sincronización fue registrada para ejecutarse en segundo plano.</summary>
public sealed class ProductSyncAcceptedResponse
{
    /// <summary>Identificador usado para consultar o cancelar la ejecución.</summary>
    public Guid ExecutionId { get; set; }
    /// <summary>Estado inicial de la solicitud: QUEUED.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>Ruta relativa para consultar el avance.</summary>
    public string StatusUrl { get; set; } = string.Empty;
}

/// <summary>Resumen auditable de una ejecución de sincronización.</summary>
public sealed class SyncExecutionResponse
{
    /// <summary>Identificador único del intento de sincronización.</summary>
    public Guid Id { get; set; }
    /// <summary>Sistema consultado, por ejemplo OSIS o SAP.</summary>
    public string SourceSystem { get; set; } = string.Empty;
    /// <summary>Recurso sincronizado: PRODUCTS u ORDERS.</summary>
    public string ResourceType { get; set; } = string.Empty;
    /// <summary>Momento en que comenzó el intento.</summary>
    public DateTime StartedAt { get; set; }
    /// <summary>Momento en que terminó; nulo mientras está RUNNING.</summary>
    public DateTime? CompletedAt { get; set; }
    /// <summary>RUNNING, COMPLETED o FAILED.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>Segundos transcurridos desde el inicio; continúa creciendo mientras está RUNNING.</summary>
    public double DurationSeconds { get; set; }
    /// <summary>Último código confirmado; permite auditar el avance del cursor.</summary>
    public string? LastCursor { get; set; }
    public long RecordsRead { get; set; }
    public int RecordsCreated { get; set; }
    public int RecordsChanged { get; set; }
    public int RecordsUnchanged { get; set; }
    public int ChildRecordsCreated { get; set; }
    public int ChildRecordsChanged { get; set; }
    public int ChildRecordsUnchanged { get; set; }
    /// <summary>Detalle técnico cuando la ejecución termina en FAILED.</summary>
    public string? ErrorMessage { get; set; }
}
