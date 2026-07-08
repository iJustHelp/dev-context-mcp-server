using DevContextMcp.Infrastructure.Analytics;
using DevContextMcp.Server.Core.Models.Analytics;
using DevContextMcp.Server.Core.Models.Context;

namespace DevContextMcp.UnitTests.Analytics;

// SqliteAnalyticsStore has no injected collaborators, so per the test standard it
// is exercised directly without Moq, against a temporary database file.
public sealed class SqliteAnalyticsStoreTests : IDisposable
{
    private static readonly DateTimeOffset Base =
        new(2026, 6, 19, 10, 0, 0, TimeSpan.Zero);

    private static readonly AnalyticsWindow Window = new(
        new DateTimeOffset(2026, 6, 19, 9, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 6, 19, 12, 0, 0, TimeSpan.Zero));

    private readonly string _databasePath =
        Path.Combine(Path.GetTempPath(), $"analytics-{Guid.NewGuid():N}.db");

    private readonly SqliteAnalyticsStore _target = new();

    // Purpose: returns an empty summary when the analytics database does not exist
    [Fact]
    public async Task GetSummaryAsync_DatabaseMissing_ReturnsEmptySummary()
    {
        // arrange

        // act
        var actual = await _target.GetSummaryAsync(_databasePath, Window, CancellationToken.None);

        // assert
        Assert.False(File.Exists(_databasePath));
        Assert.Equal(0, actual.TotalCalls);
        Assert.Equal(0, actual.StatusCounts.Success);
        Assert.Equal(0, actual.LatencyMs.Max);
    }

    // Purpose: returns no tool rows when the analytics database does not exist
    [Fact]
    public async Task GetToolBreakdownAsync_DatabaseMissing_ReturnsEmpty()
    {
        // arrange

        // act
        var actual = await _target.GetToolBreakdownAsync(_databasePath, Window, CancellationToken.None);

        // assert
        Assert.Empty(actual);
    }

    // Purpose: returns no user rows when the analytics database does not exist
    [Fact]
    public async Task GetUserBreakdownAsync_DatabaseMissing_ReturnsEmpty()
    {
        // arrange

        // act
        var actual = await _target.GetUserBreakdownAsync(_databasePath, Window, CancellationToken.None);

        // assert
        Assert.Empty(actual);
    }

    // Purpose: returns no tool-result rows when the analytics database does not exist
    [Fact]
    public async Task GetToolResultBreakdownAsync_DatabaseMissing_ReturnsEmpty()
    {
        // arrange

        // act
        var actual = await _target.GetToolResultBreakdownAsync(_databasePath, Window, CancellationToken.None);

        // assert
        Assert.Empty(actual);
    }

    // Purpose: returns no time buckets when the analytics database does not exist
    [Fact]
    public async Task GetTimeSeriesAsync_DatabaseMissing_ReturnsEmpty()
    {
        // arrange

        // act
        var actual = await _target.GetTimeSeriesAsync(_databasePath, Window, "hour", null, CancellationToken.None);

        // assert
        Assert.Empty(actual.Points);
    }

    // Purpose: returns no recent calls when the analytics database does not exist
    [Fact]
    public async Task GetRecentAsync_DatabaseMissing_ReturnsEmpty()
    {
        // arrange

        // act
        var actual = await _target.GetRecentAsync(_databasePath, Window, 50, CancellationToken.None);

        // assert
        Assert.Empty(actual);
    }

    // Purpose: aggregates total, status counts, and latency for the window
    [Fact]
    public async Task GetSummaryAsync_WithSeededEvents_AggregatesStatusAndLatency()
    {
        // arrange
        await SeedAsync();

        // act
        var actual = await _target.GetSummaryAsync(_databasePath, Window, CancellationToken.None);

        // assert
        Assert.Equal(6, actual.TotalCalls);
        Assert.Equal(5, actual.StatusCounts.Success);
        Assert.Equal(1, actual.StatusCounts.Error);
        Assert.Equal(0, actual.StatusCounts.Canceled);
        Assert.Equal(25.833, actual.LatencyMs.Avg, 3);
        Assert.Equal(25, actual.LatencyMs.P50, 3);
        Assert.Equal(47.5, actual.LatencyMs.P95, 3);
        Assert.Equal(50, actual.LatencyMs.Max, 3);
    }

