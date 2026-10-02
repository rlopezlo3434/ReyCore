using ReyplastCoreAPI.Application.DTOs.Integrations.Osis;

namespace ReyplastCoreAPI.Application.Interfaces.Integrations;

public interface IOsisOrderReader
{
    Task<OsisOrderPage> ReadRangePageAsync(DateTime dateFrom, DateTime dateTo, string? cursor,
        CancellationToken cancellationToken = default);
    /// <summary>Pedidos APROBADOS (PPC_SITPPC) del día <paramref name="date"/> (fecha PPC_FECCRE).</summary>
    Task<OsisOrderPage> ReadApprovedDayPageAsync(DateTime date, string? cursor,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OsisOrderRecord>> ReadByDocNumAsync(string docNum,
        CancellationToken cancellationToken = default);
}
