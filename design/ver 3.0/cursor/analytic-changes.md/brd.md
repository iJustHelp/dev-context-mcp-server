# Analytics Not-Ok Detail BRD

## Purpose

Give operators and developers enough context to diagnose failed or partial MCP
tool outcomes from the analytics dashboard without enabling debug logging or
persisting request or response payloads.

## Business Outcome

When a recent call has a non-ok tool result or transport failure, the dashboard
user can click the row and see why it failed: error codes and messages,
exception type, and resolved library or version context.

## Dependencies

- Analytics capture and dashboard from ver 2.0 (tool invocations table, recent
  calls UI, `/api/analytics/*` endpoints).

## Definitions

| Term | Meaning |
|------|---------|
| **Not ok** | `toolResultStatus != "ok"` or transport `status != "success"` |
| **Detail** | Metadata-only summary: `errors[]`, optional `resolvedContext`, optional `errorType` |

## In Scope

- Persist bounded detail JSON for not-ok invocations in the analytics database.
- Schema migration from analytics schema v2 to v3.
- Detail read endpoint `GET /api/analytics/recent/{id}`.
- `hasDetail` flag on recent-call list items.
- Dashboard click-to-detail UX for non-ok rows in Recent calls.

## Functional Requirements

### FR-1: Capture detail on write

When analytics capture records a not-ok invocation, persist a bounded JSON detail
payload derived from the tool response envelope:

- `errors`: `{ code, message }[]` from `ToolResponse.Errors` when present
- `resolvedContext`: `{ libraryId, sourceId, environment, version, versionSelectionReason }` when present
- `errorType`: existing exception type name for transport failures

Do not persist request bodies, response `data`, fragments, symbols, evidence, or
citations.

### FR-2: Schema migration

- Bump `AnalyticsSchema.Version` from `2` to `3`
- Add nullable column `result_detail_json TEXT` to `tool_invocations`
- Migrate existing databases with `ALTER TABLE ... ADD COLUMN`

### FR-3: Detail read API

Add `GET /api/analytics/recent/{id}`:

- Returns `404` when the id is not found within the optional `from`/`to` window
- Returns a `RecentCallDetail` model with base call fields plus parsed detail
- Uses the same window validation as other analytics endpoints

### FR-4: Recent list affordance

Extend `RecentCall` with `hasDetail: bool` so the UI can style clickable rows
without fetching every detail up front.

### FR-5: Dashboard detail UI

In the Recent calls table:

- Rows with `hasDetail === true` are clickable
- Click opens a detail panel showing tool result status, transport status, error
  type, error list, and resolved context fields
- Detail is fetched lazily via a dashboard proxy route

### FR-6: Size and safety limits

- Cap serialized `result_detail_json` at 4 KB
- Truncate excess errors rather than failing capture
- Capture failures must never affect tool behavior

## Non-Functional Requirements

- Metadata-only storage, consistent with ver 2.0 analytics spec section 11
- Backward compatible: existing rows have `result_detail_json = NULL` and
  `hasDetail = false`
- OpenAPI and generated TypeScript types stay in sync with the server contract
- Nullable reference types and existing test conventions apply

## Deliverables

- Analytics schema v3 with `result_detail_json` column
- Capture logic in `ToolInvocationLogger`
- `GET /api/analytics/recent/{id}` endpoint
- Dashboard detail panel and proxy route
- Unit tests and regenerated OpenAPI or TS types

## Acceptance Criteria

1. A `not_found` or `insufficient_evidence` tool response persists at least one
   `{ code, message }` error in `result_detail_json`.
2. A thrown exception persists `errorType` and detail is retrievable via the new
   endpoint.
3. Recent list marks non-ok rows with `hasDetail: true`.
4. Clicking a non-ok row in the dashboard shows the stored detail; ok rows are
   not clickable.
5. `dotnet test` and `npm run build` pass; existing analytics aggregations are
   unchanged.

## Out of Scope

- Storing full tool request or response payloads
- Showing detail for `ok` rows, including rows that only have warnings
- Changing aggregate KPIs, charts, or tool-result breakdown logic
- Authentication or authorization on analytics endpoints

## Exit Gate

The feature is complete when all acceptance criteria pass and the dashboard
shows actionable detail for newly captured non-ok calls.
