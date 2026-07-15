---
type: MCP Tool
title: get_symbol
description: Finds and describes a public type or member in an internal library, returning its indexed signature and XML docs.
resource: src/DevContextMcp.Server/Tools/GetSymbolTool.cs
tags: [tool, symbol, api]
timestamp: 2026-07-15T00:00:00Z
---

# get_symbol

Finds and describes a public type or member. Accepts fully qualified, simple, or
partial names; when a lookup is ambiguous it returns bounded candidates instead
of silently choosing one (`AmbiguousSymbolLimit`, see [Server Configuration](../server-configuration.md)).

# Schema

| Input | Type | Default | Notes |
|-------|------|---------|-------|
| `libraryId` | string | — | Stable library identifier from [resolve_library](./resolve_library.md). |
| `symbol` | string | — | Fully qualified, simple, or partial symbol name. |
| `version` | string? | null | Exact package or client version. |
| `targetFramework` | string? | null | Calling project's target framework, e.g. `net10.0`. |
| `projectVersion` | string? | null | Package version referenced by the calling project. |

**Output:** `GetSymbolResponse` describing the public type/member (indexed
signature and XML documentation), with a `citationUri`. Version selection follows
[Version & Environment Resolution](../version-resolution.md).

# Examples

```text
get_symbol libraryId="nuget:prod/Demo.Cities" symbol="CityService"
get_symbol libraryId="nuget:prod/Demo.Cities" symbol="ICityService.GetCity" version="1.0.0"
```

# Citations

[1] [GetSymbolTool.cs](../../src/DevContextMcp.Server/Tools/GetSymbolTool.cs)
