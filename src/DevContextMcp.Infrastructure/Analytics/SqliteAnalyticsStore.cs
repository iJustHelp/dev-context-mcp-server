using System.Globalization;
using DevContextMcp.Server.Core.Infrastructure;
using DevContextMcp.Server.Core.Models.Analytics;
using Microsoft.Data.Sqlite;

namespace DevContextMcp.Infrastructure.Analytics;

/// <summary>
/// SQLite-backed analytics store. Writes are append-only and self-create the database,
/// schema, and indexes with WAL journaling; reads compute aggregates in SQL and
/// percentiles in-process. The database is independent of the documentation index.
/// </summary>
internal sealed class SqliteAnalyticsStore : IToolInvocationWriteStore, IToolInvocationReadStore
{
    private const string TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffffff'Z'";

    public async Task AppendAsync(
        string databasePath,
        IReadOnlyList<ToolInvocationRecord> records,
        CancellationToken cancellationToken)
    {
        if (records.Count == 0)
        {
            return;
        }

        await using var connection = await OpenWriteAsync(databasePath, cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT OR IGNORE INTO tool_invocations
                (id, tool_name, user_name, started_at, duration_ms, status, tool_result_status,
                 error_type, request_bytes, response_bytes, result_detail_json)
            VALUES ($id, $tool, $user, $started, $duration, $status, $toolResultStatus,
                    $error, $request, $response, $detail);
            """;

        var id = command.Parameters.Add("$id", SqliteType.Text);
        var tool = command.Parameters.Add("$tool", SqliteType.Text);
        var user = command.Parameters.Add("$user", SqliteType.Text);
        var started = command.Parameters.Add("$started", SqliteType.Text);
        var duration = command.Parameters.Add("$duration", SqliteType.Real);
        var status = command.Parameters.Add("$status", SqliteType.Text);
        var toolResultStatus = command.Parameters.Add("$toolResultStatus", SqliteType.Text);
        var error = command.Parameters.Add("$error", SqliteType.Text);
        var request = command.Parameters.Add("$request", SqliteType.Integer);
        var response = command.Parameters.Add("$response", SqliteType.Integer);
        var detail = command.Parameters.Add("$detail", SqliteType.Text);

        foreach (var record in records)
        {
            id.Value = record.Id;
            tool.Value = record.ToolName;
            user.Value = record.UserName;
            started.Value = FormatTimestamp(record.StartedAt);
            duration.Value = record.DurationMs;
            status.Value = record.Status;
            toolResultStatus.Value = record.ToolResultStatus;
            error.Value = (object?)record.ErrorType ?? DBNull.Value;
            request.Value = (object?)record.RequestBytes ?? DBNull.Value;
            response.Value = (object?)record.ResponseBytes ?? DBNull.Value;
            detail.Value = (object?)record.ResultDetailJson ?? DBNull.Value;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<AnalyticsSummary> GetSummaryAsync(
        string databasePath,
        AnalyticsWindow window,
        CancellationToken cancellationToken)
    {
        await using var connection = await OpenReadAsync(databasePath, cancellationToken);
        if (connection is null)
        {
            return new AnalyticsSummary(
                window.From,
                window.To,
                0,
                new StatusBreakdown(0, 0, 0),
                new LatencySummary(0, 0, 0, 0));
        }

        var statusCounts = await ReadStatusCountsAsync(connection, tool: null, window, cancellationToken);
        var durations = await ReadDurationsAsync(connection, tool: null, window, cancellationToken);
        var total = statusCounts.Success + statusCounts.Error + statusCounts.Canceled;
        return new AnalyticsSummary(
            window.From,
            window.To,
            total,
            statusCounts,
            ComputeLatency(durations));
    }

    public async Task<IReadOnlyList<ToolUsage>> GetToolBreakdownAsync(
        string databasePath,
        AnalyticsWindow window,
        CancellationToken cancellationToken)
    {
        await using var connection = await OpenReadAsync(databasePath, cancellationToken);
        if (connection is null)
        {
            return [];
        }

        var counts = new Dictionary<string, long[]>(StringComparer.Ordinal);
        await using (var command = connection.CreateCommand())
        {
            command.CommandText =
                """
                SELECT tool_name, status, COUNT(*)
                FROM tool_invocations
                WHERE started_at >= $from AND started_at < $to
                GROUP BY tool_name, status;
                """;
            AddWindow(command, window);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var toolName = reader.GetString(0);
                var status = reader.GetString(1);
                var count = reader.GetInt64(2);
                if (!counts.TryGetValue(toolName, out var slot))
                {
                    slot = new long[3];
                    counts[toolName] = slot;
                }

                slot[StatusIndex(status)] += count;
            }
        }

        var durations = new Dictionary<string, List<double>>(StringComparer.Ordinal);
        await using (var command = connection.CreateCommand())
        {
            command.CommandText =
                """
                SELECT tool_name, duration_ms
                FROM tool_invocations
                WHERE started_at >= $from AND started_at < $to
                ORDER BY tool_name, duration_ms;
                """;
            AddWindow(command, window);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var toolName = reader.GetString(0);
                if (!durations.TryGetValue(toolName, out var list))
                {
                    list = [];
                    durations[toolName] = list;
                }

                list.Add(reader.GetDouble(1));
            }
        }

        var overall = counts.Values.Sum(slot => slot[0] + slot[1] + slot[2]);
        var usage = new List<ToolUsage>(counts.Count);
        foreach (var (toolName, slot) in counts)
        {
            var count = slot[0] + slot[1] + slot[2];
            var toolDurations = durations.TryGetValue(toolName, out var list)
                ? list.ToArray()
                : [];
            usage.Add(new ToolUsage(
                toolName,
                count,
                overall == 0 ? 0 : (double)count / overall,
                new StatusBreakdown(slot[0], slot[1], slot[2]),
                ComputeLatency(toolDurations)));
        }

        return usage
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.ToolName, StringComparer.Ordinal)
            .ToArray();
    }

    public async Task<IReadOnlyList<UserBreakdownItem>> GetUserBreakdownAsync(
        string databasePath,
        AnalyticsWindow window,
        CancellationToken cancellationToken)
    {
        await using var connection = await OpenReadAsync(databasePath, cancellationToken);
        if (connection is null)
        {
            return [];
        }

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT user_name, COUNT(*)
            FROM tool_invocations
            WHERE started_at >= $from AND started_at < $to
            GROUP BY user_name
            ORDER BY COUNT(*) DESC, user_name;
            """;
        AddWindow(command, window);

        var users = new List<UserBreakdownItem>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            users.Add(new UserBreakdownItem(reader.GetString(0), reader.GetInt64(1)));
        }

        return users;
    }

