# Indexed Context UI Plan

## Summary
Add app navigation with two pages: `Dashboard` for the existing analytics view and `Context` for a read-only inventory of what is currently indexed in `docs.db`. Default scope is inventory-only: no full document content, no symbol/document preview.

## Key Changes
- Move the current dashboard from `ui/app/page.tsx` into the dashboard route, keeping `/` as the dashboard entry.
- Add shared navigation in `ui/app/layout.tsx` with links for:
  - `Dashboard` -> `/`
  - `Context` -> `/context`
- Add `GET /api/context` on the .NET server.
  - Returns totals, sources, environments, libraries, latest versions, counts, and last indexed timestamps.
  - Reads from the existing docs index read-only through `INuGetReadStore`.
  - Returns a clear API error if the index is missing or schema is too old.
- Add a Next.js proxy route for `/api/context`, matching the current analytics proxy pattern.
- Add TypeScript response types and UI components for:
  - Document table: doc name, last indexed timestamp, document lenght.
  - NuGet table: package/display name, environment, all versios, last indexed.
- The Context page  tables must include a text search filter and sortable columns.

## Public Interfaces
- New server endpoint: `GET /api/context`
- New read-store method: `GetIndexedContextAsync(databasePath, cancellationToken)`
- New response contract:
  - `generatedAt`
  - `totals`
  - `documents[]`
  - `nugets[]`
- Regenerate or update `ui/openapi.json` and `ui/lib/generated/schema.d.ts` so the UI client remains typed.

## Test Plan
- Unit test the SQLite read-store query against a temporary index database with NuGet and docs entries.
- Integration test `GET /api/context` for success and missing-index error behavior.
- UI build check with `npm run build`.
- Verify navigation renders both pages and the existing dashboard still loads analytics data.
- Verify the Context page table can be filtered by search text and sorted by each visible column.

## Assumptions
- “Current context” means indexed inventory, not full content browsing.
- The first version should not expose document bodies or symbol documentation in the UI.
- The page can show a capped library table if the index grows large, with totals still reflecting the full index.
