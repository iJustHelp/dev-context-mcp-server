namespace DevContextMcp.Server.Core.Models.Analytics;

/// <summary>
/// Call counts bucketed over time, optionally filtered to one tool.
/// </summary>
public sealed record AnalyticsTimeSeries(
    string Bucket,
    string? Tool,
    IReadOnlyList<TimeBucketPoint> Points);