    public async Task<IReadOnlyList<ToolResultBreakdownItem>> GetToolResultBreakdownAsync(
        string databasePath,
        AnalyticsWindow window,
        CancellationToken cancellationToken)
    {
        await using var connection = await OpenReadAsync(databasePath, cancellationToken);
        if (connection is null)
        {
            return [];
        }

        var hasToolResultStatus = await HasColumnAsync(
            connection,
            "tool_invocations",
            "tool_result_status",
            cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            hasToolResultStatus
                ? """
            SELECT tool_result_status, COUNT(*)
            FROM tool_invocations
            WHERE started_at >= $from AND started_at < $to
            GROUP BY tool_result_status
            ORDER BY COUNT(*) DESC, tool_result_status;
            """
                : """
            SELECT status, COUNT(*)
            FROM tool_invocations
            WHERE started_at >= $from AND started_at < $to
            GROUP BY status
            ORDER BY COUNT(*) DESC, status;
            """;
        AddWindow(command, window);

        var results = new List<ToolResultBreakdownItem>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new ToolResultBreakdownItem(reader.GetString(0), reader.GetInt64(1)));
        }

        return results;
    }

    public async Task<AnalyticsTimeSeries> GetTimeSeriesAsync(
        string databasePath,
        AnalyticsWindow window,
        string bucket,
        string? tool,
        CancellationToken cancellationToken)
    {
        var bucketExpression = string.Equals(bucket, "day", StringComparison.OrdinalIgnoreCase)
            ? "strftime('%Y-%m-%dT00:00:00Z', started_at)"
            : "strftime('%Y-%m-%dT%H:00:00Z', started_at)";

        await using var connection = await OpenReadAsync(databasePath, cancellationToken);
        if (connection is null)
        {
            return new AnalyticsTimeSeries(bucket, tool, []);
        }

        await using var command = connection.CreateCommand();
        command.CommandText =
            $"""
            SELECT {bucketExpression} AS bucket_start, COUNT(*)
            FROM tool_invocations
            WHERE started_at >= $from AND started_at < $to
              AND ($tool IS NULL OR tool_name = $tool)
            GROUP BY bucket_start
            ORDER BY bucket_start;
            """;
        AddWindow(command, window);
        command.Parameters.AddWithValue("$tool", (object?)tool ?? DBNull.Value);

        var points = new List<TimeBucketPoint>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            points.Add(new TimeBucketPoint(ParseTimestamp(reader.GetString(0)), reader.GetInt64(1)));
        }

        return new AnalyticsTimeSeries(bucket, tool, points);
    }

    public async Task<IReadOnlyList<RecentCall>> GetRecentAsync(
        string databasePath,
        AnalyticsWindow window,
        int limit,
        CancellationToken cancellationToken)
    {
        await using var connection = await OpenReadAsync(databasePath, cancellationToken);
        if (connection is null)
        {
            return [];
        }

        var hasToolResultStatus = await HasColumnAsync(
            connection,
            "tool_invocations",
            "tool_result_status",
            cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            hasToolResultStatus
                ? """
            SELECT id, tool_name, user_name, started_at, duration_ms, status, tool_result_status
            FROM tool_invocations
            WHERE started_at >= $from AND started_at < $to
            ORDER BY started_at DESC
            LIMIT $limit;
            """
                : """
            SELECT id, tool_name, user_name, started_at, duration_ms, status, status
            FROM tool_invocations
            WHERE started_at >= $from AND started_at < $to
            ORDER BY started_at DESC
            LIMIT $limit;
            """;
        AddWindow(command, window);
        command.Parameters.AddWithValue("$limit", limit);

        var calls = new List<RecentCall>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            calls.Add(new RecentCall(
                Id: reader.GetString(0),
                ToolName: reader.GetString(1),
                UserName: reader.GetString(2),
                StartedAt: ParseTimestamp(reader.GetString(3)),
                DurationMs: reader.GetDouble(4),
                Status: reader.GetString(5),
                ToolResultStatus: reader.GetString(6),
                HasDetail: AnalyticsDetailJson.HasDetail(
                    reader.GetString(5),
                    reader.GetString(6))));
        }

        return calls;
    }

    public async Task<RecentCallDetail?> GetRecentDetailAsync(
        string databasePath,
        AnalyticsWindow window,
        string id,
        CancellationToken cancellationToken)
    {
        await using var connection = await OpenReadAsync(databasePath, cancellationToken);
        if (connection is null)
        {
            return null;
        }

        var hasToolResultStatus = await HasColumnAsync(
            connection,
            "tool_invocations",
            "tool_result_status",
            cancellationToken);
        var hasResultDetailJson = await HasColumnAsync(
            connection,
            "tool_invocations",
            "result_detail_json",
            cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            hasToolResultStatus
                ? hasResultDetailJson
                    ? """
                SELECT id, tool_name, user_name, started_at, duration_ms, status, tool_result_status,
                       error_type, result_detail_json
                FROM tool_invocations
                WHERE id = $id
                  AND started_at >= $from AND started_at < $to;
                """
                    : """
                SELECT id, tool_name, user_name, started_at, duration_ms, status, tool_result_status,
                       error_type, NULL
                FROM tool_invocations
                WHERE id = $id
                  AND started_at >= $from AND started_at < $to;
                """
                : """
            SELECT id, tool_name, user_name, started_at, duration_ms, status, status,
                   error_type, NULL
            FROM tool_invocations
            WHERE id = $id
              AND started_at >= $from AND started_at < $to;
            """;
        command.Parameters.AddWithValue("$id", id);
        AddWindow(command, window);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var transportStatus = reader.GetString(5);
        var toolResultStatus = reader.GetString(6);
        return new RecentCallDetail(
            Id: reader.GetString(0),
            ToolName: reader.GetString(1),
            UserName: reader.GetString(2),
            StartedAt: ParseTimestamp(reader.GetString(3)),
            DurationMs: reader.GetDouble(4),
            Status: transportStatus,
            ToolResultStatus: toolResultStatus,
            ErrorType: reader.IsDBNull(7) ? null : reader.GetString(7),
            Detail: AnalyticsDetailJson.Deserialize(
                reader.IsDBNull(8) ? null : reader.GetString(8)));
    }

    private static async Task<StatusBreakdown> ReadStatusCountsAsync(
        SqliteConnection connection,
        string? tool,
        AnalyticsWindow window,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT status, COUNT(*)
            FROM tool_invocations
            WHERE started_at >= $from AND started_at < $to
              AND ($tool IS NULL OR tool_name = $tool)
            GROUP BY status;
            """;
        AddWindow(command, window);
        command.Parameters.AddWithValue("$tool", (object?)tool ?? DBNull.Value);

        var slot = new long[3];
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            slot[StatusIndex(reader.GetString(0))] += reader.GetInt64(1);
        }

        return new StatusBreakdown(slot[0], slot[1], slot[2]);
    }

    private static async Task<double[]> ReadDurationsAsync(
        SqliteConnection connection,
        string? tool,
        AnalyticsWindow window,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT duration_ms
            FROM tool_invocations
            WHERE started_at >= $from AND started_at < $to
              AND ($tool IS NULL OR tool_name = $tool)
            ORDER BY duration_ms;
            """;
        AddWindow(command, window);
        command.Parameters.AddWithValue("$tool", (object?)tool ?? DBNull.Value);

        var values = new List<double>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            values.Add(reader.GetDouble(0));
        }

        return values.ToArray();
    }

    private static LatencySummary ComputeLatency(double[] sortedAscending)
    {
        if (sortedAscending.Length == 0)
        {
            return new LatencySummary(0, 0, 0, 0);
        }

        return new LatencySummary(
            Avg: sortedAscending.Average(),
            P50: Percentile(sortedAscending, 0.50),
            P95: Percentile(sortedAscending, 0.95),
            Max: sortedAscending[^1]);
    }

    private static double Percentile(double[] sortedAscending, double quantile)
    {
        if (sortedAscending.Length == 1)
        {
            return sortedAscending[0];
        }

        var rank = quantile * (sortedAscending.Length - 1);
        var low = (int)Math.Floor(rank);
        var high = (int)Math.Ceiling(rank);
        if (low == high)
        {
            return sortedAscending[low];
        }

        return sortedAscending[low] + ((sortedAscending[high] - sortedAscending[low]) * (rank - low));
    }

    private static int StatusIndex(string status) => status switch
    {
        AnalyticsStatus.Error => 1,
        AnalyticsStatus.Canceled => 2,
        _ => 0,
    };

    private static void AddWindow(SqliteCommand command, AnalyticsWindow window)
    {
        command.Parameters.AddWithValue("$from", FormatTimestamp(window.From));
        command.Parameters.AddWithValue("$to", FormatTimestamp(window.To));
    }

    private static async Task<SqliteConnection> OpenWriteAsync(
        string databasePath,
        CancellationToken cancellationToken)
    {
        var path = Path.GetFullPath(databasePath);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = false
        }.ToString());
        await connection.OpenAsync(cancellationToken);

        await ExecuteAsync(connection, "PRAGMA journal_mode=WAL;", cancellationToken);
        await ExecuteAsync(connection, AnalyticsSchema.CreateSql, cancellationToken);
        await MigrateAsync(connection, cancellationToken);
        await ExecuteAsync(
            connection,
            $"PRAGMA user_version = {AnalyticsSchema.Version};",
            cancellationToken);
        return connection;
    }

    private static async Task<SqliteConnection?> OpenReadAsync(
        string databasePath,
        CancellationToken cancellationToken)
    {
        var path = Path.GetFullPath(databasePath);
        if (!File.Exists(path))
        {
            return null;
        }

        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false
        }.ToString());
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static async Task ExecuteAsync(
        SqliteConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task MigrateAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        if (!await HasColumnAsync(connection, "tool_invocations", "tool_result_status", cancellationToken))
        {
            await ExecuteAsync(
                connection,
                "ALTER TABLE tool_invocations ADD COLUMN tool_result_status TEXT NOT NULL DEFAULT 'ok';",
                cancellationToken);
        }

        if (!await HasColumnAsync(connection, "tool_invocations", "result_detail_json", cancellationToken))
        {
            await ExecuteAsync(
                connection,
                "ALTER TABLE tool_invocations ADD COLUMN result_detail_json TEXT NULL;",
                cancellationToken);
        }
    }

    private static async Task<bool> HasColumnAsync(
        SqliteConnection connection,
        string tableName,
        string columnName,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({tableName});";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string FormatTimestamp(DateTimeOffset value) =>
        value.ToUniversalTime().ToString(TimestampFormat, CultureInfo.InvariantCulture);

    private static DateTimeOffset ParseTimestamp(string value) =>
        DateTimeOffset.Parse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
}
