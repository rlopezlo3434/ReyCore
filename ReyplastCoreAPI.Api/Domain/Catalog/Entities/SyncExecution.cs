using ReyplastCoreAPI.Domain.Catalog;

namespace ReyplastCoreAPI.Domain.Catalog.Entities;

/// <summary>Audita una ejecución de sincronización desde un sistema externo.</summary>
public sealed class SyncExecution
{
    /// <summary>Identificador único compartido con los registros observados.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Sistema que originó la ejecución: OSIS, SAP u otro conector soportado.</summary>
    public string SourceSystem { get; set; } = SourceSystems.Osis;

    /// <summary>Recurso sincronizado: PRODUCTS u ORDERS.</summary>
    public string ResourceType { get; set; } = SyncResourceTypes.Products;

    /// <summary>Fecha y hora de inicio en horario de Lima.</summary>
    public DateTime StartedAt { get; set; }

    /// <summary>Fecha y hora de finalización; permanece nula mientras se ejecuta.</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>Estado QUEUED, RUNNING, COMPLETED, FAILED o CANCELLED.</summary>
    public string Status { get; set; } = SyncStatuses.Queued;

    /// <summary>Último código procesado; permite diagnosticar hasta dónde avanzó el cursor.</summary>
    public string? LastCursor { get; set; }

    /// <summary>Cantidad total de registros principales leídos: productos o cabeceras de pedido.</summary>
    public long RecordsRead { get; set; }
    /// <summary>Registros principales creados en RP_ReyPlastCoreBD.</summary>
    public int RecordsCreated { get; set; }
    /// <summary>Registros principales cuyo contenido cambió.</summary>
    public int RecordsChanged { get; set; }
    /// <summary>Registros principales verificados sin cambios.</summary>
    public int RecordsUnchanged { get; set; }
    /// <summary>Registros hijos creados: colores o detalles, según ResourceType.</summary>
    public int ChildRecordsCreated { get; set; }
    /// <summary>Registros hijos modificados.</summary>
    public int ChildRecordsChanged { get; set; }
    /// <summary>Registros hijos verificados sin cambios.</summary>
    public int ChildRecordsUnchanged { get; set; }

    /// <summary>Error de la ejecución fallida, limitado a 2,000 caracteres.</summary>
    public string? ErrorMessage { get; set; }
}

public static class SyncResourceTypes
{
    public const string Products = "PRODUCTS";
    public const string Orders = "ORDERS";
    public const string Channels = "CHANNELS";
}

/// <summary>Estados persistidos para cualquier sincronización de catálogo.</summary>
public static class SyncStatuses
{
    public const string Queued = "QUEUED";
    public const string Running = "RUNNING";
    public const string Completed = "COMPLETED";
    public const string Failed = "FAILED";
    public const string Cancelled = "CANCELLED";
}
