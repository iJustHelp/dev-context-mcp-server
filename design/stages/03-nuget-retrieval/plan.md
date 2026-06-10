# Stage 3 Implementation Plan: NuGet Retrieval

## Objective

Expose the Stage 2 SQLite index through deterministic, version-aware MCP tools
and read-only resources.

## Public Surface

Implement exactly:

- `resolve_library`
- `query_docs`
- `get_symbol`
- `list_versions`

Generated API-client packages use these same tools. Their client classes,
methods, request models, and response models are ordinary indexed symbols.

## Retrieval Components

### Library resolution

- Search exact package IDs, prefixes, metadata, and indexed text.
- Apply source-order and configured-recommendation boosts.
- Return stable `nuget:{environment}/{packageId}` identifiers and confidence
  scores while accepting legacy `nuget:{packageId}` identifiers through
  configured environment and source precedence.
- Deduplicate package matches across sources deterministically.

### Version resolution

Apply this precedence:

1. Exact requested version.
2. Project-context version.
3. Configured recommended version.
4. Latest stable listed indexed version.
5. Prerelease only when explicitly enabled.

Return the selected version and reason. Never mix evidence across versions.

### Documentation retrieval

- Query FTS5 only within the selected library version.
- Combine package-authored artifacts, XML documentation, and symbol evidence.
- Rank exact symbols above prose.
- Apply evidence thresholds, maximum-result limits, response budgets, and
  configured query timeout.
- Return `ok`, `not_found`, or `insufficient_evidence`.

### Symbol retrieval

- Support fully qualified, simple, and partial names.
- Filter by target framework when provided.
- Return one exact/unambiguous symbol or a deterministic candidate list.
- Include signature, documentation, assembly, frameworks, related members, and
  citation.

### Version listing

- Sort with semantic-version ordering.
- Report listed, prerelease, deprecated, indexed, and publication state.
- Report the recommended version and selection reason.

## Citations And Resources

Create stable citation URIs:

```text
nuget://{source}/{packageId}/{version}/artifact/{path}
nuget://{source}/{packageId}/{version}/symbol/{qualifiedName}
```

Register resource templates for both forms. Resource reads use only the local
index and enforce configured response limits.

## Implementation Sequence

1. Add read-store queries for library, version, document, symbol, and resource
   retrieval.
2. Implement semantic version resolution and library-ID parsing.
3. Implement response budgeting and citation creation.
4. Implement the four retrieval handlers.
5. Register the four MCP tool classes.
6. Register artifact and symbol resource templates.
7. Add fixture packages with multiple stable and prerelease versions.
8. Add handler, MCP transport, resource, error, and determinism tests.
9. Document Inspector workflows and generated-client package expectations.

## Tests

- Exact and descriptive package resolution.
- Recommendation, project version, requested version, latest stable, and
  prerelease precedence.
- Descending semantic version listing.
- Version-isolated documentation evidence.
- Exact, partial, ambiguous, framework-filtered, and missing symbol lookup.
- Stable artifact and symbol citations.
- Local MCP resource reads.
- Missing index, unknown package/version/symbol, timeout, and response-limit
  behavior.
- Deterministic ordering for an unchanged index.

## Completion Criteria

- A representative package resolves and lists versions over MCP.
- Documentation and symbols are retrieved from one selected version.
- Generated API-client package methods can be found as public symbols.
- Citations resolve through local resources.
- Unknown APIs are not invented.
- `dotnet build` and `dotnet test` pass.
