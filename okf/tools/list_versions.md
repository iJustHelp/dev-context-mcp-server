---
type: MCP Tool
title: list_versions
description: Lists indexed versions for a library and identifies the recommended version.
resource: src/DevContextMcp.Server/Tools/ListVersionsTool.cs
tags: [tool, versions]
timestamp: 2026-07-15T00:00:00Z
---

# list_versions

Lists the indexed versions of a library and identifies the recommended version.

# Schema

| Input | Type | Default | Notes |
|-------|------|---------|-------|
| `libraryId` | string | — | Stable library identifier returned by [resolve_library](./resolve_library.md). |

**Output:** `ListVersionsResponse` — indexed versions plus the recommended
version. See [Version & Environment Resolution](/version-resolution.md) for how
the recommended version is chosen.

# Examples

```text
list_versions libraryId="nuget:qa/Demo.Cities"
```

# Citations

[1] [ListVersionsTool.cs](../../src/DevContextMcp.Server/Tools/ListVersionsTool.cs)
