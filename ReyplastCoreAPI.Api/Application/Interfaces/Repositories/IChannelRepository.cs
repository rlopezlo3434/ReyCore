using ReyplastCoreAPI.Domain.Catalog.Entities;

namespace ReyplastCoreAPI.Application.Interfaces.Repositories;

public interface IChannelRepository
{
    Task<List<Channel>> GetAllAsync(bool? isActive, string? sourceSystem, CancellationToken cancellationToken = default);
    Task<List<Channel>> GetByCodesAsync(IReadOnlyCollection<string> codes, string sourceSystem, CancellationToken cancellationToken = default);
    Task<List<Channel>> GetActiveMissingFromExecutionAsync(Guid executionId, string sourceSystem, CancellationToken cancellationToken = default);
    void AddRange(IReadOnlyCollection<Channel> channels);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    // Agrupación normalizada (ver ChannelGroup) -- curada a mano desde RP_ReyPlastCoreBD, no sincronizada.
    Task<Channel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<ChannelGroup>> GetGroupsAsync(CancellationToken cancellationToken = default);
    Task<ChannelGroup?> GetGroupByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Dictionary<Guid, int>> CountChannelsByGroupAsync(CancellationToken cancellationToken = default);
    void AddGroup(ChannelGroup group);
}
