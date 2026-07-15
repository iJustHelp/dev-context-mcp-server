---
type: .NET Project
title: DevContextMcp.Indexer.Core
description: Indexer domain library — source-neutral indexing models, ports, and IIndexCoordinator orchestration.
resource: src/DevContextMcp.Indexer.Core/DevContextMcp.Indexer.Core.csproj
tags: [project, indexer-domain, library]
timestamp: 2026-07-15T00:00:00Z
---

# DevContextMcp.Indexer.Core

**Role:** indexer-domain &nbsp;·&nbsp; **Type:** library &nbsp;·&nbsp; **Solution folder:** `/src/Indexer/`

The inner indexing feature boundary:

* Source-neutral indexing models.
* Ports for source access, package processing, configuration, and persistence.
* `IIndexCoordinator` and indexing orchestration.

# Schema

| Aspect | Value |
|--------|-------|
| Project references | (none) |
| Package references | `Microsoft.Extensions.DependencyInjection.Abstractions` |
| Composition | `Indexer.Core.AddIndexer()` registers indexing orchestration only. |

**Constraint:** No project references. Contains no hosting, NuGet client,
archive-processing, or SQLite implementation packages.

Implemented by [DevContextMcp.Infrastructure](./infrastructure.md) and composed by
[DevContextMcp.Indexer](./indexer.md). See [Data Flows](../data-flows.md).

# Citations

[1] [DevContextMcp.Indexer.Core.csproj](../../src/DevContextMcp.Indexer.Core/DevContextMcp.Indexer.Core.csproj)
[2] [Solution architecture](../../design/architecture.md)
