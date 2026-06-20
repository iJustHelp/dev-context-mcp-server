namespace DevContextMcp.Server.Core.Models.Analytics;

/// <summary>
/// Counts of invocations by terminal status.
/// </summary>
public sealed record StatusBreakdown(long Success, long Error, long Canceled);
