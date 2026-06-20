# Create app to show MCP server ananalytics data.

## Claude

Backend (C#) — capture analytics at the single chokepoint where every tool call already passes (ToolInvocationLogger.InvokeAsync), persist to a self-creating separate analytics.db, and serve /api/analytics/* endpoints from the existing server process. It follows the project's existing layering (abstractions in Server.Core, SQLite impl in Infrastructure, mirroring INuGetReadStore / SqliteNuGetReadStore). Capture is buffered and fire-and-forget so it can never affect tool latency. User name is audited per request via a configurable header (X-User-Name) with fallbacks, since the server has no auth today.

Frontend — analytics-dashboard/ with an Express BFF (TypeScript) that proxies/aggregates the C# API server-to-server, and a Vite + React + TS client using Recharts for per-tool breakdown, calls-over-time, latency, and the user-audit table.

One thing worth flagging: the per-request user audit depends on clients sending an identifying header — there's no authentication in the server today, so it's a trust-the-header model. If you later want real identity, that'd be a separate auth piece. The plan documents this assumption.

## Codex

# Analytics Dashboard With Next.js

## Summary
Build a local Next.js analytics dashboard for MCP server calls. The .NET MCP server owns analytics capture, stores metadata-only events in a separate SQLite database, and exposes REST endpoints. The Next.js app runs on Node.js, renders the dashboard, and proxies analytics API calls to the .NET server.

## Key Changes
- Add `DevContextMcp:Analytics` config:
  - `Enabled: true`
  - `DatabasePath: "../../../../../database/analytics.db"`
  - `UserHeaderName: "X-User-Name"`
- Capture analytics in `ToolInvocationLogger.InvokeAsync` using a non-blocking recorder:
  - tool name, invocation ID, timestamps, duration, status, user header, request/response byte sizes
  - no request or response content stored.
- Add a self-creating SQLite analytics store and `/api/analytics/*` endpoints on the .NET host:
  - `summary`
  - `timeseries`
  - `tools`
  - `recent`
- Add `analytics-dashboard/` as a Next.js TypeScript app:
  - App Router
  - server-side route handlers under `/api/analytics/*` that proxy to the .NET analytics API
  - `MCP_ANALYTICS_API_BASE_URL`, default `http://127.0.0.1:2222`
  - Recharts for charts.
- Dashboard first screen:
  - total calls
  - calls over time
  - per-tool breakdown
  - latency overview
  - status/error distribution
  - recent calls table.

## Tests
- Unit-test analytics option validation, event capture, and SQLite aggregate queries.
- Integration-test .NET analytics endpoints with a temp analytics DB.
- Add a Next.js build check with `npm run build`.
- Keep existing MCP tool/resource behavior unchanged.

## Assumptions
- Next.js replaces the earlier Vite React plus Express idea.
- .NET remains the analytics database owner because it has the direct MCP invocation chokepoint.
- The dashboard is local-development oriented and uses the current unauthenticated loopback model.
