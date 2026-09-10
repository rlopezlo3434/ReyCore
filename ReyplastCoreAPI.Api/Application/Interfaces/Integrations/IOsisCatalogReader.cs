using ReyplastCoreAPI.Application.DTOs.Integrations.Osis;

namespace ReyplastCoreAPI.Application.Interfaces.Integrations;

/// <summary>Puerto de lectura del catálogo operativo de OSIS.</summary>
public interface IOsisCatalogReader
{
    Task<OsisCatalogPage> ReadPageAsync(
        string? afterProductCode,
        CancellationToken cancellationToken = default);
}
