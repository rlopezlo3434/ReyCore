using System.ComponentModel;
using System.Globalization;
using Hangfire;
using Hangfire.Storage;
using Microsoft.Extensions.Options;
using ReyplastCoreAPI.Application.DTOs.Order;
using ReyplastCoreAPI.Application.Interfaces.Services;

namespace ReyplastCoreAPI.Application.Services.Orders;

/// <summary>Configuración del job horario (sección OrderSync:OsisApprovedHourly).</summary>
public sealed class OsisApprovedHourlySyncOptions
{
    public const string SectionName = "OrderSync:OsisApprovedHourly";

    /// <summary>false = el job recurrente se elimina de Hangfire al iniciar el API.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Primera hora del día (hora Perú) en que se sincroniza. Por defecto 6 = 06:00.</summary>
    public int FirstHour { get; set; } = 6;

    /// <summary>Última hora del día en que se sincroniza. Por defecto 22 = 22:00; de 23:00 a 05:59 no corre.</summary>
    public int LastHour { get; set; } = 22;

    /// <summary>Zona horaria del cron. "SA Pacific Standard Time" es Perú (UTC-5, sin horario de verano).</summary>
    public string TimeZoneId { get; set; } = "SA Pacific Standard Time";

    /// <summary>Al inicio de cada hora dentro de la ventana: "0 6-22 * * *". Hangfire lo muestra con su descripción.</summary>
    public string Cron => $"0 {FirstHour}-{LastHour} * * *";

    /// <summary>Texto de la ventana para el nombre del job y su resultado en el dashboard.</summary>
    public string WindowLabel => $"{FirstHour:00}:00–{LastHour:00}:00 hora Perú";

    public bool IsInsideWindow(DateTime peruNow) => peruNow.Hour >= FirstHour && peruNow.Hour <= LastHour;

    public void Validate()
    {
        if (FirstHour is < 0 or > 23 || LastHour is < 0 or > 23 || FirstHour > LastHour)
            throw new InvalidOperationException(
                $"{SectionName}: FirstHour y LastHour deben estar entre 0 y 23 y FirstHour no puede ser mayor que LastHour.");
    }
}

/// <summary>
/// Cada hora, de 06:00 a 22:00 (hora Perú, configurable), sincroniza desde OSIS los pedidos APROBADOS del
/// día. Reutiliza la comparación del sync por rango: solo escribe lo que cambió y avisa a MasterLogic
/// únicamente si hubo cambios. El resumen queda como resultado del job en el dashboard de Hangfire.
/// </summary>
public sealed class OsisApprovedOrdersHourlyJob
{
    public const string RecurringJobId = "osis-pedidos-aprobados-cada-hora";
    private static readonly CultureInfo Peru = CultureInfo.GetCultureInfo("es-PE");

    private readonly IOsisOrderSyncService _osis;
    private readonly IOrderSyncNotifier _notifier;
    private readonly JobStorage _storage;
    private readonly OsisApprovedHourlySyncOptions _options;
    private readonly ILogger<OsisApprovedOrdersHourlyJob> _logger;

    public OsisApprovedOrdersHourlyJob(IOsisOrderSyncService osis, IOrderSyncNotifier notifier, JobStorage storage,
        IOptions<OsisApprovedHourlySyncOptions> options, ILogger<OsisApprovedOrdersHourlyJob> logger)
    {
        _osis = osis;
        _notifier = notifier;
        _storage = storage;
        _options = options.Value;
        _logger = logger;
    }

    // "schedule" solo sirve para que el dashboard muestre la ventana en el nombre de cada ejecución
    // ("OSIS · pedidos aprobados del día · 06:00–22:00 hora Perú"); la regla vive en las opciones.
    [Queue("core-sync")]
    [DisableConcurrentExecution(timeoutInSeconds: 3600)]
    [AutomaticRetry(Attempts = 2)]
    [DisplayName("OSIS · pedidos aprobados del día · {0}")]
    public async Task<string> ExecuteAsync(string schedule, IJobCancellationToken cancellationToken)
    {
        DateTime startedAt = PeruNow();
        // El cron ya no la programa de 23:00 a 05:59; esto cubre "Trigger now" desde el dashboard o un
        // reintento que caiga fuera de la ventana.
        if (!_options.IsInsideWindow(startedAt))
        {
            string outside = $"Omitida: {startedAt:HH:mm} está fuera del horario de sincronización ({_options.WindowLabel}).";
            _logger.LogInformation("Sincronización horaria de pedidos aprobados omitida: fuera de horario ({Hour:HH:mm}).", startedAt);
            return outside;
        }

        // Una sincronización manual por rango ya está recorriendo OSIS: esta hora no suma nada y solo
        // cargaría el servidor. La próxima ejecución horaria vuelve a revisar el día completo.
        if (IsOrderRangeSyncRunning())
        {
            const string skipped = "Omitida: ya había una sincronización de pedidos en curso.";
            _logger.LogInformation("Sincronización horaria de pedidos aprobados omitida: hay otra en curso.");
            return skipped;
        }

        DateTime today = startedAt.Date;
        Guid executionId = Guid.NewGuid();

        OrdersSynchronizedEvent? result = await _osis.ExecuteApprovedDayAsync(
            executionId, today, cancellationToken.ShutdownToken);
        if (result is null) return "Cancelada.";

        if (result.HasChanges)
            await _notifier.PublishOrdersSynchronizedAsync(result, CancellationToken.None);

        string summary = string.Format(Peru,
            "{0:dd/MM/yyyy} (hora Perú) · {1} pedidos aprobados del día · {2} nuevos · {3} con cambios · {4} sin cambios · {5:HH:mm:ss}–{6:HH:mm:ss}{7}",
            today, result.RecordsRead, result.RecordsCreated, result.RecordsChanged, result.RecordsUnchanged,
            startedAt, PeruNow(), result.HasChanges ? " · MasterLogic notificado" : string.Empty);
        _logger.LogInformation("Sincronización horaria de pedidos aprobados: {Summary}", summary);
        return summary;
    }

    private bool IsOrderRangeSyncRunning()
    {
        try
        {
            IMonitoringApi monitoring = _storage.GetMonitoringApi();
            return monitoring.ProcessingJobs(0, 100)
                .Any(job => job.Value?.Job?.Type == typeof(OrderSyncBackgroundJob));
        }
        catch (Exception exception)
        {
            // Si no se puede consultar Hangfire se sigue igual: DisableConcurrentExecution y el
            // bloqueo de persistencia ya evitan escrituras simultáneas.
            _logger.LogWarning(exception, "No se pudo revisar si hay otra sincronización de pedidos en curso.");
            return false;
        }
    }

    // Misma convención que el resto del servicio de pedidos: OSIS guarda hora de Perú (UTC-5).
    private static DateTime PeruNow() => DateTime.UtcNow.AddHours(-5);
}
