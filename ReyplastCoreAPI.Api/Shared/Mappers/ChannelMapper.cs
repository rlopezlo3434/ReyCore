using ReyplastCoreAPI.Application.DTOs.Channel.Response;
using ReyplastCoreAPI.Domain.Catalog.Entities;

namespace ReyplastCoreAPI.Shared.Mappers;

public static class ChannelMapper
{
    public static ChannelResponse ToResponse(Channel channel) => new()
    {
        Id = channel.Id,
        Code = channel.Code,
        Name = channel.Name,
        IsActive = channel.IsActive,
        SourceSystem = channel.SourceSystem,
        SyncExecutionId = channel.SyncExecutionId,
        LastSynchronizedAt = channel.LastSynchronizedAt,
        ChannelGroupId = channel.ChannelGroupId,
        ChannelGroupName = channel.ChannelGroup?.Name
    };

    public static ChannelGroupResponse ToResponse(ChannelGroup group, int channelCount) => new()
    {
        Id = group.Id,
        Name = group.Name,
        ChannelCount = channelCount
    };
}
