using System.Text.Json.Serialization;

namespace DevContextMcp.Server.Core.Contracts.Common;

/// <summary>
/// Common envelope for all MCP documentation tool responses.
/// </summary>
/// <typeparam name="TData">Tool-specific payload type.</typeparam>
public abstract record ToolResponse<TData>
{
    /// <summary>
    /// Machine-readable response status.
    /// </summary>
    [JsonPropertyName("status")]
    public ToolResultStatus Status { get; init; } = ToolResultStatus.NotReady;

    /// <summary>
    /// Tool-specific payload. This may be null when no payload is available.
    /// </summary>
    [JsonPropertyName("data")]
    public TData? Data { get; init; }

    /// <summary>
    /// Source and version context searched by the tool.
    /// </summary>
    [JsonPropertyName("resolvedContext")]
    public ResolvedContext? ResolvedContext { get; init; }

    /// <summary>
    /// Optional ranked evidence metadata. Omitted on normal successful retrieval
    /// responses; agents should use ordered <see cref="Data"/> items and their
    /// <c>citationUri</c> values instead.
    /// </summary>
    [JsonPropertyName("evidence")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<EvidenceItem>? Evidence { get; init; }

    /// <summary>
    /// Optional deduplicated citations for envelope consumers. Omitted on normal
    /// successful retrieval responses; agents should use <c>citationUri</c> on
    /// <see cref="Data"/> items instead.
    /// </summary>
    [JsonPropertyName("citations")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<Citation>? Citations { get; init; }

    /// <summary>
    /// Non-fatal warnings.
    /// </summary>
    [JsonPropertyName("warnings")]
    public IReadOnlyList<ToolWarning> Warnings { get; init; } = [];

    /// <summary>
    /// Tool-level errors returned as data rather than protocol errors.
    /// </summary>
    [JsonPropertyName("errors")]
    public IReadOnlyList<ToolError> Errors { get; init; } = [];
}
