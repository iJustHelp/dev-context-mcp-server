---
type: .NET Project
title: DevContextMcp.Indexer
description: One-shot indexing executable and sole index writer; the separate composition root for indexing.
resource: src/DevContextMcp.Indexer/DevContextMcp.Indexer.csproj
tags: [project, indexer-cli, executable]
timestamp: 2026-07-15T00:00:00Z
---

# DevContextMcp.Indexer

**Role:** indexer-cli &nbsp;·&nbsp; **Type:** executable &nbsp;·&nbsp; **Solution folder:** `/src/Indexer/`

The one-shot indexing executable and composition root; the sole index writer:

* Owns indexing configuration, validation, and `appsettings.json`.
* Loads and caches external package-policy JSON at startup.
* Runs every configured source once and reports summaries.

# Schema

| Aspect | Value |
|--------|-------|
| Project references | [DevContextMcp.Indexer.Core](./indexer-core.md), [DevContextMcp.Infrastructure](./infrastructure.md) |
| Package references | `Microsoft.Extensions.Hosting`, `Microsoft.Extensions.Options.ConfigurationExtensions`, `NuGet.Versioning`, `Serilog.Extensions.Hosting`, `Serilog.Settings.Configuration`, `Serilog.Sinks.Console`, `Serilog.Sinks.File` |
| Composition | `Indexer.AddIndexerCli(configuration)` binds CLI config and composes the full indexing pipeline. |
| Output database | `database/docs.db` |

Exit codes:

| Code | Meaning |
|------|---------|
| `0` | success, or no configured sources |
| `1` | partial success, failure, invalid configuration, or cancellation |

**Constraint:** Only one CLI process may write to a given SQLite database at a
time. Recurring execution is delegated to an external scheduler.

# Examples

```powershell
dotnet run --project .\src\DevContextMcp.Indexer\DevContextMcp.Indexer.csproj
```

See [Data Flows](/data-flows.md) for the write path.

# Citations

[1] [DevContextMcp.Indexer.csproj](../../src/DevContextMcp.Indexer/DevContextMcp.Indexer.csproj)
[2] [Solution architecture](../../design/architecture.md)
