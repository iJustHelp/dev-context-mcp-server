namespace DevContextMcp.Server.Core.Models.Analytics;

/// <summary>
/// Per-tool usage: call count, share of total, status distribution, and latency.
/// </summary>
public sealed record ToolUsage(
    string ToolName,
    long Count,
    double Share,
    StatusBreakdown StatusCounts,
    LatencySummary LatencyMs);
