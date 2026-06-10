using System.Text.Json.Serialization;

namespace DevContextMcp.Server.Core.Contracts.Common;

/// <summary>
/// Non-fatal warning attached to a tool response.
/// </summary>
public sealed record ToolWarning
{
    /// <summary>
    /// Stable warning code.
    /// </summary>
    [JsonPropertyName("code")]
    public required string Code { get; init; }

    /// <summary>
    /// Human-readable warning message.
    /// </summary>
    [JsonPropertyName("message")]
    public required string Message { get; init; }
}
