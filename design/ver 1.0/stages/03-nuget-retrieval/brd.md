# Stage 3 BRD: NuGet Retrieval

## Purpose

Expose the indexed NuGet knowledge through version-aware MCP tools with stable,
traceable citations.

## Business Outcome

A coding agent can identify an internal package, select the correct version,
find a public symbol, and retrieve grounded documentation without inventing
package APIs.

## Dependencies

- Stage 2 completed.
- Indexed fixtures containing multiple packages and versions.

## In Scope

- Library resolution by exact ID and descriptive query.
- Version selection and compatibility filtering.
- Full-text documentation retrieval and deterministic ranking.
- Exact and partial symbol lookup.
- Version listing.
- Stable MCP resource URIs and citations.
- Real implementations of the NuGet portions of four MCP tools.

## Functional Requirements

### FR-1: Library resolution

`resolve_library` must rank matches using:

1. Exact package ID match.
2. Package ID prefix or token match.
3. Description, tag, and documentation matches.
4. Configured source and recommendation boosts.

Each match must include a stable library ID, display name, kind, description,
recommended version, and confidence score.

### FR-2: Version selection

The service must apply the resolution order defined in the main specification:

1. Exact requested version.
2. Version from supplied project context.
3. Configured recommended version.
4. Latest stable listed version.
5. Prerelease only when requested or configured.

The selected version and selection reason must be returned. Evidence from
different versions must not be silently combined.

### FR-3: Documentation query

`query_docs` must:

- Restrict results to the resolved package version.
- Search package-authored documentation, XML docs, and symbols.
- Rank exact symbols above prose.
- Apply source-quality and deprecation adjustments.
- Return concise evidence fragments and citations.
- Respect configured result and response-size limits.

The result status must be `ok`, `not_found`, or `insufficient_evidence`.

### FR-4: Symbol lookup

`get_symbol` must support:

- Fully qualified names.
- Type and member names.
- Partial names when the match is unambiguous.
- Optional target-framework filtering.

Results must include signature, documentation, containing assembly, package
version, target framework, related members, and citations.

Ambiguous queries must return candidate symbols rather than choosing silently.

### FR-5: Version listing

`list_versions` must return versions in descending semantic-version order and
identify:

- Listed state.
- Prerelease state.
- Deprecation state.
- Indexed state.
- Recommended version and reason.

### FR-6: Citations and resources

Every evidence item must include a stable URI such as:

```text
nuget://{feed}/{packageId}/{version}/readme
nuget://{feed}/{packageId}/{version}/symbol/{qualifiedName}
```

Exact indexed artifacts should be exposed as read-only MCP resources where
supported by the SDK.

### FR-7: Missing evidence

The tools must never create signatures, examples, or documentation not present
in the index. Unknown packages, versions, and symbols must produce explicit
machine-readable statuses.

## Non-Functional Requirements

- Exact symbol lookup p95 is below 300 ms on the target dataset.
- Documentation query p95 is below 1 second on the target dataset.
- Version listing p95 is below 200 ms.
- Default responses do not exceed 100 KB.
- Every query accepts cancellation and a configured timeout.
- Tool output is deterministic for an unchanged index.

## Deliverables

- Library and version resolver.
- FTS5 search and ranking service.
- Symbol query service.
- Citation factory and MCP resource handlers.
- Implemented `resolve_library`, `query_docs`, `get_symbol`, and
  `list_versions` tools.
- Contract snapshots and integration tests.

## Acceptance Criteria

1. A package resolves by exact ID.
2. A package resolves from a term in its description.
3. An exact requested version returns evidence only from that version.
4. The configured recommended version is selected when no version is given.
5. A public type and method return correct signatures and XML documentation.
6. A natural-language question returns ranked, cited evidence.
7. Unknown APIs return `not_found` or `insufficient_evidence`.
8. Ambiguous symbols return candidates.
9. Representative queries meet the performance targets.
10. `dotnet build` and `dotnet test` succeed.

## Out of Scope

- Repository-derived examples.
- Semantic/vector search.
- HTTP transport and user authentication.

## Exit Gate

Stage 4 may begin when an MCP client can reliably discover and use internal
NuGet packages from indexed evidence over stdio.
