using Hangfire;
using ReyplastCoreAPI.Application.DTOs.Order;
using ReyplastCoreAPI.Application.Interfaces.Services;

namespace ReyplastCoreAPI.Application.Services.Orders;

public sealed class OrderSyncQueue : IOrderSyncQueue
{
    private readonly IBackgroundJobClient _jobs;
    private readonly ILogger<OrderSyncQueue> _logger;

    public OrderSyncQueue(IBackgroundJobClient jobs, ILogger<OrderSyncQueue> logger)
    {
        _jobs = jobs;
        _logger = logger;
    }

    public Task<string?> EnqueueOsisRangeAsync(Guid executionId, DateTime dateFrom, DateTime dateTo,
        CancellationToken cancellationToken) =>
        Task.FromResult(Enqueue(executionId, "OSIS_RANGE", dateFrom.Date, dateTo.Date));
    public Task<string?> EnqueueSapRangeAsync(Guid executionId, DateTime dateFrom, DateTime dateTo,
        CancellationToken cancellationToken) =>
        Task.FromResult(Enqueue(executionId, "SAP_RANGE", dateFrom.Date, dateTo.Date));

    private string? Enqueue(Guid executionId, string kind, DateTime? dateFrom, DateTime? dateTo)
    {
        try
        {
            return _jobs.Enqueue<OrderSyncBackgroundJob>(job => job.ExecuteAsync(
                executionId, kind, dateFrom, dateTo, JobCancellationToken.Null));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "No se pudo encolar la sincronización {ExecutionId} en Hangfire.", executionId);
            return null;
        }
    }
}

public sealed class OrderSyncBackgroundJob
{
    private readonly IOsisOrderSyncService _osis;
    private readonly ISapOrderSyncService _sap;
    private readonly IOrderSyncNotifier _notifier;

    public OrderSyncBackgroundJob(IOsisOrderSyncService osis, ISapOrderSyncService sap, IOrderSyncNotifier notifier)
    {
        _osis = osis; _sap = sap; _notifier = notifier;
    }

    [Queue("core-sync")]
    [DisableConcurrentExecution(timeoutInSeconds: 21600)]
    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync(Guid executionId, string kind, DateTime? dateFrom, DateTime? dateTo,
        IJobCancellationToken cancellationToken)
    {
        await ExecuteCoreAsync(executionId, kind, dateFrom, dateTo, cancellationToken);
    }

    /// <summary>Compatibilidad temporal para jobs de rango encolados antes de retirar Delta.</summary>
    [Queue("core-sync")]
    [DisableConcurrentExecution(timeoutInSeconds: 21600)]
    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync(Guid executionId, string kind, DateTime? dateFrom, DateTime? dateTo,
        int? obsoleteOverlapSeconds, IJobCancellationToken cancellationToken)
    {
        await ExecuteCoreAsync(executionId, kind, dateFrom, dateTo, cancellationToken);
    }

    private async Task ExecuteCoreAsync(Guid executionId, string kind, DateTime? dateFrom, DateTime? dateTo,
        IJobCancellationToken cancellationToken)
    {
        CancellationToken token = cancellationToken.ShutdownToken;
        OrdersSynchronizedEvent? result = kind switch
        {
            "OSIS_RANGE" => await _osis.ExecuteRangeAsync(executionId, dateFrom!.Value, dateTo!.Value, token),
            "SAP_RANGE" => await _sap.ExecuteRangeAsync(executionId, dateFrom!.Value, dateTo!.Value, token),
            _ => throw new InvalidOperationException($"Tipo de sincronización desconocido: {kind}.")
        };
        // Solo al terminar bien (null = cancelada). Si falla, Hangfire reintenta y el aviso sale
        // en el intento que complete.
        if (result is not null)
            await _notifier.PublishOrdersSynchronizedAsync(result, CancellationToken.None);
    }
}
