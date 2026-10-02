using Hangfire;
using ReyplastCoreAPI.Application.Interfaces.Services;

namespace ReyplastCoreAPI.Application.Services.Products;

/// <summary>Encola sincronizaciones de productos en almacenamiento persistente de Hangfire.</summary>
public sealed class ProductSyncQueue : IProductSyncQueue
{
    private readonly IBackgroundJobClient _jobs;
    private readonly ILogger<ProductSyncQueue> _logger;

    public ProductSyncQueue(IBackgroundJobClient jobs, ILogger<ProductSyncQueue> logger)
    {
        _jobs = jobs;
        _logger = logger;
    }

    public Task<string?> EnqueueOsisAsync(Guid executionId, CancellationToken cancellationToken)
    {
        try
        {
            string jobId = _jobs.Enqueue<ProductSyncBackgroundJob>(job =>
                job.ExecuteAsync(executionId, JobCancellationToken.Null));
            return Task.FromResult<string?>(jobId);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "No se pudo encolar la sincronización {ExecutionId} en Hangfire.", executionId);
            return Task.FromResult<string?>(null);
        }
    }
}

public sealed class ProductSyncBackgroundJob
{
    private readonly IProductSyncService _service;
    public ProductSyncBackgroundJob(IProductSyncService service) => _service = service;

    [Queue("core-sync")]
    [DisableConcurrentExecution(timeoutInSeconds: 21600)]
    [AutomaticRetry(Attempts = 3)]
    public Task ExecuteAsync(Guid executionId, IJobCancellationToken cancellationToken) =>
        _service.ExecuteOsisAsync(executionId, cancellationToken.ShutdownToken);
}
