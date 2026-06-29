namespace DevContextMcp.Server.Core.Models.Analytics;

/// <summary>
/// Full detail for one recent invocation, including parsed not-ok metadata.
/// </summary>
public sealed record RecentCallDetail(
    string Id,
    string ToolName,
    string UserName,
    DateTimeOffset StartedAt,
    double DurationMs,
    string Status,
    string ToolResultStatus,
    string? ErrorType,
    ToolInvocationResultDetail? Detail);
