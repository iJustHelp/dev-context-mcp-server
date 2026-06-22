using DevContextMcp.Server.Core.Models.Analytics;

namespace DevContextMcp.Server.Core.Infrastructure;

/// <summary>
/// Read-only aggregate access over captured tool-invocation analytics events.
/// When the analytics database does not yet exist, queries return empty results.
/// </summary>
public interface IToolInvocationReadStore
{
    Task<AnalyticsSummary> GetSummaryAsync(
        string databasePath,
        AnalyticsWindow window,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ToolUsage>> GetToolBreakdownAsync(
        string databasePath,
        AnalyticsWindow window,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<UserBreakdownItem>> GetUserBreakdownAsync(
        string databasePath,
        AnalyticsWindow window,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ToolResultBreakdownItem>> GetToolResultBreakdownAsync(
        string databasePath,
        AnalyticsWindow window,
        CancellationToken cancellationToken);

    Task<AnalyticsTimeSeries> GetTimeSeriesAsync(
        string databasePath,
        AnalyticsWindow window,
        string bucket,
        string? tool,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<RecentCall>> GetRecentAsync(
        string databasePath,
        AnalyticsWindow window,
        int limit,
        CancellationToken cancellationToken);
}
