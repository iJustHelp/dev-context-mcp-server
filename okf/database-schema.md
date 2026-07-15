---
type: Database Schema
title: Database Schema
description: SQLite/FTS5 schema for the documentation index database and the host-owned analytics database.
resource: src/DevContextMcp.Infrastructure/Indexer/Persistence/SqliteIndexStore.Schema.cs
tags: [sqlite, fts5, schema, index, analytics]
timestamp: 2026-07-15T00:00:00Z
---

# Database Schema

DevContextMcp uses two SQLite databases:

* **Documentation index** (`database/docs.db`) — written only by [DevContextMcp.Indexer](/projects/indexer.md), read only by [DevContextMcp.Server](/projects/server.md). Schema **version 1**, stamped into `PRAGMA user_version`. There are **no in-place migrations**; bump the version and rebuild the index when the schema changes. The read store refuses any database older than its expected version.
* **Analytics** — self-creating and owned solely by the host (Server). Schema **version 3**, stamped into `PRAGMA user_version`. Uses `CREATE TABLE IF NOT EXISTS`.

# Schema

## Documentation index database

Applied once at creation (see `SqliteIndexStore.Schema.cs`). All primary keys are
`TEXT` (deterministic IDs). `INTEGER` columns holding `0`/`1` are boolean flags;
timestamps are ISO-8601 `TEXT`.

### sources

One row per configured feed. `environment` is the environment slug; `kind`
defaults to `nuget`.

| Column | Type | Notes |
|--------|------|-------|
| id | TEXT | PRIMARY KEY |
| name | TEXT | NOT NULL |
| environment | TEXT | NOT NULL |
| service_index | TEXT | NOT NULL |
| kind | TEXT | NOT NULL, default `nuget` |
| last_indexed_at | TEXT | NULL |

### libraries

One row per package within a source.

| Column | Type | Notes |
|--------|------|-------|
| id | TEXT | PRIMARY KEY |
| source_id | TEXT | NOT NULL → `sources(id)` ON DELETE CASCADE |
| package_id | TEXT | NOT NULL |
| normalized_package_id | TEXT | NOT NULL |
| kind | TEXT | NOT NULL, default `nuget` |
| display_name | TEXT | NULL |

Constraints: `UNIQUE(source_id, normalized_package_id)`; index
`ix_libraries_normalized_package_id(normalized_package_id)`.

### library_versions

One row per indexed package version. `content_hash` drives idempotent re-indexing.

| Column | Type | Notes |
|--------|------|-------|
| id | TEXT | PRIMARY KEY |
| library_id | TEXT | NOT NULL → `libraries(id)` ON DELETE CASCADE |
| version | TEXT | NOT NULL |
| content_hash | TEXT | NOT NULL |
| title, description, summary, authors, tags | TEXT | NULL |
| project_url, repository_url | TEXT | NULL |
| is_listed, is_prerelease, is_deprecated | INTEGER | NOT NULL (boolean flags) |
| published_at | TEXT | NULL |
| indexed_at | TEXT | NOT NULL |

Constraints: `UNIQUE(library_id, version)`; index
`ix_library_versions_selection(library_id, is_listed, is_prerelease, version)`
supports version selection.

### artifacts

Stored package files (README, XML docs, text). `content` may be NULL for large
or binary entries.

| Column | Type | Notes |
|--------|------|-------|
| id | TEXT | PRIMARY KEY |
| library_version_id | TEXT | NOT NULL → `library_versions(id)` ON DELETE CASCADE |
| path | TEXT | NOT NULL |
| kind | TEXT | NOT NULL |
| content_hash | TEXT | NOT NULL |
| size | INTEGER | NOT NULL |
| content | TEXT | NULL |

Constraints: `UNIQUE(library_version_id, path)`.

### document_chunks

Searchable, ordered chunks derived from artifacts.

| Column | Type | Notes |
|--------|------|-------|
| id | TEXT | PRIMARY KEY |
| library_version_id | TEXT | NOT NULL → `library_versions(id)` ON DELETE CASCADE |
| artifact_id | TEXT | NULL → `artifacts(id)` ON DELETE SET NULL |
| path | TEXT | NOT NULL |
| kind | TEXT | NOT NULL |
| member_name | TEXT | NULL |
| ordinal | INTEGER | NOT NULL |
| content | TEXT | NOT NULL |
| content_hash | TEXT | NOT NULL |

Constraints: `UNIQUE(library_version_id, path, member_name, ordinal)`; index
`ix_document_chunks_lookup(library_version_id, kind, member_name)`.

### document_chunks_fts (FTS5 virtual table)

Full-text search over chunk content. `tokenize = 'unicode61'`.

Columns: `document_chunk_id` (UNINDEXED), `package_id`, `version` (UNINDEXED),
`path`, `member_name`, `content`.

### symbols

