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
    /// Ranked evidence fragments.
    /// </summary>
    [JsonPropertyName("evidence")]
    public IReadOnlyList<EvidenceItem> Evidence { get; init; } = [];

    /// <summary>
    /// Stable citations for evidence and source artifacts.
    /// </summary>
    [JsonPropertyName("citations")]
    public IReadOnlyList<Citation> Citations { get; init; } = [];

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
