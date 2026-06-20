# Stage 3 Implementation Plan: Next.js Dashboard

## Objective

Provide a local Next.js dashboard that visualizes the analytics API: total
calls, calls over time, per-tool breakdown, latency overview, status/error
distribution, and recent calls.

Implements spec §10.

## Dependencies

- Stage 2 complete: `/api/analytics/*` endpoints serve stable shapes.

## Scope

- A new `analytics-dashboard/` Next.js TypeScript app (App Router).
- Server-side route handlers that proxy to the .NET analytics API.
- Recharts-based first screen.

Out of scope: any change to the .NET server contract or authentication.

## Components

### Project

- `analytics-dashboard/` Next.js + TypeScript, App Router.
- `MCP_ANALYTICS_API_BASE_URL` env var, default `http://127.0.0.1:2222`.
- Scripts: `dev`, `build`, `start`. A `README.md` with run instructions.

### Proxy route handlers

- Server-side handlers under `app/api/analytics/*` that forward
  `summary`/`timeseries`/`tools`/`recent` (and their query params) to the host
  via `fetch`, server-to-server. The browser never calls the host directly.

### Dashboard screen

- Overview KPIs: total calls, success rate, average and p95 latency (`summary`).
- Calls-over-time chart with hour/day toggle and optional tool filter
  (`timeseries`).
- Per-tool table: count, share, success/error, average and p95 latency (`tools`).
- Status/error distribution (`summary`).
- Recent-calls table: tool, user, time, duration, status (`recent`).
- A shared date-range control feeding `from`/`to` to all queries.

## Implementation Sequence

1. Scaffold the Next.js TypeScript app and config (`MCP_ANALYTICS_API_BASE_URL`).
2. Add the proxy route handlers for the four endpoints.
3. Add a typed fetch layer matching the spec §7 shapes.
4. Build KPI, charts, distribution, and recent-calls components with Recharts.
5. Add the date-range control and wire it through all queries.
6. Add a `README.md`.

## Tests

- `npm run build` (production build check) passes.
- Proxy handlers forward query params and base URL correctly.
- Components render against representative API fixtures.

## Completion Criteria

- The dashboard renders all six views from live endpoints and updates as new
  calls arrive.
- The app reads the API only through its own server-side proxy.
- `npm run build` succeeds.
