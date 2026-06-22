namespace DevContextMcp.Infrastructure.Analytics;

/// <summary>
/// Schema contract for the analytics database. The store stamps this value into
/// PRAGMA user_version on creation. Unlike the documentation index, the analytics
/// database is self-creating and owned solely by the host. Bump this constant
/// whenever the analytics schema changes.
/// </summary>
internal static class AnalyticsSchema
{
    internal const int Version = 2;

    internal const string CreateSql =
        """
        CREATE TABLE IF NOT EXISTS tool_invocations (
            id             TEXT    PRIMARY KEY,
            tool_name      TEXT    NOT NULL,
            user_name      TEXT    NOT NULL,
            started_at     TEXT    NOT NULL,
            duration_ms    REAL    NOT NULL,
            status         TEXT    NOT NULL,
            tool_result_status TEXT NOT NULL DEFAULT 'ok',
            error_type     TEXT    NULL,
            request_bytes  INTEGER NULL,
            response_bytes INTEGER NULL
        );

        CREATE INDEX IF NOT EXISTS ix_ti_started ON tool_invocations(started_at);
        CREATE INDEX IF NOT EXISTS ix_ti_tool    ON tool_invocations(tool_name, started_at);
        CREATE INDEX IF NOT EXISTS ix_ti_user    ON tool_invocations(user_name, started_at);
        """;
}
