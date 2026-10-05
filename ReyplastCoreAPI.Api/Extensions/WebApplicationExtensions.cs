using Hangfire;
using Microsoft.Extensions.Options;
using ReyplastCoreAPI.Api.Hubs;
using ReyplastCoreAPI.Api.Middleware;
using ReyplastCoreAPI.Application.Services.Orders;

namespace ReyplastCoreAPI.Api.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.MapControllers();
        app.MapHub<SyncHub>(SyncHub.Route);
        return app;
    }

    public static void UseApiDocumentation(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    /// <summary>
    /// Da de alta (o actualiza) los jobs recurrentes de sincronización en Hangfire. Se ven en
    /// /hangfire/recurring con su zona horaria (Perú) y la próxima ejecución.
    /// </summary>
    public static WebApplication UseOrderSyncSchedules(this WebApplication app)
    {
        // Solo los servidores que ejecutan tareas registran los jobs: un API levantado en una PC de desarrollo
        // (o un servidor con otra versión) contra la misma base pisaba la definición del job de producción.
        if (!app.Configuration.GetValue("Hangfire:ServerEnabled", true)) return app;

        IRecurringJobManager recurringJobs = app.Services.GetRequiredService<IRecurringJobManager>();
        OsisApprovedHourlySyncOptions options = app.Services
            .GetRequiredService<IOptions<OsisApprovedHourlySyncOptions>>().Value;

        if (!options.Enabled)
        {
            recurringJobs.RemoveIfExists(OsisApprovedOrdersHourlyJob.RecurringJobId);
            return app;
        }

        // Si quedó deshabilitado o con error (Hangfire lo apaga cuando no puede cargarlo), se recrea desde cero;
        // si está sano, AddOrUpdate solo actualiza la definición. El vigilante repite esta revisión cada 10 min.
        JobStorage storage = app.Services.GetRequiredService<JobStorage>();
        if (OsisApprovedOrdersSchedule.FindProblem(storage) is { } problem
            && !problem.StartsWith("El job recurrente no existe", StringComparison.Ordinal))
        {
            app.Logger.LogWarning("Job {JobId} recreado al iniciar: {Problem}", OsisApprovedOrdersHourlyJob.RecurringJobId, problem);
            OsisApprovedOrdersSchedule.Repair(recurringJobs, options);
            return app;
        }

        OsisApprovedOrdersSchedule.Register(recurringJobs, options);
        return app;
    }
}
