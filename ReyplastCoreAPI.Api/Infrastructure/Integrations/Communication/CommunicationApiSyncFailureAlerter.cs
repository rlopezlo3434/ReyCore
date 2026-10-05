using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using ReyplastCoreAPI.Application.Interfaces.Services;

namespace ReyplastCoreAPI.Infrastructure.Integrations.Communication;

/// <summary>Configuración de los avisos de fallos de sincronización (sección SyncAlerts).</summary>
public sealed class SyncAlertOptions
{
    public const string SectionName = "SyncAlerts";

    /// <summary>false = el fallo solo queda en el log (por ejemplo en Development).</summary>
    public bool Enabled { get; set; }

    /// <summary>URL de CommunicationAPI, que envía el correo con su SMTP (POST api/applications/{AppCode}/emails).</summary>
    public string CommunicationApiBaseUrl { get; set; } = string.Empty;

    public string AppCode { get; set; } = "CORE";

    /// <summary>Destinatarios del aviso.</summary>
    public string[] To { get; set; } = Array.Empty<string>();

    public int TimeoutSeconds { get; set; } = 15;
}

/// <summary>Envía el aviso por correo a través de CommunicationAPI (mismo contrato que usa MasterLogic).</summary>
internal sealed class CommunicationApiSyncFailureAlerter : ISyncFailureAlerter
{
    private readonly HttpClient _httpClient;
    private readonly SyncAlertOptions _options;
    private readonly ILogger<CommunicationApiSyncFailureAlerter> _logger;

    public CommunicationApiSyncFailureAlerter(HttpClient httpClient, IOptions<SyncAlertOptions> options,
        ILogger<CommunicationApiSyncFailureAlerter> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(string subject, string body, CancellationToken cancellationToken = default)
    {
        string[] recipients = _options.To.Where(address => !string.IsNullOrWhiteSpace(address)).ToArray();
        if (!_options.Enabled || recipients.Length == 0 || _httpClient.BaseAddress is null)
        {
            _logger.LogWarning("Aviso de sincronización no enviado (SyncAlerts deshabilitado o sin destinatarios): {Subject}", subject);
            return;
        }

        try
        {
            string appCode = Uri.EscapeDataString(_options.AppCode);
            // Asunto: CommunicationAPI acepta hasta 250 caracteres y sin saltos de línea.
            string header = subject.Replace('\r', ' ').Replace('\n', ' ');
            if (header.Length > 250) header = header[..250];
            using HttpResponseMessage response = await _httpClient.PostAsJsonAsync($"api/applications/{appCode}/emails",
                new { To = recipients, Cc = Array.Empty<string>(), Header = header, Body = body, IsHtml = false },
                cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                string detail = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("CommunicationAPI rechazó el aviso de sincronización ({Status}): {Detail}",
                    (int)response.StatusCode, detail);
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "No se pudo enviar el aviso de sincronización: {Subject}", subject);
        }
    }
}
