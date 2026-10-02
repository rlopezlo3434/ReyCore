namespace ReyplastCoreAPI.Application.Services.Orders;

/// <summary>Serializa escrituras masivas de pedidos OSIS y SAP en las tablas compartidas.</summary>
public sealed class OrderSynchronizationLock
{
    public SemaphoreSlim Gate { get; } = new(1, 1);
}
