# Stage 2 BRD: NuGet Indexing

## Purpose

Build the ingestion pipeline that converts approved NuGet package versions into
safe, normalized, searchable records.

## Business Outcome

Internal package content is available in a local index without loading or
executing package assemblies.

## Dependencies

- Stage 1 completed.
- Access to a NuGet v3 fixture feed for automated tests.
- At least one representative internal package approved for validation.

## In Scope

- Connect to configured NuGet v3 feeds with official NuGet client APIs.
- Discover packages and versions allowed by configured prefixes or filters.
- Download package archives safely.
- Parse package metadata, dependencies, README files, and XML documentation.
- Inspect public assembly symbols without executing package code.
- Create the SQLite schema and FTS5 indexes.
- Persist artifacts, symbols, chunks, versions, and index-run records.
- Make indexing incremental and idempotent at artifact level.

## Functional Requirements

### FR-1: Feed access

The indexer must use configured NuGet v3 service indexes and official NuGet
client resources. It must support cancellation, timeouts, source caching, and
an injectable credential mechanism.

The indexer must not accept arbitrary feed URLs from MCP tool calls.

### FR-2: Package selection

For each source, configuration must control:

- Allowed package IDs or prefixes.
- Inclusion of prerelease versions.
- Inclusion of unlisted versions.
- Maximum versions retained per package.
- Optional explicit package allowlist.

Version values must be normalized and compared using NuGet semantic-version
rules.

### FR-3: Safe package download

Package downloads must enforce:

- Maximum archive size.
- Timeout and cancellation.
- ZIP entry count and extracted-size limits.
- Protection against path traversal.
- Content hashing before persistence.

Downloaded package code must never be executed or loaded into the application
runtime.

### FR-4: Metadata extraction

The pipeline must capture:

- Package ID and version.
- Description, authors, tags, and project/repository links.
- Listed, prerelease, and deprecation state when available.
- Dependencies and dependency groups.
- Supported target frameworks.
- Package publication time when available.

### FR-5: Documentation extraction

The pipeline must extract:

- Package README content.
- XML documentation members.
- Approved Markdown and text documentation included in the package.

Large documents must be split on semantic boundaries and retain source
location metadata.

### FR-6: Symbol extraction

Managed assemblies must be inspected through metadata-only APIs.

For public symbols, persist:

- Namespace and fully qualified name.
- Symbol kind.
- C#-style signature.
- Containing type and assembly.
- Target framework.
- Related XML documentation member.

### FR-7: Persistence

SQLite must persist the core entities defined in the main specification.
Searchable text must be represented in FTS5.

An indexing run must:

- Upsert changed artifacts by content hash.
- Skip unchanged artifacts.
- Commit a source update atomically.
- Preserve the previous successful index when processing fails.
- Record counts, duration, status, and errors.

## Non-Functional Requirements

- Re-running an unchanged source produces no duplicate records.
- A malformed package cannot stop other configured packages from indexing.
- Sensitive credentials are never logged or persisted.
- Parser and storage behavior is covered by deterministic local fixtures.
- Index operations support cancellation.

## Deliverables

- NuGet source client.
- Safe package archive reader.
- Metadata, README, XML documentation, and symbol extractors.
- SQLite schema, migrations, and FTS5 setup.
- Index orchestration and index-run reporting.
- NuGet feed and package fixtures.
- Unit and integration tests.

## Acceptance Criteria

1. A fixture NuGet v3 feed can be queried and indexed.
2. A package version produces metadata, dependency, framework, document, and
   public-symbol records.
3. README and XML documentation text appears in FTS5.
4. Assembly inspection does not execute package code.
5. Reindexing unchanged content does not duplicate or rewrite records.
6. A failed update preserves the previous successful data.
7. Oversized, malformed, or path-traversing archives are rejected safely.
8. `dotnet build` and `dotnet test` succeed.

## Out of Scope

- MCP retrieval behavior beyond diagnostics.
- Natural-language result ranking.
- Shared HTTP deployment.

## Exit Gate

Stage 3 may begin when a representative package can be indexed end to end and
its metadata, documentation, and symbols can be queried directly from SQLite.
