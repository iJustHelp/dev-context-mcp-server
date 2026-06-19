namespace DevContextMcp.Server.Core.Models.Analytics;

/// <summary>
/// One time bucket of the call-count time series.
/// </summary>
public sealed record TimeBucketPoint(DateTimeOffset BucketStart, long Count);
