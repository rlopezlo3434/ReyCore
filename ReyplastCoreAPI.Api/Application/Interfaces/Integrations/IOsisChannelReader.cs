using ReyplastCoreAPI.Application.DTOs.Integrations.Osis;

namespace ReyplastCoreAPI.Application.Interfaces.Integrations;

/// <summary>Puerto de lectura del catálogo de canales de venta de OSIS.</summary>
public interface IOsisChannelReader
{
    /// <summary>Lee todos los códigos de canal distintos. Es un catálogo cerrado y pequeño
    /// (decenas de filas), así que no necesita paginación por cursor como Products/Orders.</summary>
    Task<IReadOnlyList<OsisChannelRecord>> ReadAllAsync(CancellationToken cancellationToken = default);
}
