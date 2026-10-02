using ReyplastCoreAPI.Domain.Common;
using ReyplastCoreAPI.Domain.Catalog;

namespace ReyplastCoreAPI.Domain.Catalog.Entities;

/// <summary>Canal de venta del catálogo local, sincronizado desde un sistema externo.</summary>
public class Channel : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Código del canal tal como lo expone el origen (p.ej. CANAL_COD en OSIS).</summary>
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    /// <summary>OSIS o SAP. A diferencia de Product, no admite MANUAL: los canales son un
    /// catálogo cerrado del negocio, no algo que se cree libremente desde esta API.</summary>
    public string SourceSystem { get; set; } = SourceSystems.Osis;
    public Guid? SyncExecutionId { get; set; }
    public DateTime? LastSynchronizedAt { get; set; }
    /// <summary>Agrupación normalizada, curada a mano (ver ChannelGroup) -- OSIS trae decenas de
    /// canales finos ("MAYORISTA LIMA", "MAYORISTA LIMA - -", "CANALES ALTERNATIVOS - LIMA"...)
    /// que el negocio quiere poder consolidar bajo un mismo nombre limpio sin perder el detalle
    /// original en Code/Name. Nulo = sin agrupar todavía.</summary>
    public Guid? ChannelGroupId { get; set; }
    public ChannelGroup? ChannelGroup { get; set; }
}

/// <summary>Agrupación normalizada de canales, mantenida a mano desde RP_ReyPlastCoreBD (no viene
/// sincronizada de OSIS/SAP) -- p.ej. varios canales finos de OSIS bajo un mismo "Mayorista Lima".</summary>
public class ChannelGroup : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public ICollection<Channel> Channels { get; set; } = new List<Channel>();
}
