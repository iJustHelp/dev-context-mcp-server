---
type: Reference
title: Analytics Subsystem
description: How tool invocations are captured, buffered, stored, and how the index-run snapshot is published and served.
resource: src/DevContextMcp.Infrastructure/Analytics/SqliteAnalyticsStore.cs
tags: [analytics, capture, sqlite, snapshot]
timestamp: 2026-07-15T00:00:00Z
---

# Analytics Subsystem

[DevContextMcp.Server](/projects/server.md) records every MCP tool call into a
separate, host-owned analytics database and publishes a snapshot of the last
indexing run. The subsystem is deliberately best-effort: analytics failures are
logged and never surface to tool callers. See the
[Database Schema](/database-schema.md) for the analytics tables and the
[HTTP API](/http-api.md) for how the data is served.

# Schema

## Capture pipeline

1. A tool invocation is wrapped by the invocation logger (see [MCP Tools](/tools/index.md)),
   which enqueues a `ToolInvocationRecord` onto the in-process `AnalyticsRecorder`
   channel.
2. `AnalyticsWriterHostedService` (a `BackgroundService`) drains the channel,
   batches up to **200** records, and calls `IToolInvocationWriteStore.AppendAsync`.
3. On host shutdown, remaining buffered events are discarded. Persistence
   exceptions are logged (`LogError`) but never thrown back to the caller.

## Store — `SqliteAnalyticsStore`

One class implementing four contracts: `IToolInvocationWriteStore`,
`IToolInvocationReadStore`, `IIndexSnapshotWriteStore`, `IIndexSnapshotReadStore`.

- Writes are append-only `INSERT OR IGNORE` inside a transaction, with WAL journaling.
- Aggregates are computed in SQL; percentiles (p50/p95) are computed in-process
  via linear interpolation.
- Forward-compatible migration probes for columns (`HasColumnAsync`) and adds
  `tool_result_status` / `result_detail_json` to older databases, degrading
  gracefully in read queries when they are absent.
- `AnalyticsDetailJson` serializes the per-call `result_detail_json` blob that
  backs the "has detail" flag and the recent-call detail view.

## Index-run snapshot lifecycle

- After an indexing run, [DevContextMcp.Indexer](/projects/indexer.md) calls
  `IIndexRunSnapshotPublisher.PublishAsync(IndexRunResult)`, projecting the run
  into an `IndexSnapshot` (generated-at, run status, and per-package environment,
  available-vs-indexed versions, status, and error).
- The publisher calls `IIndexSnapshotWriteStore.ReplaceAsync`, which clears both
  `index_snapshot_meta` and `index_snapshot_packages` and rewrites a single meta
  row (`id = 1`) plus one row per package.
- Snapshot write failures are logged as warnings and never fail the indexing run.
- `GET /api/context/last-run` reads it back via `GetAsync` (returns an empty
  `"none"` snapshot when the meta table is absent).

# Citations

[1] [AnalyticsWriterHostedService.cs](../src/DevContextMcp.Server/Analytics/AnalyticsWriterHostedService.cs)
[2] [SqliteAnalyticsStore.cs](../src/DevContextMcp.Infrastructure/Analytics/SqliteAnalyticsStore.cs)
[3] [AnalyticsSchema.cs](../src/DevContextMcp.Infrastructure/Analytics/AnalyticsSchema.cs)
[4] [IndexRunSnapshotPublisher.cs](../src/DevContextMcp.Indexer/IndexRunSnapshotPublisher.cs)
