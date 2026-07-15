---
type: TypeScript Project
title: ui (Analytics Dashboard)
description: The local Next.js dashboard that visualizes tool-call analytics and the indexed-context inventory.
resource: ui/package.json
tags: [project, ui, nextjs, typescript, web]
timestamp: 2026-07-15T00:00:00Z
---

# ui (Analytics Dashboard)

**Role:** dashboard &nbsp;·&nbsp; **Type:** Next.js web app (not part of `DevContextMcp.slnx`) &nbsp;·&nbsp; **Location:** `ui/`

A local Next.js (App Router, TypeScript) dashboard that visualizes MCP analytics
and the current index inventory. Its server-side route handlers proxy to the
.NET host's [HTTP API](/http-api.md), so the browser only talks to its own
origin. Full design in [Dashboard UI](/dashboard-ui.md).

# Schema

| Aspect | Value |
|--------|-------|
| Framework | Next.js `14.2.35` (App Router), React `18.3.1`, TypeScript `5.5.4` |
| Key deps | `openapi-fetch` (typed client), `recharts` (charts) |
| Proxy target | `MCP_ANALYTICS_API_BASE_URL` (default `http://127.0.0.1:2222`) |
| Pages | `app/page.tsx` (analytics), `app/context/page.tsx` (indexed context) |
| Generated client | `lib/generated/schema.d.ts` from `ui/openapi.json` via `npm run gen:api` (do not hand-edit) |

Scripts: `dev`, `build`, `start`, `lint`, `gen:api`.

# Examples

```bash
cd ui
npm install
npm run dev      # http://localhost:3000 (requires the .NET server on :2222 with analytics enabled)
```

See [Dashboard UI](/dashboard-ui.md) for pages, proxy routes, and data flow, and
[HTTP API](/http-api.md) for the endpoints it consumes.

# Citations

[1] [ui/package.json](../../ui/package.json)
[2] [ui/README.md](../../ui/README.md)
