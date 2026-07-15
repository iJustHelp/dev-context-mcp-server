---
type: Reference
title: Data Flows — Indexing & Retrieval
description: How the Indexer writes to the index and how the Server reads from it, with isolation guarantees.
tags: [indexing, retrieval, idempotency, safety]
timestamp: 2026-07-15T00:00:00Z
---

# Data Flows

## Indexing (write path)

**Writer:** [DevContextMcp.Indexer](/projects/indexer.md) (sole writer).

1. Read configured sources and external package-policy files.
2. Discover package versions per feed.
3. Download bounded `.nupkg` files and extract index data — **no code execution**.
4. Publish each source atomically to SQLite; update FTS5.
5. Reconcile: remove packages no longer in configuration (configuration is the source of truth). Feeds whose discovery fails skip reconciliation, so an unreachable feed never deletes already-indexed data.

**Idempotency:** Content hashes and deterministic IDs make repeated runs
idempotent. Unchanged content is not rewritten. `index_runs` records every
execution, while canonical package/version/artifact/symbol rows are not
duplicated.

**Safety limits:** package size, document size, archive entry count,
extracted-size, compression ratio, and path-traversal validation. Package
archives are treated as untrusted input; assemblies are inspected via metadata
APIs, never loaded. See [Security & Safety Model](/security-model.md) for details.

After a run, the result is projected into an index-run snapshot — see [Analytics](/analytics.md).

## Retrieval (read path)

**Reader:** [DevContextMcp.Server](/projects/server.md) (read-only).

1. MCP client sends a request (stdio or Streamable HTTP).
2. A Host tool maps it to a typed retrieval request contract.
3. The handler runs a version-scoped query against `INuGetReadStore`.
4. Read-only SQL or FTS5 search returns indexed records.
5. The response is returned with citations. **No NuGet feed is contacted.**

**Isolation:** Environment-qualified `nuget:{environment}/{packageId}` IDs never
cross environments. All evidence is isolated to one selected package version;
evidence from different versions is never combined.

See [MCP Surface](/mcp-surface.md) for the request/response contracts and citation URIs.

# Citations

[1] [Solution architecture](../design/architecture.md)
[2] [README.md](../README.md)
