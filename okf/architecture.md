---
type: Reference
title: Architecture & Dependency Rules
description: Project roles, the enforced dependency graph, and DI composition methods.
resource: design/architecture.md
tags: [clean-architecture, dependencies, composition]
timestamp: 2026-07-15T00:00:00Z
---

# Architecture & Dependency Rules

DevContextMcp separates package index production from MCP retrieval. The Host is
retrieval-only; the Indexer CLI is the separate one-shot composition root and
sole index writer.

# Schema

Project roles:

| Project | Role | Type |
|---------|------|------|
| [DevContextMcp.Server.Core](projects/server-core.md) | application | library |
| [DevContextMcp.Indexer.Core](projects/indexer-core.md) | indexer-domain | library |
| [DevContextMcp.Infrastructure](projects/infrastructure.md) | infrastructure | library |
| [DevContextMcp.Server](projects/server.md) | host | executable |
| [DevContextMcp.Indexer](projects/indexer.md) | indexer-cli | executable |
| [Tests](projects/tests.md) | tests | test |

Dependency rules (enforced by architecture tests; arrows read "depends on"):

```text
Application    -> (none)
Indexer.Core   -> (none)
Infrastructure -> Application + Indexer.Core
Host           -> Application + Infrastructure
Indexer        -> Indexer.Core + Infrastructure
Tests          -> projects required by each scenario
```

Architecture tests also verify that the former indexing projects are absent.

# Composition

DI registration entry points:

* `Application.AddApplication()` — retrieval handlers and policies.
* `Indexer.Core.AddIndexer()` — indexing orchestration only.
* `Infrastructure.AddRetrievalInfrastructure()` — read-only retrieval adapters.
* `Infrastructure.AddIndexingInfrastructure()` — concrete indexing adapters.
* `Host.AddDevContextMcpCore()` — binds Host configuration and composes retrieval.
* `Indexer.AddIndexerCli(configuration)` — binds CLI config and composes the full indexing pipeline.
* `Host.WithDevContextMcpTools()` — publishes tools and resources.

Retrieval and indexing use separate registration methods so the Host never
composes index writers.

See [Data Flows](data-flows.md) for how the layers interact at runtime.

# Citations

[1] [Solution architecture](../design/architecture.md)
[2] [DevContextMcp.slnx](../DevContextMcp.slnx)
