using Microsoft.EntityFrameworkCore;
using ReyplastCoreAPI.Application.Interfaces.Repositories;
using ReyplastCoreAPI.Domain.Catalog.Entities;
using ReyplastCoreAPI.Infrastructure.Persistence;

namespace ReyplastCoreAPI.Infrastructure.Repositories;

public sealed class ChannelRepository : IChannelRepository
{
    private readonly ReyplastCoreDbContext _context;

    public ChannelRepository(ReyplastCoreDbContext context) => _context = context;

    public async Task<List<Channel>> GetAllAsync(
        bool? isActive, string? sourceSystem, CancellationToken cancellationToken = default)
    {
        IQueryable<Channel> query = _context.Channels.AsNoTracking().Include(channel => channel.ChannelGroup);
        if (isActive.HasValue) query = query.Where(channel => channel.IsActive == isActive.Value);
        if (!string.IsNullOrWhiteSpace(sourceSystem))
            query = query.Where(channel => channel.SourceSystem == sourceSystem);
        return await query.OrderBy(channel => channel.Name).ToListAsync(cancellationToken);
    }

    public async Task<Channel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Channels.Include(channel => channel.ChannelGroup)
            .FirstOrDefaultAsync(channel => channel.Id == id, cancellationToken);

    public async Task<List<ChannelGroup>> GetGroupsAsync(CancellationToken cancellationToken = default) =>
        await _context.ChannelGroups.AsNoTracking().OrderBy(group => group.Name).ToListAsync(cancellationToken);

    public async Task<ChannelGroup?> GetGroupByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.ChannelGroups.FirstOrDefaultAsync(group => group.Id == id, cancellationToken);

    public async Task<Dictionary<Guid, int>> CountChannelsByGroupAsync(CancellationToken cancellationToken = default) =>
        await _context.Channels.AsNoTracking()
            .Where(channel => channel.ChannelGroupId != null)
            .GroupBy(channel => channel.ChannelGroupId!.Value)
            .Select(group => new { GroupId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.GroupId, group => group.Count, cancellationToken);

    public void AddGroup(ChannelGroup group) => _context.ChannelGroups.Add(group);

    public async Task<List<Channel>> GetByCodesAsync(
        IReadOnlyCollection<string> codes, string sourceSystem, CancellationToken cancellationToken = default) =>
        await _context.Channels
            .Where(channel => channel.SourceSystem == sourceSystem && codes.Contains(channel.Code))
            .ToListAsync(cancellationToken);

    public async Task<List<Channel>> GetActiveMissingFromExecutionAsync(
        Guid executionId, string sourceSystem, CancellationToken cancellationToken = default) =>
        await _context.Channels
            .Where(channel => channel.SourceSystem == sourceSystem
                && channel.IsActive
                && channel.SyncExecutionId != null
                && channel.SyncExecutionId != executionId)
            .ToListAsync(cancellationToken);

    public void AddRange(IReadOnlyCollection<Channel> channels) => _context.Channels.AddRange(channels);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
