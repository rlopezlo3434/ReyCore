using Microsoft.AspNetCore.SignalR;
using ReyplastCoreAPI.Application.DTOs.Order;
using ReyplastCoreAPI.Application.Interfaces.Services;

namespace ReyplastCoreAPI.Api.Hubs;

public sealed class SignalROrderSyncNotifier : IOrderSyncNotifier
{
    public const string OrdersSynchronizedEventName = "OrdersSynchronized";

    private readonly IHubContext<SyncHub> _hubContext;
    private readonly ILogger<SignalROrderSyncNotifier> _logger;

    public SignalROrderSyncNotifier(IHubContext<SyncHub> hubContext, ILogger<SignalROrderSyncNotifier> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task PublishOrdersSynchronizedAsync(OrdersSynchronizedEvent synchronizedEvent,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients.All.SendAsync(OrdersSynchronizedEventName, synchronizedEvent, cancellationToken);
            _logger.LogInformation(
                "Sync {Kind} {SourceSystem} {ExecutionId} notificado: {Created} nuevos, {Changed} cambiados, {Unchanged} sin cambios.",
                synchronizedEvent.Kind, synchronizedEvent.SourceSystem, synchronizedEvent.ExecutionId,
                synchronizedEvent.RecordsCreated, synchronizedEvent.RecordsChanged, synchronizedEvent.RecordsUnchanged);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "No se pudo notificar por SignalR el fin del sync {ExecutionId}.",
                synchronizedEvent.ExecutionId);
        }
    }
}
