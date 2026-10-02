namespace ReyplastCoreAPI.Application.DTOs.Integrations.Osis;

/// <summary>Canal OSIS ya normalizado, independiente de nombres físicos de XIMESA.</summary>
public sealed class OsisChannelRecord
{
    public string ChannelCode { get; init; } = string.Empty;
    public string ChannelName { get; init; } = string.Empty;
}
