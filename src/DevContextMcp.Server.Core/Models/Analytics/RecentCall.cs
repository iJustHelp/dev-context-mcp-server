namespace DevContextMcp.Server.Core.Models.Analytics;

/// <summary>
/// A recent invocation as shown in the recent-calls table.
/// </summary>
public sealed record RecentCall(
    string Id,
    string ToolName,
    string UserName,
    DateTimeOffset StartedAt,
    double DurationMs,
    string Status);
