---
type: Reference
title: Technology Stack
description: Language, SDK, storage, protocol, transports, and key packages used by DevContextMcp.
tags: [dotnet, sqlite, mcp, serilog, nuget]
timestamp: 2026-07-15T00:00:00Z
---

# Technology Stack

# Schema

| Aspect | Choice |
|--------|--------|
| Language | C# |
| Target framework | `net10.0` |
| .NET SDK | `10.0.301`, roll-forward `latestPatch`, `allowPrerelease: false` |
| Storage | SQLite + FTS5 (`Microsoft.Data.Sqlite`) |
| Protocol | Model Context Protocol (`ModelContextProtocol`, `ModelContextProtocol.AspNetCore`) |
| Transports | stdio; stateless Streamable HTTP (loopback `http://` only) |
| Logging | Serilog (Console + File sinks) |
| NuGet APIs | `NuGet.Packaging`, `NuGet.Protocol`, `NuGet.Versioning` |
| DI | `Microsoft.Extensions.DependencyInjection` |
| Tests | xUnit, coverlet |

# Notes

* The HTTP transport is deliberately restricted to an unauthenticated loopback `http://` address — suitable for local development, not shared-network use.
* The Server never loads or executes package assemblies; symbols are read via metadata APIs.

See [Architecture & Dependency Rules](architecture.md) for where each package is referenced,
and [Operations](operations.md) for build/test/run commands.

# Citations

[1] [global.json](../global.json)
[2] [README.md](../README.md)
