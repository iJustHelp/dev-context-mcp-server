namespace DevContextMcp.Server.Core.Models.Analytics;

/// <summary>
/// Response envelope for the recent-calls endpoint: serializes as { "calls": [...] }.
/// </summary>
public sealed record RecentCallsResponse(IReadOnlyList<RecentCall> Calls);
