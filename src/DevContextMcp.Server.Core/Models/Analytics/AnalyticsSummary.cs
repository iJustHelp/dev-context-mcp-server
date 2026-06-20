namespace DevContextMcp.Server.Core.Models.Analytics;

/// <summary>
/// Overall analytics for a window: totals, status distribution, and latency.
/// </summary>
public sealed record AnalyticsSummary(
    DateTimeOffset From,
    DateTimeOffset To,
    long TotalCalls,
    StatusBreakdown StatusCounts,
    LatencySummary LatencyMs);
