using System.Globalization;
using DevContextMcp.Server.Configuration;
using DevContextMcp.Server.Core.Infrastructure;
using DevContextMcp.Server.Core.Models.Analytics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
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
        var group = app.MapGroup("/api/analytics");
        group.MapGet("/summary", GetSummaryAsync);
        group.MapGet("/timeseries", GetTimeSeriesAsync);
        group.MapGet("/tools", GetToolsAsync);
        group.MapGet("/recent", GetRecentAsync);
    }

    private static async Task<IResult> GetSummaryAsync(
        HttpContext context,
        IToolInvocationReadStore store,
        IOptions<DevContextMcpOptions> options,
        CancellationToken cancellationToken)
    {
        if (!TryGetWindow(context.Request, out var window, out var error))
        {
            return Results.BadRequest(new { error });
        }

        var summary = await store.GetSummaryAsync(ResolvePath(options.Value), window, cancellationToken);
        return Results.Json(summary);
    }

    private static async Task<IResult> GetTimeSeriesAsync(
        HttpContext context,
        IToolInvocationReadStore store,
        IOptions<DevContextMcpOptions> options,
        CancellationToken cancellationToken)
    {
        if (!TryGetWindow(context.Request, out var window, out var error))
        {
            return Results.BadRequest(new { error });
        }

        var bucket = context.Request.Query.TryGetValue("bucket", out var bucketValue)
            && !string.IsNullOrWhiteSpace(bucketValue)
                ? bucketValue.ToString().ToLowerInvariant()
                : "hour";
        if (bucket is not ("hour" or "day"))
        {
            return Results.BadRequest(new { error = "bucket must be 'hour' or 'day'." });
        }

        var tool = context.Request.Query.TryGetValue("tool", out var toolValue)
            && !string.IsNullOrWhiteSpace(toolValue)
                ? toolValue.ToString()
                : null;

        var series = await store.GetTimeSeriesAsync(
            ResolvePath(options.Value),
            window,
            bucket,
            tool,
            cancellationToken);
        return Results.Json(series);
    }

    private static async Task<IResult> GetToolsAsync(
        HttpContext context,
        IToolInvocationReadStore store,
        IOptions<DevContextMcpOptions> options,
        CancellationToken cancellationToken)
    {
        if (!TryGetWindow(context.Request, out var window, out var error))
        {
            return Results.BadRequest(new { error });
        }

        var tools = await store.GetToolBreakdownAsync(ResolvePath(options.Value), window, cancellationToken);
        return Results.Json(new { tools });
    }

    private static async Task<IResult> GetRecentAsync(
        HttpContext context,
        IToolInvocationReadStore store,
        IOptions<DevContextMcpOptions> options,
        CancellationToken cancellationToken)
    {
        if (!TryGetWindow(context.Request, out var window, out var error))
        {
            return Results.BadRequest(new { error });
        }

        var limit = DefaultRecentLimit;
        if (context.Request.Query.TryGetValue("limit", out var limitValue)
            && !string.IsNullOrWhiteSpace(limitValue))
        {
            if (!int.TryParse(
                    limitValue,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out limit))
            {
                return Results.BadRequest(new { error = "limit must be an integer." });
            }
        }

        limit = Math.Clamp(limit, 1, MaxRecentLimit);
        var calls = await store.GetRecentAsync(ResolvePath(options.Value), window, limit, cancellationToken);
        return Results.Json(new { calls });
    }

    private static bool TryGetWindow(
        HttpRequest request,
        out AnalyticsWindow window,
        out string? error)
    {
        var now = DateTimeOffset.UtcNow;
        var from = now.AddHours(-24);
        var to = now;
        error = null;
        window = new AnalyticsWindow(from, to);

        if (request.Query.TryGetValue("from", out var fromValue)
            && !string.IsNullOrWhiteSpace(fromValue))
        {
            if (!TryParseTimestamp(fromValue.ToString(), out from))
            {
                error = "Invalid 'from' timestamp.";
                return false;
            }
        }

        if (request.Query.TryGetValue("to", out var toValue)
            && !string.IsNullOrWhiteSpace(toValue))
        {
            if (!TryParseTimestamp(toValue.ToString(), out to))
            {
                error = "Invalid 'to' timestamp.";
                return false;
            }
        }

        if (from >= to)
        {
            error = "'from' must be earlier than 'to'.";
            return false;
        }

        window = new AnalyticsWindow(from, to);
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
