using ReyplastCoreAPI.Application.DTOs.Integrations.Sap;

namespace ReyplastCoreAPI.Application.Interfaces.Integrations;

/// <summary>Puerto de lectura del API de pedidos SAP.</summary>
public interface ISapOrderReader
{
    Task<IReadOnlyList<SapOrderRecord>> ReadRangeAsync(
        DateTime dateFrom, DateTime dateTo, CancellationToken cancellationToken = default);
    Task<SapOrderRecord?> ReadByDocNumAsync(string docNum,
        CancellationToken cancellationToken = default);
}
