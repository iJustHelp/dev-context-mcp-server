---
type: .NET Project
title: Test Projects
description: The unit and integration test projects and what they cover.
resource: tests/
tags: [project, tests, xunit]
timestamp: 2026-07-15T00:00:00Z
---

# Test Projects

Two xUnit test projects under the `/tests/` solution folder.

# Schema

## DevContextMcp.UnitTests

`tests/DevContextMcp.UnitTests/DevContextMcp.UnitTests.csproj`

Covers: configuration, archive safety, chunking, symbol extraction, version
selection, serialization, registration boundaries, and architecture rules.

## DevContextMcp.IntegrationTests

`tests/DevContextMcp.IntegrationTests/DevContextMcp.IntegrationTests.csproj`

References all five source projects. Covers:

* Indexing local fixture packages into temporary SQLite databases.
* Exercising retrieval end to end.
* Child-process tests verifying Indexer CLI exit codes and Host stdio/HTTP behavior.
* Idempotency tests verifying canonical rows stay unique while run history grows.

See [Architecture & Dependency Rules](../architecture.md) for the dependency graph these tests enforce.

# Citations

[1] [DevContextMcp.IntegrationTests.csproj](../../tests/DevContextMcp.IntegrationTests/DevContextMcp.IntegrationTests.csproj)
[2] [Test plan](../../design/test-plan.md)
