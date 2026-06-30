# Indexing Snapshot BRD: Last-Run Package Status

## Purpose

Record a per-package snapshot of the most recent indexing run and surface it on the
UI context page, so operators can see — for every configured package — how many
versions the feed offered, which versions were indexed, the package's status, and
whether it failed.

## Business Outcome

After an indexing run, a user can open the context page and immediately tell whether
each configured NuGet package indexed as expected, including packages that produced
errors or indexed fewer versions than expected, without reading indexer logs.

## Current Behavior

- The indexer records a per-source run summary in the **documentation index** tables
  `index_runs` and `index_run_errors`
  ([SqliteIndexStore.Schema.cs](../../../../src/DevContextMcp.Infrastructure/Indexer/Persistence/SqliteIndexStore.Schema.cs)).
  These are aggregate counts plus errors; there is no per-package row, and the count
  of versions *available on the feed* is discarded during selection
  ([NuGetPackageSourceClient.cs](../../../../src/DevContextMcp.Infrastructure/Indexer/NuGet/NuGetPackageSourceClient.cs),
  [IndexCoordinator.cs](../../../../src/DevContextMcp.Indexer.Core/Services/IndexCoordinator.cs)).
- The **analytics database** is owned by the server host and currently holds only
  `tool_invocations`
  ([AnalyticsSchema.cs](../../../../src/DevContextMcp.Infrastructure/Analytics/AnalyticsSchema.cs)).
- The context page is served from `/api/context`
  ([ContextEndpoints.cs](../../../../src/DevContextMcp.Server/api/ContextEndpoints.cs)),
  returning the current indexed inventory
  ([IndexedContextResponse](../../../../src/DevContextMcp.Server.Core/Models/Context/IndexedContextResponse.cs))
  — what is in the index now, not what the last run did.

## In Scope

- Capture, per package, the available-version count, indexed version(s), status, and
  any error during a full indexing run.
- Persist this as a **last-run snapshot** that is replaced on every run (one run only).
- Expose the snapshot through the context API.
- Render it on the UI context page as a NuGet packages table.

## Functional Requirements

### FR-1: Capture per-package run data

During a full indexing run the indexer must capture, for each configured package:

- Package id / display name.
- Environment.
- **Available versions**: the count of stable, listed versions the feed returned for
  the package (before the default version window is applied).
- **Indexed version(s)**: the version(s) actually selected and indexed this run.
- **Status**: the per-package outcome — for example `unchanged`,`added`, `updated`,`deleted` , `failed`,
  or `not_found` (no eligible versions on the feed). Derived from the run result and
  any per-package `IndexRunError`.
- **Error**: the indexer error message for the package, when one occurred (reusing the
  per-package `IndexRunError` code/message already produced in
  [IndexCoordinator.cs](../../../../src/DevContextMcp.Indexer.Core/Services/IndexCoordinator.cs)).

### FR-2: Last-run snapshot table

The snapshot is stored in the analytics database in a new table that holds **only the
most recent run**. Each new full run replaces the previous snapshot atomically (clear
then insert, or replace-by-run), so the table never accumulates history. Schema and
version are managed alongside the existing analytics schema
([AnalyticsSchema.cs](../../../../src/DevContextMcp.Infrastructure/Analytics/AnalyticsSchema.cs),
bump `Version`). A run-level header (generated-at, overall status) accompanies the
per-package rows.

The analytics database is shared between the indexer and the host the same way the
documentation index (`docs.db`) already is: the **indexer writes** the snapshot,
opening the analytics database with the existing self-creating/schema-ensuring path
([SqliteAnalyticsStore](../../../../src/DevContextMcp.Infrastructure/Analytics/SqliteAnalyticsStore.cs)),
and the **host reads** it for the API. The analytics database path is supplied to the
indexer via configuration, mirroring how the documentation index path is configured.

### FR-3: Snapshot API

The snapshot must be retrievable by the UI. Either extend `/api/context`
([ContextEndpoints.cs](../../../../src/DevContextMcp.Server/api/ContextEndpoints.cs))
to include a `lastRun` section, or add a sibling endpoint (e.g. `/api/context/last-run`).
The response includes, per package: name, environment, available-version count,
indexed version(s), status, and error message. When no run has been recorded, the API
returns an empty snapshot rather than an error.

### FR-4: UI context page table

The context page shows a NuGet packages table populated from the snapshot with columns:

- NuGet name
- Environment
- Number of available versions
- Status
- Indexed version(s)
- Indexer error

Rows whose status is failed (or that carry an error) are visually distinguishable. The
table reflects the last run only.

## Non-Functional Requirements

- Writing the snapshot must not block or fail the indexing run; a snapshot-write
  failure is logged and does not change the run's success status.
- The snapshot is consistent: it always reflects exactly one run, never a mix.
- Reading the snapshot tolerates a missing table/older schema (returns empty).

## Deliverables

- Per-package run capture in the indexer (available count, indexed versions, status,
  error).
- Analytics schema addition + snapshot read/write store methods.
- Context API change exposing the snapshot.
- UI context-page table.
- Unit/integration tests for capture, replace-on-run, and the API shape.

## Acceptance Criteria

1. After a run, the snapshot lists one row per configured package with available
   count, indexed version(s), status, and error.
2. A package whose feed offers more versions than the window shows
   available > indexed (e.g. available 5, indexed `2.8.1, 1.8.8`) with status
   `added` (or `unchanged` on a re-run with no changes).
3. A package that failed shows status `failed`, its error message, and no indexed
   versions.
4. Running the indexer again fully replaces the snapshot (no rows from prior runs).
5. The context page renders the table; failed/error rows are highlighted.
6. `dotnet build` and `dotnet test` succeed.

## Out of Scope

- Multi-run history or trends (snapshot keeps one run only).
- Retrieval/tool behavior and analytics for tool invocations.
- Changes to the version-selection window itself.
