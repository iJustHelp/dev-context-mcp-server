using System.Text.Json.Serialization;

namespace DevContextMcp.Server.Core.Contracts.Common;

/// <summary>
/// Machine-readable error returned inside a successful tool response.
/// </summary>
public sealed record ToolError
{
    /// <summary>
    /// Stable error code.
    /// </summary>
    [JsonPropertyName("code")]
    public required string Code { get; init; }

    /// <summary>
    /// Human-readable error message.
    /// </summary>
    [JsonPropertyName("message")]
    public required string Message { get; init; }
}
