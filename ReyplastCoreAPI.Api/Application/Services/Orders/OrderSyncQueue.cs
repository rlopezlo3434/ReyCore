using System.Collections.Concurrent;
using System.Threading.Channels;
using ReyplastCoreAPI.Application.Interfaces.Services;

namespace ReyplastCoreAPI.Application.Services.Orders;

/// <summary>Worker inactivo al arranque; procesa solamente pedidos encolados por el endpoint.</summary>
public sealed class OrderSyncQueue : BackgroundService, IOrderSyncQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _cancellations = new();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderSyncQueue> _logger;
    public OrderSyncQueue(IServiceScopeFactory scopeFactory, ILogger<OrderSyncQueue> logger)
    { _scopeFactory = scopeFactory; _logger = logger; }

    public bool TryEnqueueOsis(Guid executionId)
    {
        CancellationTokenSource token = new();
        if (!_cancellations.TryAdd(executionId, token)) { token.Dispose(); return false; }
        if (_channel.Writer.TryWrite(executionId)) return true;
        _cancellations.TryRemove(executionId, out _); token.Dispose(); return false;
    }

    public bool TryCancel(Guid executionId)
    {
        if (!_cancellations.TryGetValue(executionId, out var token)) return false;
        token.Cancel(); return true;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (Guid id in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            if (!_cancellations.TryGetValue(id, out var token)) continue;
            using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, token.Token);
            try
            {
                using IServiceScope scope = _scopeFactory.CreateScope();
                await scope.ServiceProvider.GetRequiredService<IOrderSyncService>().ExecuteOsisAsync(id, linked.Token);
            }
            catch (Exception exception) { _logger.LogError(exception, "Falló la sincronización de pedidos {Id}.", id); }
            finally { _cancellations.TryRemove(id, out _); token.Dispose(); }
        }
    }
}
