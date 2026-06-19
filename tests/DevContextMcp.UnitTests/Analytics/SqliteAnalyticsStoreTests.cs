using DevContextMcp.Infrastructure.Analytics;
using DevContextMcp.Server.Core.Models.Analytics;

namespace DevContextMcp.UnitTests.Analytics;

public sealed class SqliteAnalyticsStoreTests : IDisposable
{
    private static readonly DateTimeOffset Base =
        new(2026, 6, 19, 10, 0, 0, TimeSpan.Zero);

    private readonly string _databasePath =
        Path.Combine(Path.GetTempPath(), $"analytics-{Guid.NewGuid():N}.db");

    private readonly SqliteAnalyticsStore _store = new();

    private readonly AnalyticsWindow _window = new(
        new DateTimeOffset(2026, 6, 19, 9, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 6, 19, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task ReadsReturnEmptyWhenDatabaseMissing()
    {
        var summary = await _store.GetSummaryAsync(_databasePath, _window, default);
        var tools = await _store.GetToolBreakdownAsync(_databasePath, _window, default);
        var series = await _store.GetTimeSeriesAsync(_databasePath, _window, "hour", null, default);
        var recent = await _store.GetRecentAsync(_databasePath, _window, 50, default);

        Assert.False(File.Exists(_databasePath));
        Assert.Equal(0, summary.TotalCalls);
        Assert.Empty(tools);
        Assert.Empty(series.Points);
        Assert.Empty(recent);
    }

    [Fact]
    public async Task AppendThenSummaryAggregatesStatusAndLatency()
    {
        await SeedAsync();

        var summary = await _store.GetSummaryAsync(_databasePath, _window, default);

        Assert.Equal(6, summary.TotalCalls);
        Assert.Equal(5, summary.StatusCounts.Success);
        Assert.Equal(1, summary.StatusCounts.Error);
        Assert.Equal(0, summary.StatusCounts.Canceled);
        Assert.Equal(25.833, summary.LatencyMs.Avg, 3);
        Assert.Equal(25, summary.LatencyMs.P50, 3);
        Assert.Equal(47.5, summary.LatencyMs.P95, 3);
        Assert.Equal(50, summary.LatencyMs.Max, 3);
    }

    [Fact]
    public async Task ToolBreakdownComputesPerToolCountsSharesAndLatency()
    {
        await SeedAsync();

        var tools = await _store.GetToolBreakdownAsync(_databasePath, _window, default);

        // Ordered by count descending.
        Assert.Collection(
            tools,
            queryDocs =>
            {
                Assert.Equal("query_docs", queryDocs.ToolName);
                Assert.Equal(5, queryDocs.Count);
                Assert.Equal(4, queryDocs.StatusCounts.Success);
                Assert.Equal(1, queryDocs.StatusCounts.Error);
                Assert.Equal(5d / 6d, queryDocs.Share, 5);
                Assert.Equal(30, queryDocs.LatencyMs.Avg, 3);
                Assert.Equal(30, queryDocs.LatencyMs.P50, 3);
                Assert.Equal(48, queryDocs.LatencyMs.P95, 3);
                Assert.Equal(50, queryDocs.LatencyMs.Max, 3);
            },
            ping =>
            {
                Assert.Equal("ping", ping.ToolName);
                Assert.Equal(1, ping.Count);
                Assert.Equal(1d / 6d, ping.Share, 5);
                Assert.Equal(5, ping.LatencyMs.Max, 3);
            });
    }

    [Fact]
    public async Task TimeSeriesBucketsByHour()
    {
        await SeedAsync();

        var series = await _store.GetTimeSeriesAsync(_databasePath, _window, "hour", null, default);

        Assert.Equal("hour", series.Bucket);
        Assert.Collection(
            series.Points,
            ten =>
            {
                Assert.Equal(new DateTimeOffset(2026, 6, 19, 10, 0, 0, TimeSpan.Zero), ten.BucketStart);
                Assert.Equal(5, ten.Count);
            },
            eleven =>
            {
                Assert.Equal(new DateTimeOffset(2026, 6, 19, 11, 0, 0, TimeSpan.Zero), eleven.BucketStart);
                Assert.Equal(1, eleven.Count);
            });
    }

    [Fact]
    public async Task TimeSeriesFiltersByTool()
    {
        await SeedAsync();

        var series = await _store.GetTimeSeriesAsync(_databasePath, _window, "hour", "ping", default);

        Assert.Equal("ping", series.Tool);
        var point = Assert.Single(series.Points);
        Assert.Equal(1, point.Count);
    }

    [Fact]
    public async Task RecentReturnsNewestFirstAndRespectsLimit()
    {
        await SeedAsync();

        var recent = await _store.GetRecentAsync(_databasePath, _window, 2, default);

        Assert.Equal(2, recent.Count);
        Assert.Equal("ping", recent[0].ToolName);
        Assert.Equal("bob", recent[0].UserName);
        Assert.True(recent[0].StartedAt >= recent[1].StartedAt);
    }

    [Fact]
    public async Task WindowExcludesEventsOutsideRange()
    {
        await SeedAsync();
        var narrow = new AnalyticsWindow(
            new DateTimeOffset(2026, 6, 19, 10, 30, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 19, 12, 0, 0, TimeSpan.Zero));

        var summary = await _store.GetSummaryAsync(_databasePath, narrow, default);

        // Only the 11:00 ping falls inside the narrowed window.
        Assert.Equal(1, summary.TotalCalls);
    }

    private async Task SeedAsync()
    {
        await _store.AppendAsync(
            _databasePath,
            [
                Record("q1", "query_docs", "alice", Base, 10, AnalyticsStatus.Success, null),
                Record("q2", "query_docs", "alice", Base.AddMinutes(1), 20, AnalyticsStatus.Success, null),
                Record("q3", "query_docs", "alice", Base.AddMinutes(2), 30, AnalyticsStatus.Success, null),
                Record("q4", "query_docs", "alice", Base.AddMinutes(3), 40, AnalyticsStatus.Success, null),
                Record("q5", "query_docs", "alice", Base.AddMinutes(4), 50, AnalyticsStatus.Error, "InvalidOperationException"),
                Record("p1", "ping", "bob", Base.AddHours(1), 5, AnalyticsStatus.Success, null),
            ],
            default);
    }

    private static ToolInvocationRecord Record(
        string id,
        string tool,
        string user,
        DateTimeOffset startedAt,
        double durationMs,
        string status,
        string? errorType) =>
        new(id, tool, user, startedAt, durationMs, status, errorType, null, null);

    public void Dispose()
    {
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }

        foreach (var sidecar in new[] { "-wal", "-shm" })
        {
            var path = _databasePath + sidecar;
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