Public types and members extracted via metadata APIs.

| Column | Type | Notes |
|--------|------|-------|
| id | TEXT | PRIMARY KEY |
| library_version_id | TEXT | NOT NULL → `library_versions(id)` ON DELETE CASCADE |
| namespace | TEXT | NOT NULL |
| fully_qualified_name | TEXT | NOT NULL |
| kind | TEXT | NOT NULL |
| signature | TEXT | NOT NULL |
| containing_type | TEXT | NULL |
| assembly_path | TEXT | NOT NULL |
| target_framework | TEXT | NULL |
| xml_documentation_member | TEXT | NULL |

Indexes: `ix_symbols_fully_qualified_name(fully_qualified_name)`,
`ix_symbols_lookup(library_version_id, fully_qualified_name, target_framework)`,
`ix_symbols_containing_type(library_version_id, containing_type)`.

### dependencies

| Column | Type | Notes |
|--------|------|-------|
| id | TEXT | PRIMARY KEY |
| library_version_id | TEXT | NOT NULL → `library_versions(id)` ON DELETE CASCADE |
| package_id | TEXT | NOT NULL |
| version_range | TEXT | NOT NULL |
| target_framework | TEXT | NULL |

### target_frameworks

| Column | Type | Notes |
|--------|------|-------|
| library_version_id | TEXT | NOT NULL → `library_versions(id)` ON DELETE CASCADE |
| framework | TEXT | NOT NULL |

Primary key: `(library_version_id, framework)`.

### index_runs

One row per Indexer execution against a source (run history; intentionally not
deduplicated).

| Column | Type | Notes |
|--------|------|-------|
| id | TEXT | PRIMARY KEY |
| source_id | TEXT | NOT NULL → `sources(id)` ON DELETE CASCADE |
| status | TEXT | NOT NULL |
| started_at, completed_at | TEXT | NOT NULL |
| duration_ms | INTEGER | NOT NULL |
| indexed_count, changed_count, unchanged_count, error_count | INTEGER | NOT NULL |

### index_run_errors

| Column | Type | Notes |
|--------|------|-------|
| id | TEXT | PRIMARY KEY |
| index_run_id | TEXT | NOT NULL → `index_runs(id)` ON DELETE CASCADE |
| code | TEXT | NOT NULL |
| message | TEXT | NOT NULL |
| package_id | TEXT | NULL |
| version | TEXT | NULL |

### libraries_fts (FTS5 virtual table)

Full-text discovery for `resolve_library`. `tokenize = 'unicode61'`.

Columns: `library_id` (UNINDEXED), `source_name` (UNINDEXED), `package_id`,
`title`, `description`, `summary`, `tags`, `document_text`.

## Analytics database

Self-creating, host-owned. Schema version 3.

### tool_invocations

One row per MCP tool call, for usage analytics.

| Column | Type | Notes |
|--------|------|-------|
| id | TEXT | PRIMARY KEY |
| tool_name | TEXT | NOT NULL |
| user_name | TEXT | NOT NULL |
| started_at | TEXT | NOT NULL |
| duration_ms | REAL | NOT NULL |
| status | TEXT | NOT NULL |
| tool_result_status | TEXT | NOT NULL, default `ok` |
| error_type | TEXT | NULL |
| request_bytes, response_bytes | INTEGER | NULL |
| result_detail_json | TEXT | NULL |

Indexes: `ix_ti_started(started_at)`, `ix_ti_tool(tool_name, started_at)`,
`ix_ti_user(user_name, started_at)`.

### index_snapshot_meta

Single-row snapshot metadata (`CHECK (id = 1)`).

| Column | Type | Notes |
|--------|------|-------|
| id | INTEGER | PRIMARY KEY, `CHECK (id = 1)` |
| generated_at | TEXT | NOT NULL |
| status | TEXT | NOT NULL |

### index_snapshot_packages

| Column | Type | Notes |
|--------|------|-------|
| package_id | TEXT | NOT NULL |
| environment | TEXT | NOT NULL |
| available_versions | INTEGER | NOT NULL |
| indexed_versions | TEXT | NOT NULL |
| status | TEXT | NOT NULL |
| error | TEXT | NULL |

See [Data Flows](/data-flows.md) for how these tables are written and read,
[Analytics](/analytics.md) for the analytics capture and snapshot lifecycle, and
[DevContextMcp.Infrastructure](/projects/infrastructure.md) for the persistence code.

# Citations

[1] [SqliteIndexStore.Schema.cs](../src/DevContextMcp.Infrastructure/Indexer/Persistence/SqliteIndexStore.Schema.cs)
[2] [IndexSchema.cs](../src/DevContextMcp.Infrastructure/IndexSchema.cs)
[3] [AnalyticsSchema.cs](../src/DevContextMcp.Infrastructure/Analytics/AnalyticsSchema.cs)
