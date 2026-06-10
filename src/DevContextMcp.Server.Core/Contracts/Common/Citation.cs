using System.Text.Json.Serialization;

namespace DevContextMcp.Server.Core.Contracts.Common;

/// <summary>
/// Stable reference to an indexed source artifact.
/// </summary>
public sealed record Citation
{
    /// <summary>
    /// Stable MCP resource URI for the cited artifact.
    /// </summary>
    [JsonPropertyName("uri")]
    public required string Uri { get; init; }

    /// <summary>
    /// Human-readable source label.
    /// </summary>
    [JsonPropertyName("label")]
    public required string Label { get; init; }

    /// <summary>
    /// Optional source location such as a line range or member name.
    /// </summary>
    [JsonPropertyName("location")]
    public string? Location { get; init; }
}
