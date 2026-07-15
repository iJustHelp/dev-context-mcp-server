---
type: Reference
title: HTTP API (non-MCP)
description: Read-only REST endpoints for indexed-context inventory and tool-usage analytics, served alongside the MCP endpoint.
resource: src/DevContextMcp.Server/api/AnalyticsEndpoints.cs
tags: [http, api, rest, analytics, context]
timestamp: 2026-07-15T00:00:00Z
---

# HTTP API (non-MCP)

Beyond the MCP endpoint, [DevContextMcp.Server](/projects/server.md) exposes a
small read-only REST API used by the [dashboard UI](/dashboard-ui.md): a
**Context** group describing the current index, and an **Analytics** group over
the [analytics database](/analytics.md). All analytics endpoints are read-only,
default their time window to the last 24 hours, and return `400` with an
`ApiError` body on invalid parameters.

# Schema

## Context

| Method & path | Returns | Notes |
|---------------|---------|-------|
| `GET /api/context` | `IndexedContextResponse` | Inventory of the current documentation index (via `INuGetReadStore`). Returns `503 ApiError` when the index does not exist (`IndexUnavailableException`). |
| `GET /api/context/last-run` | `IndexSnapshot` | Last indexing run snapshot. Returns an empty `"none"` snapshot when absent. |

## Analytics — `/api/analytics`

Common query parameters `from` and `to` are ISO-8601 timestamps (parsed as
universal time). When omitted, the window is the last 24 hours; `from` must be
earlier than `to`, otherwise `400`.

| Method & path | Extra params | Returns |
|---------------|--------------|---------|
| `GET /summary` | — | `AnalyticsSummary` |
| `GET /timeseries` | `bucket` = `hour` (default) or `day`; optional `tool` | `AnalyticsTimeSeries` |
| `GET /tools` | — | `ToolBreakdownResponse` |
| `GET /users` | — | `UserBreakdownResponse` |
| `GET /tool-results` | — | `ToolResultBreakdownResponse` |
| `GET /recent` | `limit` (default 50, clamped 1–500) | `RecentCallsResponse` |
| `GET /recent/{id}` | — | `RecentCallDetail`, or `404` when not found |

The analytics database path is resolved from `Analytics.DatabasePath`
(see [Server Configuration](/server-configuration.md)).

# Examples

```text
GET /api/context
GET /api/context/last-run
GET /api/analytics/summary?from=2026-07-14T00:00:00Z&to=2026-07-15T00:00:00Z
GET /api/analytics/timeseries?bucket=day&tool=query_docs
GET /api/analytics/recent?limit=100
GET /api/analytics/recent/{id}
```

See [Analytics](/analytics.md) for how these values are captured and stored, and
[Dashboard UI](/dashboard-ui.md) for the client that consumes them.

# Citations

[1] [ContextEndpoints.cs](../src/DevContextMcp.Server/api/ContextEndpoints.cs)
[2] [AnalyticsEndpoints.cs](../src/DevContextMcp.Server/api/AnalyticsEndpoints.cs)
