namespace ReyplastCoreAPI.Application.DTOs.Channel.Response;

/// <summary>Canal de venta local, sincronizado desde un sistema externo.</summary>
public sealed class ChannelResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string SourceSystem { get; set; } = string.Empty;
    public Guid? SyncExecutionId { get; set; }
    public DateTime? LastSynchronizedAt { get; set; }
    /// <summary>Agrupación normalizada asignada a mano (ver ChannelGroup) -- nulo si todavía no
    /// se agrupó. p.ej. varios canales finos de OSIS ("MAYORISTA LIMA", "MAYORISTA LIMA - -")
    /// pueden apuntar al mismo grupo "Mayorista Lima".</summary>
    public Guid? ChannelGroupId { get; set; }
    public string? ChannelGroupName { get; set; }
}

/// <summary>Agrupación normalizada de canales, mantenida a mano desde RP_ReyPlastCoreBD.</summary>
public sealed class ChannelGroupResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ChannelCount { get; set; }
}

public sealed class ChannelGroupCreateRequest
{
    public string Name { get; set; } = string.Empty;
}

/// <summary>Asigna (o quita, con Id nulo) el canal a una agrupación.</summary>
public sealed class ChannelGroupAssignmentRequest
{
    public Guid? ChannelGroupId { get; set; }
}

/// <summary>Resultado inmediato de una sincronización de canales -- se ejecuta en la misma
/// solicitud (a diferencia de Products/Orders) porque el catálogo es pequeño y rápido de leer.</summary>
public sealed class ChannelSyncResponse
{
    /// <summary>Identificador del intento registrado en SyncExecutions.</summary>
    public Guid ExecutionId { get; set; }
    /// <summary>Estado final: COMPLETED o FAILED.</summary>
    public string Status { get; set; } = string.Empty;
    public int ChannelsCreated { get; set; }
    public int ChannelsChanged { get; set; }
    public int ChannelsUnchanged { get; set; }
    public int ChannelsDeactivated { get; set; }
}
