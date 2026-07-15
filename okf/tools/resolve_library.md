---
type: MCP Tool
title: resolve_library
description: Finds indexed NuGet packages by name, client name, or implementation concept.
resource: src/DevContextMcp.Server/Tools/ResolveLibraryTool.cs
tags: [tool, discovery, resolve]
timestamp: 2026-07-15T00:00:00Z
---

# resolve_library

Finds indexed NuGet packages by name or concept and returns stable library IDs.

# Schema

| Input | Type | Default | Notes |
|-------|------|---------|-------|
| `query` | string | — | Package name, client name, or implementation concept. |
| `limit` | int | `10` | Maximum library matches to return. |
| `environment` | string? | null | Optional indexed environment such as `qa` or `production`. |

**JSON-query unwrapping:** if `query` parses as a JSON object with a string
`query` property, the tool unwraps it and also reads optional `limit` and
`environment` from that object; a normal (non-JSON) query is used as-is.

**Output:** `ResolveLibraryResponse` → `ResolveLibraryResult.Matches`, each a
`LibraryMatch` (`libraryId`, `kind`, `displayName`, `environment`, `description`,
`confidence`). See [Retrieval Contracts](/retrieval-contracts.md) for the envelope.

# Examples

```text
resolve_library query="Demo.Cities"
resolve_library query="reverse geocoding client" limit=5 environment="prod"
```

Pass a returned `libraryId` (e.g. `nuget:prod/Demo.Cities`) to
[list_versions](./list_versions.md), [query_docs](./query_docs.md), or
[get_symbol](./get_symbol.md).

# Citations

[1] [ResolveLibraryTool.cs](../../src/DevContextMcp.Server/Tools/ResolveLibraryTool.cs)
