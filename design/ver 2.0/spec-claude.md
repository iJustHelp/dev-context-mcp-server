# MCP Server Analytics Specification (v2.0)

## Summary

The .NET host records one metadata-only event per MCP tool invocation at the
existing `ToolInvocationLogger.InvokeAsync` chokepoint, persists events in a
self-creating `analytics.db` SQLite database isolated from `docs.db`, and serves
read-only `/api/analytics/{summary,timeseries,tools,recent}` endpoints. Capture
is non-blocking and behavior-neutral: it never changes tool results, status, or
latency. A separate local Next.js dashboard is the only consumer of the API.
Configuration lives under `DevContextMcp:Analytics`. No request or response
content is ever stored, and no authentication is added.

## 1. Purpose

Add usage analytics to the MCP documentation server. The .NET host captures one
metadata-only event per MCP tool invocation, persists events in a dedicated
SQLite database, and exposes read-only REST endpoints. A separate local Next.js
dashboard consumes those endpoints; this specification governs the server
changes that own capture, storage, and the analytics API.

## 2. Goals

- Record every MCP tool invocation at a single, behavior-neutral chokepoint.
- Persist events in a self-creating analytics database, isolated from `docs.db`.
- Audit a caller identity per request from a configurable HTTP header.
- Serve aggregate and recent-call analytics over `/api/analytics/*`.
- Keep capture non-blocking so tool latency and behavior are unchanged.

## 3. Non-Goals

- Store request or response content. Only metadata is recorded.
- Authenticate or authorize callers. The model stays unauthenticated loopback.
- Modify any existing MCP tool or resource contract or behavior.
- Aggregate, persist, or render analytics inside the dashboard process; the
  Next.js app only proxies and visualizes.
- Add analytics tables to `docs.db`, which is read-only at runtime and rebuilt
  by the indexer.
- Enforce data retention or rollup in this release. See Section 13.

## 4. Captured Event

Every tool invocation that passes through `ToolInvocationLogger.InvokeAsync`
produces exactly one event with metadata only:

- Invocation ID (the existing per-call GUID).
- Tool name.
- User name resolved from the configured header (Section 4.1).
- Start timestamp (UTC, ISO-8601) and duration in milliseconds, taken from the
  `Stopwatch` the method already uses.
- Status: `success`, `error`, or `canceled`, matching the three outcomes the
  method already distinguishes (return, `OperationCanceledException`, fault).
- Error type name (CLR exception type) when the status is `error`; otherwise
  null.
- Request and response payload sizes in UTF-8 bytes (Section 4.2).

No request or response body, argument values, or evidence content is stored.

### 4.1 User resolution

The user name is read from the configured request header (default
`X-User-Name`), via the already-registered `IHttpContextAccessor`. When the
header is absent or empty, resolution falls back in order to the authenticated
identity (`HttpContext.User.Identity.Name`), then the remote IP address, then
the literal `anonymous`. This is a trust-the-header model; no header is validated
against an identity provider. The resolved value is recorded verbatim and never
used for any authorization decision.

### 4.2 Payload sizes

Byte sizes are best-effort and recorded only when they can be obtained without
adding serialization to the tool path. When the host already serializes a
payload for diagnostics, that size is reused; otherwise the field is recorded as
null rather than serializing solely for measurement. Sizes never cause content
to be retained.

## 5. Capture Pipeline

- Capture occurs in `ToolInvocationLogger.InvokeAsync`, reusing the timing and
  outcome the method already computes. No new tool wrapping is introduced.
- A non-blocking recorder enqueues each event onto a bounded in-memory channel.
  Enqueue never blocks the tool call and never throws into the tool path; when
  the channel is full the event is dropped (drop-oldest), and a drop counter is
  incremented for diagnostics.
- A hosted background service drains the channel and writes events to the
  analytics store in batches.
- Capture and persistence failures are swallowed, consistent with existing
  diagnostic logging: analytics must never change tool results, status, or
  latency.
- When analytics is disabled, the chokepoint behaves exactly as today and no
  recorder, hosted service, or endpoint is registered.

## 6. Persistence

- A separate SQLite database, default path
  `../../../../../database/analytics.db`, owned and written only by the host and
  resolved relative to the host base directory.
- The store is self-creating: on first use it creates the file, schema, and
  indexes in read-write mode with WAL journaling so reads for the API do not
  block the background writer. It is never created or touched by the indexer.
- Schema versioning mirrors the index store: `AnalyticsSchema.Version` (initial
  value `1`) is stamped in `PRAGMA user_version` and validated on open; a newer
  required version than the file fails fast.
- One `tool_invocations` table holds the captured event:

```sql
CREATE TABLE tool_invocations (
    id             TEXT    PRIMARY KEY,   -- invocation GUID
    tool_name      TEXT    NOT NULL,
    user_name      TEXT    NOT NULL,
    started_at     TEXT    NOT NULL,      -- ISO-8601 UTC
    duration_ms    REAL    NOT NULL,
    status         TEXT    NOT NULL,      -- success | error | canceled
    error_type     TEXT    NULL,
    request_bytes  INTEGER NULL,
    response_bytes INTEGER NULL
);

CREATE INDEX ix_ti_started ON tool_invocations(started_at);
CREATE INDEX ix_ti_tool    ON tool_invocations(tool_name, started_at);
CREATE INDEX ix_ti_user    ON tool_invocations(user_name, started_at);
```

- Aggregations are computed in SQL (counts, averages, group-by tool and user,
  `strftime` time buckets). Percentile latency (p50, p95) is computed in the
  host from per-tool durations, since SQLite has no percentile function.

