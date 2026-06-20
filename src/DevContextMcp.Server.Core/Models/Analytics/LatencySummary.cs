namespace DevContextMcp.Server.Core.Models.Analytics;

/// <summary>
/// Latency statistics in milliseconds over a set of invocations.
/// </summary>
public sealed record LatencySummary(double Avg, double P50, double P95, double Max);
