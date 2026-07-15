---
type: Reference
title: MCP Surface
description: The four MCP tools, library-ID format, citation URIs, and version selection order.
tags: [tools, resources, citations, version-selection]
timestamp: 2026-07-15T00:00:00Z
---

# MCP Surface

# Schema

Tools exposed by [DevContextMcp.Server](/projects/server.md):

| Tool | Purpose | Important inputs |
|------|---------|------------------|
| `resolve_library` | Find indexed NuGet packages by ID, name, or concept. | `query`, `environment`, `includePrerelease`, `limit` |
| `list_versions` | List indexed versions and identify the recommended version. | `libraryId`, `includePrerelease` |
| `query_docs` | Search version-scoped package evidence. | `libraryId`, `question`, `version`, `projectVersion`, `targetFramework`, `maxResults` |
| `get_symbol` | Return a public type/member's indexed signature and XML docs. | `libraryId`, `symbol`, `version`, `projectVersion`, `targetFramework` |

Machine-readable outcome codes: `ok`, `not_found`, `insufficient_evidence`.

## Library IDs

Format: `nuget:{environment}/{packageId}`. Must use the `nuget:` prefix; legacy
`docs:` IDs are not supported. Environment-qualified IDs never fall back to
another environment.

# Examples

```text
nuget:prod/Demo.Cities
nuget:qa/Demo.Cities
```

Citation URIs point to read-only MCP resources:

```text
nuget://{source}/{packageId}/{version}/artifact/{path}
nuget://{source}/{packageId}/{version}/symbol/{qualifiedName}
```

## Version selection order

For `query_docs` and `get_symbol`, one version is selected in this order:

1. Exact `version` from the tool request.
2. Exact `projectVersion` supplied as calling-project context.
3. Environment-qualified version-selection entry.
4. Package-wide version-selection entry.
5. Latest indexed, listed stable version.
6. Latest indexed, listed prerelease (when prereleases are allowed).

The selected version must already be in the local index. Evidence from
different package versions is never combined.

See [MCP Tools](/tools/index.md) for per-tool inputs/outputs, [Retrieval Contracts](/retrieval-contracts.md)
for the response envelope and status codes, [Version & Environment Resolution](/version-resolution.md)
for selection mechanics, and [Data Flows](/data-flows.md) for the retrieval path behind these tools.

# Citations

[1] [README.md](../README.md)
[2] [Solution architecture](../design/architecture.md)
