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
        IRecurringJobManager recurringJobs = app.Services.GetRequiredService<IRecurringJobManager>();
        OsisApprovedHourlySyncOptions options = app.Services
            .GetRequiredService<IOptions<OsisApprovedHourlySyncOptions>>().Value;

        if (!options.Enabled)
        {
            recurringJobs.RemoveIfExists(OsisApprovedOrdersHourlyJob.RecurringJobId);
            return app;
        }

        // Solo dentro de la ventana (por defecto "0 6-22 * * *"): de 23:00 a 05:59 no se programa. En
        // Hangfire > Recurring Jobs se ve el cron con su descripción, la zona horaria y la próxima ejecución.
        options.Validate();
        string windowLabel = options.WindowLabel;
        recurringJobs.AddOrUpdate<OsisApprovedOrdersHourlyJob>(
            OsisApprovedOrdersHourlyJob.RecurringJobId,
            job => job.ExecuteAsync(windowLabel, JobCancellationToken.Null),
            options.Cron,
            new RecurringJobOptions { TimeZone = ResolveTimeZone(options.TimeZoneId) });
        return app;
    }

    // Windows usa "SA Pacific Standard Time"; si el servidor solo reconoce IANA, "America/Lima".
    private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        foreach (string id in new[] { timeZoneId, "SA Pacific Standard Time", "America/Lima" })
        {
            if (string.IsNullOrWhiteSpace(id)) continue;
            if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out TimeZoneInfo? zone)) return zone;
        }
        return TimeZoneInfo.CreateCustomTimeZone("Peru", TimeSpan.FromHours(-5), "Perú (UTC-5)", "Perú (UTC-5)");
    }
}
