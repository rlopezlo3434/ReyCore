using ReyplastCoreAPI.Application.DTOs.Channel.Response;
using ReyplastCoreAPI.Application.DTOs.Integrations.Osis;
using ReyplastCoreAPI.Application.Interfaces.Integrations;
using ReyplastCoreAPI.Application.Interfaces.Repositories;
using ReyplastCoreAPI.Application.Interfaces.Services;
using ReyplastCoreAPI.Domain.Catalog;
using ReyplastCoreAPI.Domain.Catalog.Entities;
using ReyplastCoreAPI.Shared.Mappers;
using ReyplastCoreAPI.Shared.Helpers;

namespace ReyplastCoreAPI.Application.Services.Channels;

/// <summary>Sincroniza y expone el catálogo local de canales de venta.</summary>
public sealed class ChannelService : IChannelService
{
    private readonly IChannelRepository _channelRepository;
    private readonly IOsisChannelReader _osisReader;

    public ChannelService(
        IChannelRepository channelRepository,
        IOsisChannelReader osisReader)
    {
        _channelRepository = channelRepository;
        _osisReader = osisReader;
    }

    public async Task<List<ChannelResponse>> GetAllAsync(
        bool? isActive, string? sourceSystem, CancellationToken cancellationToken = default)
    {
        List<Channel> channels = await _channelRepository.GetAllAsync(isActive, sourceSystem, cancellationToken);
        // Auto-sincroniza UNA vez si el catalogo local todavia esta vacio (nunca se corrio
        // POST channels/sync/osis) -- el origen es pequeño y rapido de leer (ver SyncFromOsisAsync),
        // asi que este bootstrap no compromete la velocidad del camino normal (tabla ya poblada,
        // que es una simple lectura local sin tocar OSIS).
        if (channels.Count == 0)
        {
            await SyncFromOsisAsync(cancellationToken);
            channels = await _channelRepository.GetAllAsync(isActive, sourceSystem, cancellationToken);
        }
        return channels.Select(ChannelMapper.ToResponse).ToList();
    }

    public async Task<ChannelSyncResponse> SyncFromOsisAsync(CancellationToken cancellationToken = default)
    {
        DateTime now = DateHelper.PeruNow();
        // Se graba como SyncExecutionId para saber qué canales vio esta ejecución (los demás se apagan).
        Guid executionId = Guid.NewGuid();
        ChannelSyncResponse response = new() { ExecutionId = executionId, Status = SyncStatuses.Running };
        IReadOnlyList<OsisChannelRecord> source = await _osisReader.ReadAllAsync(cancellationToken);
        if (source.Count == 0)
            throw new Shared.Exceptions.AppException("OSIS no devolvió ningún canal.");

        List<Channel> existing = await _channelRepository.GetByCodesAsync(
            source.Select(record => record.ChannelCode).ToArray(), SourceSystems.Osis, cancellationToken);
        Dictionary<string, Channel> existingByCode = existing
            .ToDictionary(channel => channel.Code, StringComparer.OrdinalIgnoreCase);
        List<Channel> toInsert = new();

        foreach (OsisChannelRecord record in source)
        {
            bool isNew = !existingByCode.TryGetValue(record.ChannelCode, out Channel? channel);
            channel ??= new Channel { Id = Guid.NewGuid(), Code = record.ChannelCode, CreatedAt = now };
            bool changed = isNew || HasChanged(channel, record);

            channel.Name = record.ChannelName;
            channel.SourceSystem = SourceSystems.Osis;
            channel.IsActive = true;
            channel.SyncExecutionId = executionId;
            channel.LastSynchronizedAt = now;

            if (isNew) { toInsert.Add(channel); response.ChannelsCreated++; }
            else if (changed) { channel.ModifiedAt = now; response.ChannelsChanged++; }
            else response.ChannelsUnchanged++;
        }

        if (toInsert.Count > 0) _channelRepository.AddRange(toInsert);

        // Un canal que ya no aparece en OSIS (por ejemplo, se dejó de usar) se apaga en vez
        // de borrarse -- otras tablas (Orders) pueden seguir referenciándolo por código.
        List<Channel> missing = await _channelRepository.GetActiveMissingFromExecutionAsync(
            executionId, SourceSystems.Osis, cancellationToken);
        foreach (Channel channel in missing)
        {
            channel.IsActive = false;
            channel.LastSynchronizedAt = now;
            channel.ModifiedAt = now;
            response.ChannelsDeactivated++;
        }

        await _channelRepository.SaveChangesAsync(cancellationToken);
        response.Status = SyncStatuses.Completed;
        return response;
    }

    private static bool HasChanged(Channel channel, OsisChannelRecord source) =>
        !string.Equals(channel.Name, source.ChannelName, StringComparison.Ordinal)
        || !channel.IsActive
        || !string.Equals(channel.SourceSystem, SourceSystems.Osis, StringComparison.Ordinal);

    public async Task<List<ChannelGroupResponse>> GetGroupsAsync(CancellationToken cancellationToken = default)
    {
        List<ChannelGroup> groups = await _channelRepository.GetGroupsAsync(cancellationToken);
        Dictionary<Guid, int> counts = await _channelRepository.CountChannelsByGroupAsync(cancellationToken);
        return groups.Select(group =>
            ChannelMapper.ToResponse(group, counts.TryGetValue(group.Id, out int count) ? count : 0)).ToList();
    }

    public async Task<ChannelGroupResponse> CreateGroupAsync(
        ChannelGroupCreateRequest request, CancellationToken cancellationToken = default)
    {
        string name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new Shared.Exceptions.AppException("El nombre del grupo es obligatorio.");

        ChannelGroup group = new() { Id = Guid.NewGuid(), Name = name, CreatedAt = DateHelper.PeruNow() };
        _channelRepository.AddGroup(group);
        try
        {
            await _channelRepository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (IsUniqueConstraintViolation(exception))
        {
            throw new Shared.Exceptions.AppException($"Ya existe un grupo de canales llamado \"{name}\".", 409);
        }
        return ChannelMapper.ToResponse(group, 0);
    }

    public async Task<ChannelResponse> AssignGroupAsync(
        Guid channelId, ChannelGroupAssignmentRequest request, CancellationToken cancellationToken = default)
    {
        Channel channel = await _channelRepository.GetByIdAsync(channelId, cancellationToken)
            ?? throw new Shared.Exceptions.AppException("El canal indicado no existe.", 404);

        if (request.ChannelGroupId.HasValue)
        {
            ChannelGroup group = await _channelRepository.GetGroupByIdAsync(request.ChannelGroupId.Value, cancellationToken)
                ?? throw new Shared.Exceptions.AppException("El grupo indicado no existe.", 404);
            channel.ChannelGroupId = group.Id;
            channel.ChannelGroup = group;
        }
        else
        {
            channel.ChannelGroupId = null;
            channel.ChannelGroup = null;
        }

        await _channelRepository.SaveChangesAsync(cancellationToken);
        return ChannelMapper.ToResponse(channel);
    }

    private static bool IsUniqueConstraintViolation(Exception exception) =>
        exception.InnerException?.Message.Contains("UX_ChannelGroups_Name", StringComparison.OrdinalIgnoreCase) == true;
}
