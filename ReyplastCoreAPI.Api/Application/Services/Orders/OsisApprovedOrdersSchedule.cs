using Hangfire;
using Hangfire.Storage;

namespace ReyplastCoreAPI.Application.Services.Orders;

/// <summary>
/// Alta, revisión y reparación del job recurrente "osis-pedidos-aprobados-cada-hora". Lo usan el arranque
/// del API (UseOrderSyncSchedules) y el vigilante (<see cref="OsisApprovedOrdersScheduleWatchdog"/>).
/// </summary>
public static class OsisApprovedOrdersSchedule
{
    /// <summary>Da de alta (o actualiza) el job con la ventana y zona horaria de las opciones.</summary>
    public static void Register(IRecurringJobManager recurringJobs, OsisApprovedHourlySyncOptions options)
    {
        // Solo dentro de la ventana (por defecto "0 6-22 * * *"): de 23:00 a 05:59 no se programa. En
        // Hangfire > Recurring Jobs se ve el cron con su descripción, la zona horaria y la próxima ejecución.
        options.Validate();
        string windowLabel = options.WindowLabel;
        recurringJobs.AddOrUpdate<OsisApprovedOrdersHourlyJob>(
            OsisApprovedOrdersHourlyJob.RecurringJobId,
            job => job.ExecuteAsync(windowLabel, JobCancellationToken.Null),
            options.Cron,
            new RecurringJobOptions { TimeZone = ResolveTimeZone(options.TimeZoneId) });
    }

    /// <summary>
    /// Revisa el job en el storage. Hangfire lo deja sin próxima ejecución (deshabilitado) cuando su
    /// planificador no puede cargarlo varias veces seguidas, p. ej. si un servidor con una versión vieja del
    /// API toma el turno ("Could not resolve type ..."). Devuelve null si está sano o el motivo si no.
    /// </summary>
    public static string? FindProblem(JobStorage storage)
    {
        using IStorageConnection connection = storage.GetConnection();
        Dictionary<string, string>? entries = connection.GetAllEntriesFromHash(
            $"recurring-job:{OsisApprovedOrdersHourlyJob.RecurringJobId}");
        if (entries is null || entries.Count == 0) return "El job recurrente no existe en Hangfire.";

        if (entries.TryGetValue("Error", out string? error) && !string.IsNullOrWhiteSpace(error))
            return $"Hangfire registró un error al programarlo: {FirstLine(error)}";

        if (!entries.TryGetValue("NextExecution", out string? next) || string.IsNullOrWhiteSpace(next))
            return "No tiene próxima ejecución: Hangfire lo deshabilitó y no volverá a correr solo.";

        return null;
    }

    /// <summary>Lo vuelve a crear desde cero: borra el error y recalcula la próxima ejecución.</summary>
    public static void Repair(IRecurringJobManager recurringJobs, OsisApprovedHourlySyncOptions options)
    {
        recurringJobs.RemoveIfExists(OsisApprovedOrdersHourlyJob.RecurringJobId);
        Register(recurringJobs, options);
    }

    // Windows usa "SA Pacific Standard Time"; si el servidor solo reconoce IANA, "America/Lima".
    public static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        foreach (string id in new[] { timeZoneId, "SA Pacific Standard Time", "America/Lima" })
        {
            if (string.IsNullOrWhiteSpace(id)) continue;
            if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out TimeZoneInfo? zone)) return zone;
        }
        return TimeZoneInfo.CreateCustomTimeZone("Peru", TimeSpan.FromHours(-5), "Perú (UTC-5)", "Perú (UTC-5)");
    }

    private static string FirstLine(string value)
    {
        int end = value.IndexOfAny(new[] { '\r', '\n' });
        string line = end < 0 ? value : value[..end];
        return line.Length > 300 ? line[..300] : line;
    }
}
