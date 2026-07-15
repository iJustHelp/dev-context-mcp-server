---
type: Solution
title: DevContextMcp Solution Overview
description: A .NET 10 MCP server giving coding agents grounded, version-aware access to internal NuGet packages.
resource: DevContextMcp.slnx
tags: [overview, mcp, dotnet, architecture]
timestamp: 2026-07-15T00:00:00Z
---

# DevContextMcp — Solution Overview

**Solution file:** [DevContextMcp.slnx](../DevContextMcp.slnx) &nbsp;·&nbsp; **Architecture style:** clean architecture

A .NET 10 Model Context Protocol (MCP) server that gives coding agents grounded,
version-aware access to internal NuGet packages. It indexes package metadata,
README/XML/text docs, public .NET symbols, dependencies, and target frameworks,
then serves deterministic retrieval from a local SQLite/FTS5 index. Package
assemblies are **never loaded or executed**.

## Why it exists

Coding agents know public libraries but lack reliable context for private
packages, generated API clients, and environment-specific builds. This solution
turns those sources into a small, deterministic MCP surface with stable,
openable citations.

## Writer / reader process isolation

Index production (Indexer) and MCP retrieval (Server) are separate processes:

* [DevContextMcp.Indexer](/projects/indexer.md) is the *sole writer* to the SQLite database.
* [DevContextMcp.Server](/projects/server.md) opens the same database *read-only* and never contacts NuGet feeds during retrieval.

Only one Indexer process may write to a given database at a time.

## Related

See [Technology Stack](/tech-stack.md), [Architecture & Dependency Rules](/architecture.md),
[Data Flows](/data-flows.md), and [MCP Surface](/mcp-surface.md).

# Citations

[1] [README.md](../README.md)
[2] [Solution architecture](../design/architecture.md)
