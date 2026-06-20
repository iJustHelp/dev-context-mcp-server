using System.Globalization;
using DevContextMcp.Server.Configuration;
using DevContextMcp.Server.Core.Infrastructure;
using DevContextMcp.Server.Core.Models.Analytics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DevContextMcp.Server.Analytics;

/// <summary>
/// Read-only HTTP endpoints serving aggregate and recent-call analytics. Window
/// parameters default to the last 24 hours; invalid parameters return 400.
/// </summary>
internal static class AnalyticsEndpoints
{
    private const int DefaultRecentLimit = 50;
    private const int MaxRecentLimit = 500;

    public static void MapAnalyticsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/analytics").WithTags("Analytics");

        group.MapGet("/summary", GetSummaryAsync)
            .WithName("GetSummary")
            .Produces<AnalyticsSummary>()
            .Produces<ApiError>(StatusCodes.Status400BadRequest);

        group.MapGet("/timeseries", GetTimeSeriesAsync)
            .WithName("GetTimeSeries")
            .Produces<AnalyticsTimeSeries>()
            .Produces<ApiError>(StatusCodes.Status400BadRequest);

        group.MapGet("/tools", GetToolsAsync)
            .WithName("GetTools")
            .Produces<ToolBreakdownResponse>()
            .Produces<ApiError>(StatusCodes.Status400BadRequest);

        group.MapGet("/recent", GetRecentAsync)
            .WithName("GetRecent")
            .Produces<RecentCallsResponse>()
            .Produces<ApiError>(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> GetSummaryAsync(
        [FromQuery] string? from,
        [FromQuery] string? to,
        IToolInvocationReadStore store,
        IOptions<DevContextMcpOptions> options,
        CancellationToken cancellationToken)
    {
        if (!TryBuildWindow(from, to, out var window, out var error))
        {
            return Results.BadRequest(new ApiError(error!));
        }

        var summary = await store.GetSummaryAsync(ResolvePath(options.Value), window, cancellationToken);
        return Results.Json(summary);
    }

    private static async Task<IResult> GetTimeSeriesAsync(
        [FromQuery] string? from,
        [FromQuery] string? to,
        IToolInvocationReadStore store,
        IOptions<DevContextMcpOptions> options,
        CancellationToken cancellationToken,
        [FromQuery] string? bucket = null,
        [FromQuery] string? tool = null)
    {
        if (!TryBuildWindow(from, to, out var window, out var error))
        {
            return Results.BadRequest(new ApiError(error!));
        }

        var resolvedBucket = string.IsNullOrWhiteSpace(bucket)
            ? "hour"
            : bucket.ToLowerInvariant();
        if (resolvedBucket is not ("hour" or "day"))
        {
            return Results.BadRequest(new ApiError("bucket must be 'hour' or 'day'."));
        }

        var resolvedTool = string.IsNullOrWhiteSpace(tool) ? null : tool;

        var series = await store.GetTimeSeriesAsync(
            ResolvePath(options.Value),
            window,
            resolvedBucket,
            resolvedTool,
            cancellationToken);
        return Results.Json(series);
    }

    private static async Task<IResult> GetToolsAsync(
        [FromQuery] string? from,
        [FromQuery] string? to,
        IToolInvocationReadStore store,
        IOptions<DevContextMcpOptions> options,
        CancellationToken cancellationToken)
    {
        if (!TryBuildWindow(from, to, out var window, out var error))
        {
            return Results.BadRequest(new ApiError(error!));
        }

        var tools = await store.GetToolBreakdownAsync(ResolvePath(options.Value), window, cancellationToken);
        return Results.Json(new ToolBreakdownResponse(tools));
    }

    private static async Task<IResult> GetRecentAsync(
        [FromQuery] string? from,
        [FromQuery] string? to,
        IToolInvocationReadStore store,
        IOptions<DevContextMcpOptions> options,
        CancellationToken cancellationToken,
        [FromQuery] int? limit = null)
    {
        if (!TryBuildWindow(from, to, out var window, out var error))
        {
            return Results.BadRequest(new ApiError(error!));
        }

        var resolvedLimit = Math.Clamp(limit ?? DefaultRecentLimit, 1, MaxRecentLimit);
        var calls = await store.GetRecentAsync(ResolvePath(options.Value), window, resolvedLimit, cancellationToken);
        return Results.Json(new RecentCallsResponse(calls));
    }

    private static bool TryBuildWindow(
        string? from,
        string? to,
        out AnalyticsWindow window,
        out string? error)
    {
        var now = DateTimeOffset.UtcNow;
        var fromValue = now.AddHours(-24);
        var toValue = now;
        error = null;
        window = new AnalyticsWindow(fromValue, toValue);

        if (!string.IsNullOrWhiteSpace(from) && !TryParseTimestamp(from, out fromValue))
        {
            error = "Invalid 'from' timestamp.";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(to) && !TryParseTimestamp(to, out toValue))
        {
            error = "Invalid 'to' timestamp.";
            return false;
        }

        if (fromValue >= toValue)
        {
            error = "'from' must be earlier than 'to'.";
            return false;
        }

        window = new AnalyticsWindow(fromValue, toValue);
        return true;
    }

    private static bool TryParseTimestamp(string value, out DateTimeOffset result) =>
        DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out result);

    private static string ResolvePath(DevContextMcpOptions options) =>
        Path.GetFullPath(options.Analytics.DatabasePath, AppContext.BaseDirectory);
}