## 7. Analytics API

The host serves read-only JSON endpoints under `/api/analytics`, registered
after the MCP endpoint and only when analytics is enabled. All endpoints accept
optional `from` and `to` ISO-8601 window parameters; when omitted, the window
defaults to the last 24 hours. Out-of-range or unparparseable parameters return
`400`.

| Endpoint | Parameters | Returns |
|---|---|---|
| `GET /api/analytics/summary` | `from`, `to` | totals, status/error distribution, latency summary |
| `GET /api/analytics/timeseries` | `from`, `to`, `bucket=hour\|day` (default `hour`), `tool` | call counts per time bucket |
| `GET /api/analytics/tools` | `from`, `to` | per-tool breakdown with latency |
| `GET /api/analytics/recent` | `limit` (default `50`, max `500`) | most recent calls |

Response shapes (stable contract for the dashboard):

```jsonc
// GET /api/analytics/summary
{
  "from": "2026-06-18T00:00:00Z", "to": "2026-06-19T00:00:00Z",
  "totalCalls": 1280,
  "statusCounts": { "success": 1241, "error": 31, "canceled": 8 },
  "latencyMs": { "avg": 42.7, "p50": 18, "p95": 160, "max": 980 }
}

// GET /api/analytics/timeseries
{ "bucket": "hour", "tool": null,
  "points": [ { "bucketStart": "2026-06-18T13:00:00Z", "count": 57 } ] }

// GET /api/analytics/tools
{ "tools": [ {
    "toolName": "query_docs", "count": 612, "share": 0.478,
    "statusCounts": { "success": 600, "error": 9, "canceled": 3 },
    "latencyMs": { "avg": 71.2, "p50": 40, "p95": 220, "max": 980 } } ] }

// GET /api/analytics/recent
{ "calls": [ {
    "id": "9f2c…", "toolName": "ping", "userName": "alice",
    "startedAt": "2026-06-19T09:12:04Z", "durationMs": 3.1,
    "status": "success" } ] }
```

The browser never calls these endpoints directly; the dashboard proxies them
server-to-server, so cross-origin handling is unnecessary.

## 8. Configuration

Add an `Analytics` section under `DevContextMcp`:

```json
{
  "DevContextMcp": {
    "Analytics": {
      "Enabled": true,
      "DatabasePath": "../../../../../database/analytics.db",
      "UserHeaderName": "X-User-Name"
    }
  }
}
```

- `Enabled` toggles capture and the analytics endpoints.
- `DatabasePath` resolves relative to the host base directory, like
  `DatabasePath` for the index.
- `UserHeaderName` selects the audited header.

Options are validated on startup alongside existing host options: when
`Enabled` is true, `DatabasePath` and `UserHeaderName` must be non-empty.

## 9. Architecture

Follow the established layering and the dependency rules enforced by
`ProjectDependencyTests`:

- `Server.Core`: analytics event and aggregate models, plus the write and read
  store abstractions. No project references.
- `Infrastructure`: the self-creating SQLite analytics store implementing those
  abstractions, mirroring `SqliteNuGetReadStore`.
- `Server`: the non-blocking recorder and its background drain, the user
  resolver, options binding, dependency registration, and the
  `/api/analytics/*` endpoints.

No new project references are added. The indexer remains unaware of analytics.

## 10. Dashboard Contract (downstream)

The analytics consumer is a local Next.js TypeScript app in
`analytics-dashboard/`, outside this server specification but constrained by it:

- App Router with server-side route handlers under `/api/analytics/*` that proxy
  to the host using `MCP_ANALYTICS_API_BASE_URL`, default
  `http://127.0.0.1:2222`.
- Recharts renders the first screen: total calls, calls over time, per-tool
  breakdown, latency overview, status/error distribution, and a recent-calls
  table.

The server must keep the Section 7 endpoint shapes stable for this consumer.

## 11. Security And Limits

- Treat the audited header as untrusted, free-text metadata; never use it for
  authorization.
- Store metadata only; never persist request or response content.
- Keep the analytics database separate from `docs.db` and never expose raw
  rows beyond the defined endpoints.
- Bound `recent` results (default `50`, max `500`) and always apply a query
  window.
- Capture and persistence failures must never surface to tool callers.

## 12. Tests And Acceptance

- Unit: analytics option validation, event capture mapping, status mapping,
  user-resolution precedence, and SQLite aggregate and percentile queries
  against a temporary database.
- Integration: the `/api/analytics/*` endpoints against a temporary analytics
  database with seeded events, including window and parameter validation.
- Regression: existing MCP tool and resource behavior is unchanged with
  analytics enabled and disabled.
- Frontend: a Next.js production build check (`npm run build`).

Acceptance:

1. A tool call produces exactly one metadata-only event with correct tool,
   status, duration, user, and byte sizes.
2. An errored call is recorded as `error` with its error type; a canceled call
   is recorded as `canceled`.
3. A missing user header falls back to the documented order.
4. The analytics database is created on first run without affecting `docs.db`.
5. `summary`, `timeseries`, `tools`, and `recent` return correct aggregates and
   stable shapes for seeded events.
6. Disabling analytics removes capture and endpoints with no behavior change.
7. `dotnet build`, `dotnet test`, and the dashboard build all pass.

## 13. Future Work

- Retention and rollup: events accumulate unbounded today. A later release may
  add age-based pruning or hourly/daily rollup tables and a retention option.
- Real identity: replace the trust-the-header model with authenticated identity
  when the server gains authentication.
- Additional dimensions: environment and source labels per call, once available
  at the chokepoint.
