---
type: .NET Project
title: DevContextMcp.Server
description: MCP executable and retrieval composition root; opens the index read-only and exposes MCP tools.
resource: src/DevContextMcp.Server/DevContextMcp.Server.csproj
tags: [project, host, executable, mcp]
timestamp: 2026-07-15T00:00:00Z
---

# DevContextMcp.Server

**Role:** host &nbsp;·&nbsp; **Type:** executable (`Microsoft.NET.Sdk.Web`) &nbsp;·&nbsp; **Solution folder:** `/src/Server/`

The MCP executable and retrieval composition root:

* Loads and validates Host configuration (transport, retrieval options).
* Selects stdio or stateless Streamable HTTP.
* Exposes four MCP tools and NuGet resource templates.
* Opens the SQLite index read-only.

# Schema

| Aspect | Value |
|--------|-------|
| Project references | [DevContextMcp.Server.Core](./server-core.md), [DevContextMcp.Infrastructure](./infrastructure.md) |
| Package references | `Microsoft.AspNetCore.OpenApi`, `ModelContextProtocol`, `ModelContextProtocol.AspNetCore`, `Serilog.AspNetCore`, `Serilog.Settings.Configuration`, `Serilog.Sinks.Console`, `Serilog.Sinks.File` |
| Composition | `Host.AddDevContextMcpCore()` binds Host config and composes retrieval; `Host.WithDevContextMcpTools()` publishes tools and resources. |
| Default endpoint | `http://127.0.0.1:2222/mcp` |

**Constraint:** Never contacts NuGet sources or registers Indexer services.

# Examples

```powershell
dotnet run --project .\src\DevContextMcp.Server\DevContextMcp.Server.csproj
```

See [MCP Surface](../mcp-surface.md) for the exposed tools and [Data Flows](../data-flows.md) for the read path.

# Citations

[1] [DevContextMcp.Server.csproj](../../src/DevContextMcp.Server/DevContextMcp.Server.csproj)
[2] [Solution architecture](../../design/architecture.md)
