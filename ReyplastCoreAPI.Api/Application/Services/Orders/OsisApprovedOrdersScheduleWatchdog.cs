using Hangfire;
using Microsoft.Extensions.Options;
using ReyplastCoreAPI.Application.Interfaces.Services;

namespace ReyplastCoreAPI.Application.Services.Orders;

/// <summary>
/// Vigila el job recurrente "osis-pedidos-aprobados-cada-hora" (al arrancar y cada 10 minutos). Si Hangfire
/// lo dejó con error o deshabilitado (p. ej. el 05/10 a las 09:00 un servidor con una versión vieja no pudo
/// cargarlo y Hangfire lo apagó), lo vuelve a crear y avisa por correo una vez por incidente. Solo corre en
/// los servidores que ejecutan tareas (Hangfire:ServerEnabled = true).
/// </summary>
public sealed class OsisApprovedOrdersScheduleWatchdog : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(10);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRecurringJobManager _recurringJobs;
    private readonly JobStorage _storage;
    private readonly OsisApprovedHourlySyncOptions _options;
    private readonly ILogger<OsisApprovedOrdersScheduleWatchdog> _logger;
    private bool _incidentOpen;

    public OsisApprovedOrdersScheduleWatchdog(IServiceScopeFactory scopeFactory, IRecurringJobManager recurringJobs,
        JobStorage storage, IOptions<OsisApprovedHourlySyncOptions> options,
        ILogger<OsisApprovedOrdersScheduleWatchdog> logger)
    {
        _scopeFactory = scopeFactory;
        _recurringJobs = recurringJobs;
        _storage = storage;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled) return;

        using PeriodicTimer timer = new(Interval);
        do
        {
            await CheckAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task CheckAsync(CancellationToken cancellationToken)
    {
        try
        {
            string? problem = OsisApprovedOrdersSchedule.FindProblem(_storage);
            if (problem is null)
            {
                if (_incidentOpen) _logger.LogInformation("El job {JobId} volvió a estar sano.", OsisApprovedOrdersHourlyJob.RecurringJobId);
                _incidentOpen = false;
                return;
            }

            OsisApprovedOrdersSchedule.Repair(_recurringJobs, _options);
            _logger.LogWarning("Job {JobId} reparado: {Problem}", OsisApprovedOrdersHourlyJob.RecurringJobId, problem);

            // Un aviso por incidente: si vuelve a romperse en cada revisión (sigue conectado el servidor con
            // la versión vieja), no se manda un correo cada 10 minutos.
            if (_incidentOpen) return;
            _incidentOpen = true;
            using IServiceScope scope = _scopeFactory.CreateScope();
            await scope.ServiceProvider.GetRequiredService<ISyncFailureAlerter>().SendAsync(
                $"[ReyplastCore] El job {OsisApprovedOrdersHourlyJob.RecurringJobId} estaba deshabilitado y se reactivó",
                string.Join(Environment.NewLine,
                    $"Problema detectado: {problem}",
                    string.Empty,
                    $"El servidor {Environment.MachineName} volvió a registrar el job y recalculó su próxima ejecución.",
                    "Si el aviso se repite, revisar qué servidor con otra versión del API está conectado a la misma base de Hangfire",
                    "(Hangfire > Servers) y apagarlo o configurarlo con Hangfire:ServerEnabled = false."),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "No se pudo revisar el job {JobId}.", OsisApprovedOrdersHourlyJob.RecurringJobId);
        }
    }
}
