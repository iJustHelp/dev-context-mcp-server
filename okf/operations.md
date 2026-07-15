---
type: Reference
title: Operations — Commands & Conventions
description: Build, test, and run commands, plus coding-standard conventions and reference docs.
tags: [build, test, run, conventions, references]
timestamp: 2026-07-15T00:00:00Z
---

# Operations

# Examples

Build and test the solution:

```powershell
dotnet build .\DevContextMcp.slnx
dotnet test .\DevContextMcp.slnx
```

Build the local index (creates `database/docs.db`):

```powershell
dotnet run --project .\src\DevContextMcp.Indexer\DevContextMcp.Indexer.csproj
```

Start the MCP server (default stateless Streamable HTTP at `http://127.0.0.1:2222/mcp`):

```powershell
dotnet run --project .\src\DevContextMcp.Server\DevContextMcp.Server.csproj
```

Connect the MCP Inspector:

```powershell
npx -y @modelcontextprotocol/inspector
```

# Conventions

* Company coding standards (architecture, naming, unit-test templates) are provided as project Cursor skills under `.cursor/skills/`, not through the MCP index.
* Build note: full builds can fail with DLL copy locks when the server exe or Visual Studio is running; use `-p:BuildProjectReferences=false` as a workaround.

# Citations

[1] [README.md](../README.md)
[2] [Indexer configuration](../docs/indexer-configuration.md)
[3] [Server configuration](../docs/server-configuration.md)
