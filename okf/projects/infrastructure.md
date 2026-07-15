---
type: .NET Project
title: DevContextMcp.Infrastructure
description: Infrastructure library — SQLite/FTS5 retrieval, NuGet access, safe archive/symbol extraction, and persistence.
resource: src/DevContextMcp.Infrastructure/DevContextMcp.Infrastructure.csproj
tags: [project, infrastructure, library, sqlite, nuget]
timestamp: 2026-07-15T00:00:00Z
---

# DevContextMcp.Infrastructure

**Role:** infrastructure &nbsp;·&nbsp; **Type:** library &nbsp;·&nbsp; **Solution folder:** `/src/`

Implements Application retrieval abstractions and Indexer ports:

* Read-only SQLite and FTS5 retrieval.
* NuGet discovery, metadata lookup, and bounded package download.
* Safe archive inspection and metadata-only symbol extraction.
* Document chunking and SHA-256 hashing.
* SQLite schema migration, atomic publication, FTS5 writes, and run history.

# Schema

| Aspect | Value |
|--------|-------|
| Project references | [DevContextMcp.Server.Core](./server-core.md), [DevContextMcp.Indexer.Core](./indexer-core.md) |
| Package references | `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Data.Sqlite`, `NuGet.Packaging`, `NuGet.Protocol`, `NuGet.Versioning` |
| Composition | `Infrastructure.AddRetrievalInfrastructure()` (read-only retrieval adapters); `Infrastructure.AddIndexingInfrastructure()` (concrete indexing adapters) |

**Constraint:** Retrieval and indexing use separate registration methods so the
Host never composes index writers. Could alternatively be implemented with a
database or RAG backend.

Consumed by [DevContextMcp.Server](./server.md) and [DevContextMcp.Indexer](./indexer.md).

# Citations

[1] [DevContextMcp.Infrastructure.csproj](../../src/DevContextMcp.Infrastructure/DevContextMcp.Infrastructure.csproj)
[2] [Solution architecture](../../design/architecture.md)
