using Hangfire;
using Hangfire.Common;
using Hangfire.Storage;
using ReyplastCoreAPI.Application.DTOs.Order;
using ReyplastCoreAPI.Application.Interfaces.Services;
using ReyplastCoreAPI.Domain.Catalog.Entities;

namespace ReyplastCoreAPI.Application.Services.Orders;

public sealed class OrderSyncQueue : IOrderSyncQueue
{
    private const string OsisRange = "OSIS_RANGE";
    private const string SapRange = "SAP_RANGE";
    // Cuántos jobs de cada estado se revisan al buscar uno igual; la cola core-sync nunca tiene tantos.
    private const int LookupLimit = 200;

    private readonly IBackgroundJobClient _jobs;
    private readonly JobStorage _storage;
    private readonly ILogger<OrderSyncQueue> _logger;

    public OrderSyncQueue(IBackgroundJobClient jobs, JobStorage storage, ILogger<OrderSyncQueue> logger)
    {
        _jobs = jobs;
        _storage = storage;
        _logger = logger;
    }

    public Task<OrderSyncEnqueueResult?> EnqueueOsisRangeAsync(DateTime dateFrom, DateTime dateTo,
        CancellationToken cancellationToken) =>
        Task.FromResult(Enqueue(OsisRange, dateFrom.Date, dateTo.Date));
    public Task<OrderSyncEnqueueResult?> EnqueueSapRangeAsync(DateTime dateFrom, DateTime dateTo,
        CancellationToken cancellationToken) =>
        Task.FromResult(Enqueue(SapRange, dateFrom.Date, dateTo.Date));

    private OrderSyncEnqueueResult? Enqueue(string kind, DateTime dateFrom, DateTime dateTo)
    {
        try
        {
            // El candado de Hangfire (en su BD) cubre varias instancias del API: dos pedidos simultáneos
            // del mismo rango no pueden pasar ambos la búsqueda y encolar dos jobs.
            using IStorageConnection connection = _storage.GetConnection();
            using IDisposable enqueueLock = connection.AcquireDistributedLock(
                $"core-sync:enqueue:{kind}:{dateFrom:yyyyMMdd}:{dateTo:yyyyMMdd}", TimeSpan.FromSeconds(15));

            OrderSyncEnqueueResult? existing = FindPending(kind, dateFrom, dateTo);
            if (existing is not null)
            {
                _logger.LogInformation(
                    "Sincronización {Kind} {From:yyyy-MM-dd}..{To:yyyy-MM-dd} ya estaba en el job {JobId}; no se encola otra.",
                    kind, dateFrom, dateTo, existing.JobId);
                return existing;
            }

            Guid executionId = Guid.NewGuid();
            string jobId = _jobs.Enqueue<OrderSyncBackgroundJob>(job => job.ExecuteAsync(
                executionId, kind, dateFrom, dateTo, JobCancellationToken.Null));
            return new OrderSyncEnqueueResult(jobId, executionId, SyncStatuses.Queued, Reused: false);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "No se pudo encolar la sincronización {Kind} en Hangfire.", kind);
            return null;
        }
    }

    // Corriendo primero (es el que más avanzó), luego en cola y luego programado (reintento pendiente).
    private OrderSyncEnqueueResult? FindPending(string kind, DateTime dateFrom, DateTime dateTo)
    {
        IMonitoringApi monitoring = _storage.GetMonitoringApi();
        return Match(monitoring.ProcessingJobs(0, LookupLimit).Select(job => (job.Key, job.Value?.Job)),
                   kind, dateFrom, dateTo, SyncStatuses.Running)
            ?? Match(monitoring.EnqueuedJobs("core-sync", 0, LookupLimit).Select(job => (job.Key, job.Value?.Job)),
                   kind, dateFrom, dateTo, SyncStatuses.Queued)
            ?? Match(monitoring.ScheduledJobs(0, LookupLimit).Select(job => (job.Key, job.Value?.Job)),
                   kind, dateFrom, dateTo, SyncStatuses.Queued);
    }

    private static OrderSyncEnqueueResult? Match(IEnumerable<(string JobId, Job? Job)> jobs, string kind,
        DateTime dateFrom, DateTime dateTo, string status)
    {
        foreach ((string jobId, Job? job) in jobs)
        {
            // Ambas sobrecargas de ExecuteAsync tienen (executionId, kind, dateFrom, dateTo, ...).
            if (job?.Type != typeof(OrderSyncBackgroundJob) || job.Args.Count < 4) continue;
            if (job.Args[0] is not Guid executionId || job.Args[1] as string != kind) continue;
            if (job.Args[2] is not DateTime from || job.Args[3] is not DateTime to) continue;
            if (from.Date == dateFrom && to.Date == dateTo)
                return new OrderSyncEnqueueResult(jobId, executionId, status, Reused: true);
        }
        return null;
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
