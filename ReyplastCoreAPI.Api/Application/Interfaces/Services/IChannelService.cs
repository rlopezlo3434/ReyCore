using ReyplastCoreAPI.Application.DTOs.Channel.Response;

namespace ReyplastCoreAPI.Application.Interfaces.Services;

/// <summary>Consulta y sincroniza el catálogo local de canales de venta.</summary>
public interface IChannelService
{
    Task<List<ChannelResponse>> GetAllAsync(
        bool? isActive, string? sourceSystem, CancellationToken cancellationToken = default);

    /// <summary>Lee y persiste el catálogo desde OSIS en la misma solicitud -- el origen
    /// ([MASTERLOGIC].[ListarCanales] / tipo_lista_precio_tlp) es un catálogo cerrado y pequeño,
    /// no necesita el patrón de cola en segundo plano que usan Products/Orders para volúmenes
    /// grandes.</summary>
    Task<ChannelSyncResponse> SyncFromOsisAsync(CancellationToken cancellationToken = default);

    // Agrupación normalizada -- curada a mano desde RP_ReyPlastCoreBD (ver ChannelGroup), no
    // sincronizada de OSIS/SAP. Permite consolidar varios canales finos bajo un mismo nombre
    // limpio (p.ej. "MAYORISTA LIMA" + "MAYORISTA LIMA - -" -> "Mayorista Lima").
    Task<List<ChannelGroupResponse>> GetGroupsAsync(CancellationToken cancellationToken = default);
    Task<ChannelGroupResponse> CreateGroupAsync(ChannelGroupCreateRequest request, CancellationToken cancellationToken = default);
    Task<ChannelResponse> AssignGroupAsync(Guid channelId, ChannelGroupAssignmentRequest request, CancellationToken cancellationToken = default);
}