    // Purpose: computes per-tool counts, shares, and latency ordered by count
    [Fact]
    public async Task GetToolBreakdownAsync_WithSeededEvents_ComputesCountsSharesAndLatency()
    {
        // arrange
        await SeedAsync();

        // act
        var actual = await _target.GetToolBreakdownAsync(_databasePath, Window, CancellationToken.None);

        // assert
        Assert.Collection(
            actual,
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

    // Purpose: groups call counts by resolved analytics user
    [Fact]
    public async Task GetUserBreakdownAsync_WithSeededEvents_GroupsByUser()
    {
        // arrange
        await SeedAsync();

        // act
        var actual = await _target.GetUserBreakdownAsync(_databasePath, Window, CancellationToken.None);

        // assert
        Assert.Collection(
            actual,
            alice =>
            {
                Assert.Equal("alice", alice.UserName);
                Assert.Equal(5, alice.Count);
            },
            bob =>
            {
                Assert.Equal("bob", bob.UserName);
                Assert.Equal(1, bob.Count);
            });
    }

    // Purpose: groups call counts by tool-level result status
    [Fact]
    public async Task GetToolResultBreakdownAsync_WithSeededEvents_GroupsByToolResult()
    {
        // arrange
        await SeedAsync();

        // act
        var actual = await _target.GetToolResultBreakdownAsync(_databasePath, Window, CancellationToken.None);

        // assert
        Assert.Collection(
            actual,
            ok =>
            {
                Assert.Equal("ok", ok.ToolResultStatus);
                Assert.Equal(3, ok.Count);
            },
            error =>
            {
                Assert.Equal("error", error.ToolResultStatus);
                Assert.Equal(1, error.Count);
            },
            insufficientEvidence =>
            {
                Assert.Equal("insufficient_evidence", insufficientEvidence.ToolResultStatus);
                Assert.Equal(1, insufficientEvidence.Count);
            },
            notFound =>
            {
                Assert.Equal("not_found", notFound.ToolResultStatus);
                Assert.Equal(1, notFound.Count);
            });
    }

    // Purpose: groups call counts into hourly buckets
    [Fact]
    public async Task GetTimeSeriesAsync_HourBucket_GroupsByHour()
    {
        // arrange
        await SeedAsync();

        // act
        var actual = await _target.GetTimeSeriesAsync(_databasePath, Window, "hour", null, CancellationToken.None);

        // assert
        Assert.Equal("hour", actual.Bucket);
        Assert.Collection(
            actual.Points,
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

    // Purpose: restricts the time series to the requested tool
    [Fact]
    public async Task GetTimeSeriesAsync_ToolFilter_ReturnsOnlyMatchingTool()
    {
        // arrange
        await SeedAsync();

        // act
        var actual = await _target.GetTimeSeriesAsync(_databasePath, Window, "hour", "ping", CancellationToken.None);

        // assert
        Assert.Equal("ping", actual.Tool);
        var point = Assert.Single(actual.Points);
        Assert.Equal(1, point.Count);
    }

    // Purpose: returns the newest calls first and respects the limit
    [Fact]
    public async Task GetRecentAsync_WithLimit_ReturnsNewestFirst()
    {
        // arrange
        await SeedAsync();

        // act
        var actual = await _target.GetRecentAsync(_databasePath, Window, 2, CancellationToken.None);

        // assert
        Assert.Equal(2, actual.Count);
        Assert.Equal("ping", actual[0].ToolName);
        Assert.Equal("bob", actual[0].UserName);
        Assert.Equal("ok", actual[0].ToolResultStatus);
        Assert.True(actual[0].StartedAt >= actual[1].StartedAt);
    }

    // Purpose: excludes events that fall outside the requested window
    [Fact]
    public async Task GetSummaryAsync_NarrowWindow_ExcludesEventsOutsideRange()
    {
        // arrange
        await SeedAsync();
        var narrow = new AnalyticsWindow(
            new DateTimeOffset(2026, 6, 19, 10, 30, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 19, 12, 0, 0, TimeSpan.Zero));

        // act
        var actual = await _target.GetSummaryAsync(_databasePath, narrow, CancellationToken.None);

        // assert
        Assert.Equal(1, actual.TotalCalls);
    }

    // Purpose: marks ok rows as not clickable and non-ok rows as having detail
    [Fact]
    public async Task GetRecentAsync_WithSeededEvents_SetsHasDetail()
    {
        // arrange
        await SeedAsync();

        // act
        var actual = await _target.GetRecentAsync(_databasePath, Window, 50, CancellationToken.None);

        // assert
        var okCall = Assert.Single(actual, call => call.Id == "p1");
        Assert.False(okCall.HasDetail);
        Assert.True(Assert.Single(actual, call => call.Id == "q2").HasDetail);
        Assert.True(Assert.Single(actual, call => call.Id == "q5").HasDetail);
    }

    // Purpose: returns null when the requested call id is not found
    [Fact]
    public async Task GetRecentDetailAsync_MissingId_ReturnsNull()
    {
        // arrange
        await SeedAsync();

        // act
        var actual = await _target.GetRecentDetailAsync(
            _databasePath,
            Window,
            "missing",
            CancellationToken.None);

        // assert
        Assert.Null(actual);
    }

    // Purpose: round-trips stored detail JSON for a not-ok call
    [Fact]
    public async Task GetRecentDetailAsync_WithStoredDetail_ReturnsParsedDetail()
    {
        // arrange
        const string detailJson =
            """{"errors":[{"code":"library_not_found","message":"Library was not found."}],"resolvedContext":{"libraryId":"nuget:qa/Demo.Cities","sourceId":null,"environment":"qa","version":"1.0.0","versionSelectionReason":"recommended"}}""";
        await _target.AppendAsync(
            _databasePath,
            [
                Record(
                    "q2",
                    "query_docs",
                    "alice",
                    Base,
                    20,
                    AnalyticsStatus.Success,
                    "not_found",
                    null,
                    detailJson),
            ],
            CancellationToken.None);

        // act
        var actual = await _target.GetRecentDetailAsync(
            _databasePath,
            Window,
            "q2",
            CancellationToken.None);

        // assert
        Assert.NotNull(actual);
        Assert.Equal("not_found", actual.ToolResultStatus);
        Assert.NotNull(actual.Detail);
        var error = Assert.Single(actual.Detail!.Errors);
        Assert.Equal("library_not_found", error.Code);
        Assert.Equal("Library was not found.", error.Message);
        Assert.Equal("nuget:qa/Demo.Cities", actual.Detail.ResolvedContext!.LibraryId);
    }

    // Purpose: returns transport error type even when detail JSON is absent
    [Fact]
    public async Task GetRecentDetailAsync_WithErrorType_ReturnsErrorType()
    {
        // arrange
        await SeedAsync();

        // act
        var actual = await _target.GetRecentDetailAsync(
            _databasePath,
            Window,
            "q5",
            CancellationToken.None);

        // assert
        Assert.NotNull(actual);
        Assert.Equal(nameof(InvalidOperationException), actual!.ErrorType);
        Assert.Equal("error", actual.ToolResultStatus);
    }

    private Task SeedAsync() =>
        _target.AppendAsync(
            _databasePath,
            [
                Record("q1", "query_docs", "alice", Base, 10, AnalyticsStatus.Success, "ok", null),
                Record("q2", "query_docs", "alice", Base.AddMinutes(1), 20, AnalyticsStatus.Success, "not_found", null),
                Record("q3", "query_docs", "alice", Base.AddMinutes(2), 30, AnalyticsStatus.Success, "insufficient_evidence", null),
                Record("q4", "query_docs", "alice", Base.AddMinutes(3), 40, AnalyticsStatus.Success, "ok", null),
                Record("q5", "query_docs", "alice", Base.AddMinutes(4), 50, AnalyticsStatus.Error, "error", "InvalidOperationException"),
                Record("p1", "ping", "bob", Base.AddHours(1), 5, AnalyticsStatus.Success, "ok", null),
            ],
            CancellationToken.None);

    private static ToolInvocationRecord Record(
        string id,
        string tool,
        string user,
        DateTimeOffset startedAt,
        double durationMs,
        string status,
        string toolResultStatus,
        string? errorType,
        string? resultDetailJson = null) =>
        new(
            id,
            tool,
            user,
            startedAt,
            durationMs,
            status,
            toolResultStatus,
            errorType,
            null,
            null,
            resultDetailJson);

    public void Dispose()
    {
        foreach (var suffix in new[] { string.Empty, "-wal", "-shm" })
        {
            var path = _databasePath + suffix;
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
