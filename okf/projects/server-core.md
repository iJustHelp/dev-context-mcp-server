---
type: .NET Project
title: DevContextMcp.Server.Core
description: Application library — MCP wire contracts, retrieval models/abstractions, and retrieval services.
resource: src/DevContextMcp.Server.Core/DevContextMcp.Server.Core.csproj
tags: [project, application, library]
timestamp: 2026-07-15T00:00:00Z
---

# DevContextMcp.Server.Core

**Role:** application &nbsp;·&nbsp; **Type:** library &nbsp;·&nbsp; **Solution folder:** `/src/Server/`

Contains MCP wire contracts, retrieval models and abstractions, and retrieval
services, including version-selection policy.

# Schema

| Aspect | Value |
|--------|-------|
| Project references | (none) |
| Package references | `Microsoft.Extensions.DependencyInjection.Abstractions`, `NuGet.Versioning` |
| Composition | `Application.AddApplication()` registers retrieval handlers and policies. |

**Constraint:** No project references. Does not depend on SQLite, NuGet, or MCP
transport implementations.

Consumed by [DevContextMcp.Infrastructure](./infrastructure.md) and
[DevContextMcp.Server](./server.md). See [Architecture & Dependency Rules](/architecture.md).

# Citations

[1] [DevContextMcp.Server.Core.csproj](../../src/DevContextMcp.Server.Core/DevContextMcp.Server.Core.csproj)
[2] [Solution architecture](../../design/architecture.md)
