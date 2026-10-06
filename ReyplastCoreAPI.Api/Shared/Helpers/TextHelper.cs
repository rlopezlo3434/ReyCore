using ReyplastCoreAPI.Shared.Exceptions;

namespace ReyplastCoreAPI.Shared.Helpers;

/// <summary>Normalización de textos que llegan de requests y de los orígenes (OSIS/SAP).</summary>
public static class TextHelper
{
    /// <summary>Texto recortado, o null si viene vacío o solo con espacios.</summary>
    public static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Texto obligatorio recortado; error de negocio con <paramref name="message"/> si viene vacío.</summary>
    public static string Required(string? value, string message) =>
        string.IsNullOrWhiteSpace(value) ? throw new AppException(message) : value.Trim();

    /// <summary>Igual que <see cref="Required"/>, en mayúsculas (códigos: sistema de origen, appCode, ...).</summary>
    public static string RequiredUpper(string? value, string message) => Required(value, message).ToUpperInvariant();

    /// <summary>Valores de un filtro: acepta repetidos (?x=a&amp;x=b) y separados por coma (?x=a,b); recorta,
    /// quita vacíos y duplicados (sin distinguir mayúsculas).</summary>
    public static string[] SplitValues(IEnumerable<string>? values) => values?
        .SelectMany(value => value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray() ?? Array.Empty<string>();
}
