using System.Text.Json.Serialization;

namespace DevContextMcp.Server.Core.Contracts.Common;

/// <summary>
/// A ranked evidence reference. Text content is returned in the tool-specific data payload.
/// </summary>
public sealed record EvidenceItem
{
    /// <summary>
    /// Source category for the evidence.
    /// </summary>
    [JsonPropertyName("kind")]
    public required string Kind { get; init; }

    /// <summary>
    /// Short title for the evidence item.
    /// </summary>
    [JsonPropertyName("title")]
    public required string Title { get; init; }

    /// <summary>
    /// Optional evidence text when not returned in the tool-specific data payload.
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; init; }

    /// <summary>
    /// Relevance score assigned by the retrieval layer.
    /// </summary>
    [JsonPropertyName("score")]
    public double? Score { get; init; }

    /// <summary>
    /// Citation URI for this evidence item.
    /// </summary>
    [JsonPropertyName("citationUri")]
    public string? CitationUri { get; init; }
}
