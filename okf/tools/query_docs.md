---
type: MCP Tool
title: query_docs
description: Finds indexed documentation and examples for one internal library, scoped to a single version.
resource: src/DevContextMcp.Server/Tools/QueryDocsTool.cs
tags: [tool, documentation, search]
timestamp: 2026-07-15T00:00:00Z
---

# query_docs

Finds indexed documentation and examples for one internal library. Evidence is
isolated to a single selected version.

# Schema

| Input | Type | Default | Notes |
|-------|------|---------|-------|
| `libraryId` | string | — | Stable library identifier from [resolve_library](./resolve_library.md). |
| `question` | string | — | A focused topic or question; short, topical queries (1–3 words) retrieve best. |
| `version` | string? | null | Exact package or client version. |
| `targetFramework` | string? | null | Calling project's target framework, e.g. `net10.0`. |
| `maxResults` | int | `8` | Maximum documentation fragments and symbols to return. |
| `projectVersion` | string? | null | Package version referenced by the calling project. |

**Output:** `QueryDocsResponse` → `QueryDocsResult` with ordered `Fragments` and
`Symbols`, each carrying a `citationUri`. Version is chosen per
[Version & Environment Resolution](/version-resolution.md); results are bounded by
the response budget (`response_truncated` warning when trimmed).

# Examples

```text
query_docs libraryId="nuget:prod/Demo.Cities" question="city lookup"
query_docs libraryId="nuget:prod/Demo.Cities" question="registration" version="1.0.0" maxResults=5
```

Open a returned `citationUri` as an MCP resource — see [Resources & citations](./resources.md).

# Citations

[1] [QueryDocsTool.cs](../../src/DevContextMcp.Server/Tools/QueryDocsTool.cs)
