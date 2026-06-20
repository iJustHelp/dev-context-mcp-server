# Stage 2 Implementation Plan: Analytics API

## Objective

Expose the events captured in Stage 1 through read-only, window-scoped HTTP
endpoints with stable JSON shapes, hosted in the existing server process.

Implements spec §6 (read aggregations), §7, §11.

## Dependencies

- Stage 1 complete: `analytics.db` is populated by the recorder.

## Scope

- Aggregate and recent-call read queries over `analytics.db`.
- `/api/analytics/{summary,timeseries,tools,recent}` endpoints.
- Query-window and parameter validation.

Out of scope: dashboard UI, authentication, retention.

## Components

### Read models and abstraction (`Server.Core`)

- `OverviewStats`, `ToolUsageSummary`, `TimeBucketPoint`, `RecentCall` matching
  the spec §7 response shapes.
- `IToolInvocationReadStore`: `GetSummaryAsync`, `GetToolsAsync`,
  `GetTimeSeriesAsync(bucket, tool?, window)`, `GetRecentAsync(limit, window)`,
  all scoped by a `from`/`to` window.

### SQLite read queries (`Infrastructure`)

- Extend `SqliteAnalyticsStore` to implement `IToolInvocationReadStore` (open in
  read-only mode).
- Counts, status distribution, averages, and `strftime` hour/day buckets in SQL;
  p50/p95 computed in the host from per-tool durations.

### Endpoints (`Server`)

- `AnalyticsEndpoints.MapAnalyticsEndpoints(WebApplication)` invoked from
  `Program.cs` after `MapMcp`, registered only when analytics is enabled.
- Routes, parameters, defaults, and bounds per spec §7:
  - `summary` — `from`, `to`.
  - `timeseries` — `from`, `to`, `bucket=hour|day` (default `hour`), `tool`.
  - `tools` — `from`, `to`.
  - `recent` — `limit` (default 50, max 500).
- `from`/`to` default to the last 24 hours; unparseable or out-of-range params
  return `400`. No CORS (the dashboard proxies server-to-server).

## Implementation Sequence

1. Add read models and `IToolInvocationReadStore` in `Server.Core`.
2. Implement the read queries and percentile computation in `Infrastructure`.
3. Add the endpoint group and parameter parsing/validation in `Server`.
4. Map endpoints from `Program.cs`, gated on `Enabled`.
5. Add unit and integration tests.

## Tests

- Aggregate correctness for seeded events: totals, status distribution, per-tool
  counts and latency, hour/day buckets, tool-filtered timeseries.
- Percentile computation (p50, p95) correctness.
- Parameter handling: default window, explicit window, `bucket` values, `recent`
  limit clamping, and `400` on bad input.
- Integration: each endpoint against a temporary analytics database, asserting
  stable JSON shapes.
- Endpoints absent when analytics is disabled.

## Completion Criteria

- All four endpoints return correct, window-scoped aggregates with stable shapes.
- Bad parameters are rejected with `400`; defaults apply when omitted.
- Reads do not block the Stage 1 writer (WAL).
- `dotnet build` and `dotnet test` pass.
