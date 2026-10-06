using System.Globalization;
using ReyplastCoreAPI.Shared.Exceptions;

namespace ReyplastCoreAPI.Shared.Helpers;

/// <summary>
/// Fechas de negocio de Core. La hora de negocio es la de Perú (UTC-5 fija, sin horario de verano): es la que
/// se guarda en CreatedAt/ModifiedAt, en las ejecuciones de sincronización y en las fechas de los pedidos.
/// </summary>
public static class DateHelper
{
    public static readonly TimeSpan PeruOffset = TimeSpan.FromHours(-5);

    /// <summary>Hora actual de negocio (Perú).</summary>
    public static DateTime PeruNow() => DateTime.UtcNow.Add(PeruOffset);

    /// <summary>Fecha obligatoria sin hora; error de negocio si no se envió.</summary>
    public static DateTime RequiredDate(DateTime value, string name) => value == default
        ? throw new AppException($"{name} es obligatorio.")
        : value.Date;

    /// <summary>Zona horaria de negocio cuando el sistema no la tiene registrada (UTC-5 fija).</summary>
    public static TimeZoneInfo PeruTimeZoneFallback() =>
        TimeZoneInfo.CreateCustomTimeZone("Peru", PeruOffset, "Perú (UTC-5)", "Perú (UTC-5)");

    /// <summary>Inicio del día siguiente: límite superior EXCLUSIVO de un filtro "hasta" (fecha &lt; resultado),
    /// para no perder las horas del último día.</summary>
    public static DateTime EndExclusive(DateTime dateTo) => dateTo.Date.AddDays(1);

    /// <summary>Fecha en formato "yyyy-MM-dd" sin depender de la cultura del servidor.</summary>
    public static string IsoDate(DateTime value) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>Rango de días "yyyy-MM-dd|yyyy-MM-dd" (cursor/avance de sincronizaciones por rango).</summary>
    public static string RangeKey(DateTime from, DateTime to) => $"{IsoDate(from)}|{IsoDate(to)}";

    /// <summary>Avance de una sincronización por rango: el rango y, si ya empezó, "@cursor".</summary>
    public static string RangeProgress(DateTime from, DateTime to, string? cursor) =>
        string.IsNullOrWhiteSpace(cursor) ? RangeKey(from, to) : $"{RangeKey(from, to)}@{cursor}";
}
