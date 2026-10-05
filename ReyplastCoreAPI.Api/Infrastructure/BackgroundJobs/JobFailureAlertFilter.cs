using System.Globalization;
using Hangfire.States;
using Hangfire.Storage;
using ReyplastCoreAPI.Application.Interfaces.Services;

namespace ReyplastCoreAPI.Infrastructure.BackgroundJobs;

/// <summary>
/// Filtro global de Hangfire: cuando un job queda en Failed (ya agotó sus reintentos de AutomaticRetry, que
/// mientras quedan intentos lo pasa a Scheduled y no a Failed), lo registra como error y envía un aviso por
/// correo. Así no hace falta revisar el dashboard para enterarse de que una sincronización no corrió.
/// </summary>
public sealed class JobFailureAlertFilter : IApplyStateFilter
{
    private static readonly CultureInfo Peru = CultureInfo.GetCultureInfo("es-PE");
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<JobFailureAlertFilter> _logger;

    public JobFailureAlertFilter(IServiceScopeFactory scopeFactory, ILogger<JobFailureAlertFilter> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
    {
        if (context.NewState is not FailedState failed) return;

        string jobId = context.BackgroundJob.Id;
        string jobName = context.BackgroundJob.Job is { } job ? $"{job.Type.Name}.{job.Method.Name}" : "(job no cargado)";
        string message = failed.Exception?.Message ?? "Sin detalle.";
        DateTime failedAtPeru = failed.FailedAt.AddHours(-5);
        _logger.LogError(failed.Exception, "Job {JobId} ({JobName}) falló definitivamente tras sus reintentos: {Message}",
            jobId, jobName, message);

        string subject = $"[ReyplastCore] Falló la sincronización {jobName} (job {jobId})";
        string body = string.Join(Environment.NewLine,
            "Una tarea de sincronización de ReyplastCore falló y ya agotó sus reintentos.",
            string.Empty,
            $"Tarea: {jobName}",
            $"Job: {jobId}",
            $"Servidor: {Environment.MachineName}",
            $"Fecha: {failedAtPeru.ToString("dd/MM/yyyy HH:mm:ss", Peru)} (hora Perú)",
            $"Error: {failed.Exception?.GetType().Name}: {message}",
            string.Empty,
            $"Detalle en el dashboard: /hangfire/jobs/details/{jobId}");

        // Fuera de la transacción de Hangfire: el envío no debe demorar ni romper el cambio de estado.
        _ = Task.Run(async () =>
        {
            try
            {
                using IServiceScope scope = _scopeFactory.CreateScope();
                await scope.ServiceProvider.GetRequiredService<ISyncFailureAlerter>().SendAsync(subject, body);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "No se pudo enviar el aviso del job {JobId}.", jobId);
            }
        });
    }

    public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
    {
    }
}
