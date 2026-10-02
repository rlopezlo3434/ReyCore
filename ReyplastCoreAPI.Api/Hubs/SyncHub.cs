using Microsoft.AspNetCore.SignalR;

namespace ReyplastCoreAPI.Api.Hubs;

/// <summary>Hub de solo notificación (/hubs/sync): los clientes escuchan, no invocan nada. Eventos:
/// <see cref="SignalROrderSyncNotifier.OrdersSynchronizedEventName"/>. Su consumidor previsto es
/// MasterLogicApi (servidor a servidor), que reenvía el aviso a sus pantallas por su propio hub
/// autenticado con PadLock -- los navegadores no se conectan aquí. Mismo nivel de acceso que el
/// resto de esta API interna (sin autenticación propia).</summary>
public sealed class SyncHub : Hub
{
    public const string Route = "/hubs/sync";
}
