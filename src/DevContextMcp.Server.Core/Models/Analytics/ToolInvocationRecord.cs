namespace DevContextMcp.Server.Core.Models.Analytics;

/// <summary>
/// A single metadata-only analytics event captured for one MCP tool invocation.
/// No request or response content is stored.
/// </summary>
public sealed record ToolInvocationRecord(
    string Id,
    string ToolName,
    string UserName,
    DateTimeOffset StartedAt,
    double DurationMs,
    string Status,
    string? ErrorType,
    long? RequestBytes,
    long? ResponseBytes);
