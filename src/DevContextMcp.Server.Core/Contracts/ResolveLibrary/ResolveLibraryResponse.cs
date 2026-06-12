using System.Text.Json.Serialization;
using DevContextMcp.Server.Core.Contracts.Common;

namespace DevContextMcp.Server.Core.Contracts.ResolveLibrary;

/// <summary>
/// Response for the resolve_library tool.
/// </summary>
public sealed record ResolveLibraryResponse : ToolResponse<ResolveLibraryResult>;

/// <summary>
/// Resolved library matches.
/// </summary>
public sealed record ResolveLibraryResult
{
    /// <summary>
    /// Matching libraries ranked by confidence.
    /// </summary>
    [JsonPropertyName("matches")]
    public IReadOnlyList<LibraryMatch> Matches { get; init; } = [];
}

/// <summary>
/// Candidate NuGet package.
/// </summary>
public sealed record LibraryMatch
{
    [JsonPropertyName("libraryId")]
    public required string LibraryId { get; init; }

    [JsonPropertyName("kind")]
    public required string Kind { get; init; }

    [JsonPropertyName("displayName")]
    public required string DisplayName { get; init; }

    [JsonPropertyName("environment")]
    public string? Environment { get; init; }

    [JsonPropertyName("sourceId")]
    public required string SourceId { get; init; }

    [JsonPropertyName("recommendedVersion")]
    public string? RecommendedVersion { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("confidence")]
    public double Confidence { get; init; }
}
