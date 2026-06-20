# MCP Analytics Dashboard

A local [Next.js](https://nextjs.org/) (App Router, TypeScript) dashboard that
visualizes MCP server tool-call analytics. The dashboard's server-side route
handlers proxy to the .NET MCP server's `/api/analytics/*` endpoints, so the
browser only ever talks to this app — never directly to the .NET host.

Implements Stage 3 of `design/ver 2.0`.

## What it shows

- **KPIs** — total calls, success rate, average and p95 latency.
- **Calls over time** — hourly/daily area chart.
- **Status distribution** — success / error / canceled.
- **Per-tool breakdown** — calls, share, success/error, average and p95 latency.
- **Latency** — average, p50, p95, and max.
- **Recent calls** — newest invocations with tool, user, duration, and status.

A date-range and bucket control at the top drives every panel.

## Prerequisites

- Node.js 18.18+ (developed against Node 20+).
- The .NET MCP server running with analytics enabled (default
  `http://127.0.0.1:2222`). See `src/DevContextMcp.Server`.

## Configuration

The proxy target is read from `MCP_ANALYTICS_API_BASE_URL` (default
`http://127.0.0.1:2222`). To override it, copy `.env.example` to `.env.local`:

```bash
cp .env.example .env.local
# edit MCP_ANALYTICS_API_BASE_URL if your server is elsewhere
```

## Run

```bash
npm install
npm run dev      # http://localhost:3000
```

Generate some traffic against the MCP server (sending an `X-User-Name` header to
populate the user audit), then refresh the dashboard.

## Build

```bash
npm run build
npm run start
```

## Typed API client (generated)

The dashboard talks to the analytics API through a **generated** client, so the
TypeScript types are always in sync with the server contract:

- `openapi.json` — the OpenAPI document, exported from the .NET server.
- `lib/generated/schema.d.ts` — types generated from it by
  [`openapi-typescript`](https://openapi-ts.dev/) (do not edit by hand).
- `lib/client.ts` — a tiny [`openapi-fetch`](https://openapi-ts.dev/openapi-fetch/)
  client (`apiClient`) used by `app/page.tsx`. Its `baseUrl` is relative, so calls
  still go through the proxy described below.

Regenerate after the server's analytics contract changes:

```bash
# 1. Refresh openapi.json from the running server (analytics enabled):
curl http://127.0.0.1:2222/openapi/v1.json -o openapi.json
# 2. Regenerate the TypeScript types:
npm run gen:api
```

`npm run gen:api` reads the committed `openapi.json`. To generate straight from a
running server instead, point it at the live document:
`openapi-typescript http://127.0.0.1:2222/openapi/v1.json -o ./lib/generated/schema.d.ts`.

## How it fits together

```
browser ──► /api/analytics/* (Next.js route handler)
                     │  server-to-server fetch
                     ▼
        MCP server  /api/analytics/{summary,timeseries,tools,recent}
                     │
                     ▼
                 analytics.db (SQLite)
```

No cross-origin configuration is required because the browser calls only the
dashboard's own origin; the proxy reaches the .NET host server-to-server.
