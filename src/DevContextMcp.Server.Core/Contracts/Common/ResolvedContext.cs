using System.Text.Json.Serialization;

namespace DevContextMcp.Server.Core.Contracts.Common;

/// <summary>
/// Package, source, and version context used to produce a response.
/// </summary>
public sealed record ResolvedContext
{
    /// <summary>
    /// Stable library identifier, such as nuget:qa/Company.Customer.Client.
    /// </summary>
    [JsonPropertyName("libraryId")]
    public string? LibraryId { get; init; }

    /// <summary>
    /// Source identifier from configuration.
    /// </summary>
    [JsonPropertyName("sourceId")]
    public string? SourceId { get; init; }

    /// <summary>
    /// Deployment environment containing the selected source.
    /// </summary>
    [JsonPropertyName("environment")]
    public string? Environment { get; init; }

    /// <summary>
    /// Version searched by the tool.
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; init; }

    /// <summary>
    /// Reason the version was selected.
    /// </summary>
    [JsonPropertyName("versionSelectionReason")]
    public string? VersionSelectionReason { get; init; }
}
