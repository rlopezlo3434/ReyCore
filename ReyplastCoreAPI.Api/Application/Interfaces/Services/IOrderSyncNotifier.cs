using ReyplastCoreAPI.Application.DTOs.Order;

namespace ReyplastCoreAPI.Application.Interfaces.Services;

/// <summary>Avisa a los consumidores conectados que terminó una sincronización de pedidos. Nunca
/// lanza excepción: un fallo al notificar no debe marcar el job como fallido ni reintentarlo.</summary>
public interface IOrderSyncNotifier
{
    Task PublishOrdersSynchronizedAsync(OrdersSynchronizedEvent synchronizedEvent,
        CancellationToken cancellationToken = default);
}
