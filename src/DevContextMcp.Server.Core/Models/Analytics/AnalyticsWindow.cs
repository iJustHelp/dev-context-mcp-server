namespace DevContextMcp.Server.Core.Models.Analytics;

/// <summary>
/// Half-open time window [From, To) used to scope analytics queries.
/// </summary>
public sealed record AnalyticsWindow(DateTimeOffset From, DateTimeOffset To);
