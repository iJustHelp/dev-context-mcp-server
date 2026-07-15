---
type: Reference
title: Resources & Citations
description: The nuget:// resource templates the server serves and how tool citation URIs are constructed.
resource: src/DevContextMcp.Server/Resources/NuGetResources.cs
tags: [resources, citations, uri]
timestamp: 2026-07-15T00:00:00Z
---

# Resources & Citations

Successful [query_docs](./query_docs.md) and [get_symbol](./get_symbol.md)
results attach a `citationUri` to each fragment and symbol. Those URIs resolve to
read-only MCP resources served by the same host; the server does not contact a
NuGet feed during retrieval.

# Schema

## Resource templates — `NuGetResources`

| Template | Reads |
|----------|-------|
| `nuget://{source}/{packageId}/{version}/artifact/{path}` | A README / XML-doc / text artifact (`ReadArtifactAsync`). |
| `nuget://{source}/{packageId}/{version}/symbol/{qualifiedName}` | A symbol signature + documentation (`ReadSymbolAsync`). |

Both apply the configured `QueryTimeout`, call `INuGetReadStore`, return
`TextResourceContents`, and throw `McpException` on a miss. Each URI segment is
URL-decoded via a `Decode` helper that rejects blank/null/control-character segments.

## Citation construction — `CitationFactory`

`ICitationFactory` is the single source of truth that builds the canonical
`nuget://…` URIs used both as citations on tool results and as the returned
resource `Uri`. It escapes each segment and normalizes backslashes to `/` in
paths, so a citation always points at a servable resource.

# Examples

```text
nuget://prod/Demo.Cities/1.0.0/artifact/readme.md
nuget://prod/Demo.Cities/1.0.0/symbol/Demo.Cities.CityService
```

# Citations

[1] [NuGetResources.cs](../../src/DevContextMcp.Server/Resources/NuGetResources.cs)
[2] [CitationFactory.cs](../../src/DevContextMcp.Server.Core/Services/CitationFactory.cs)
