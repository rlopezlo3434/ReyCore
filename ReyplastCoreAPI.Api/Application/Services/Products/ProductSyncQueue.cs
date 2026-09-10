using System.Collections.Concurrent;
using System.Threading.Channels;
using ReyplastCoreAPI.Application.Interfaces.Services;

namespace ReyplastCoreAPI.Application.Services.Products;

/// <summary>
/// Worker de cola en memoria. Arranca vacío junto con la API y solamente procesa
/// sincronizaciones solicitadas explícitamente por un endpoint.
/// </summary>
public sealed class ProductSyncQueue : BackgroundService, IProductSyncQueue
{
    private readonly Channel<Guid> _osisExecutions = Channel.CreateUnbounded<Guid>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _cancellations = new();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ProductSyncQueue> _logger;

    public ProductSyncQueue(
        IServiceScopeFactory scopeFactory,
        ILogger<ProductSyncQueue> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public bool TryEnqueueOsis(Guid executionId)
    {
        CancellationTokenSource cancellation = new();
        if (!_cancellations.TryAdd(executionId, cancellation))
        {
            cancellation.Dispose();
            return false;
        }

        if (_osisExecutions.Writer.TryWrite(executionId)) return true;

        _cancellations.TryRemove(executionId, out _);
        cancellation.Dispose();
        return false;
    }

    public bool TryCancel(Guid executionId)
    {
        if (!_cancellations.TryGetValue(executionId, out CancellationTokenSource? cancellation))
            return false;
        cancellation.Cancel();
        return true;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (Guid executionId in _osisExecutions.Reader.ReadAllAsync(stoppingToken))
        {
            if (!_cancellations.TryGetValue(executionId, out CancellationTokenSource? cancellation))
                continue;

            using CancellationTokenSource linkedCancellation =
                CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, cancellation.Token);
            try
            {
                using IServiceScope scope = _scopeFactory.CreateScope();
                IProductSyncService service = scope.ServiceProvider.GetRequiredService<IProductSyncService>();
                await service.ExecuteOsisAsync(executionId, linkedCancellation.Token);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Falló el worker de sincronización {ExecutionId}.", executionId);
            }
            finally
            {
                _cancellations.TryRemove(executionId, out _);
                cancellation.Dispose();
            }
        }
    }
}
