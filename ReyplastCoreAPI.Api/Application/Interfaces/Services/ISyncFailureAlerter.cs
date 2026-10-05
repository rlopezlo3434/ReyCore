namespace ReyplastCoreAPI.Application.Interfaces.Services;

/// <summary>
/// Aviso a los responsables cuando una sincronización falla sin remedio (agotó sus reintentos) o el job
/// recurrente quedó deshabilitado. Nunca lanza: si el aviso no sale, solo queda en el log.
/// </summary>
public interface ISyncFailureAlerter
{
    Task SendAsync(string subject, string body, CancellationToken cancellationToken = default);
}
