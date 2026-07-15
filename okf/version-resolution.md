---
type: Reference
title: Version & Environment Resolution
description: How a library ID plus optional version hints resolve to exactly one indexed package version and environment.
resource: src/DevContextMcp.Server.Core/Services/VersionResolver.cs
tags: [version-selection, environment, library-id, resolution]
timestamp: 2026-07-15T00:00:00Z
---

# Version & Environment Resolution

For `query_docs` and `get_symbol`, retrieval resolves one library and one version
before searching. Evidence is never combined across versions or environments.
This complements the ordered summary in [MCP Surface](mcp-surface.md).

# Schema

## Library IDs — `LibraryId`

Parses/formats `nuget:{environment}/{package}` (or legacy `nuget:{package}`):

- Requires the `nuget:` prefix (case-insensitive) with a non-empty payload.
- A single `/` splits environment and package; a leading/trailing slash or a
  second slash is rejected.
- Environment names must match `^[A-Za-z0-9._-]+$`; the package id must be non-empty.

## Version precedence — `VersionResolver`

Versions are parsed and ordered descending by `NuGet.Versioning`
(`VersionComparer.VersionRelease`), then selected in order. The result carries a
`Reason`:

| Order | Condition | Reason |
|-------|-----------|--------|
| 1 | Exact match for the requested `version` | `requested` (no match → resolution fails). |
| 2 | Exact match for `projectVersion` | `project_context` (no match → resolution fails). |
| 3 | Exact match for the configured recommended version, and it is **not** prerelease | `configured_recommendation`. |
| — | Recommended requested but missing or prerelease | adds warning `recommended_version_not_indexed`, then falls through. |
| 4 | Highest listed, non-prerelease version | `latest_stable`. |

If no listed stable version exists, resolution returns none.

## Library selection — `RetrievalLibraryResolver`

Resolves a `LibraryId` to a single best selection:

1. Verify the environment exists (else `EnvironmentNotFound`).
2. Find matching libraries, optionally filtered by environment.
3. Resolve a version per candidate (above).
4. Order by: has-version → environment priority (`EnvironmentOrder` from
   [Server Configuration](server-configuration.md)) → recommended-version-warning
   → environment/source name.

An environment-qualified id never falls back to another environment. Returns a
result with status `Resolved`, `EnvironmentNotFound`, or `LibraryNotFound`.

# Citations

[1] [VersionResolver.cs](../src/DevContextMcp.Server.Core/Services/VersionResolver.cs)
[2] [RetrievalLibraryResolver.cs](../src/DevContextMcp.Server.Core/Services/RetrievalLibraryResolver.cs)
[3] [LibraryId.cs](../src/DevContextMcp.Server.Core/Services/LibraryId.cs)
