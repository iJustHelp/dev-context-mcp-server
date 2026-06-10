using System.Text.Json.Serialization;
using DevContextMcp.Server.Core.Contracts.Common;

namespace DevContextMcp.Server.Core.Contracts.ListVersions;

/// <summary>
/// Response for the list_versions tool.
/// </summary>
public sealed record ListVersionsResponse : ToolResponse<ListVersionsResult>;

/// <summary>
/// Version listing payload.
/// </summary>
public sealed record ListVersionsResult
{
    [JsonPropertyName("versions")]
    public IReadOnlyList<LibraryVersion> Versions { get; init; } = [];

    [JsonPropertyName("recommendedVersion")]
    public string? RecommendedVersion { get; init; }

    [JsonPropertyName("recommendedVersionReason")]
    public string? RecommendedVersionReason { get; init; }
}

public sealed record LibraryVersion
{
    [JsonPropertyName("version")]
    public required string Version { get; init; }

    [JsonPropertyName("listed")]
    public bool Listed { get; init; }

    [JsonPropertyName("prerelease")]
    public bool Prerelease { get; init; }

    [JsonPropertyName("deprecated")]
    public bool Deprecated { get; init; }

    [JsonPropertyName("indexed")]
    public bool Indexed { get; init; }

    [JsonPropertyName("publishedAt")]
    public DateTimeOffset? PublishedAt { get; init; }
}
