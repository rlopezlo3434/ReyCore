using ReyplastCoreAPI.Application.DTOs.Integrations.Osis;

namespace ReyplastCoreAPI.Application.Interfaces.Integrations;

public interface IOsisOrderReader
{
    Task<OsisOrderPage> ReadPageAsync(string? cursor, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OsisOrderRecord>> ReadByDocNumAsync(string docNum,
        CancellationToken cancellationToken = default);
}
