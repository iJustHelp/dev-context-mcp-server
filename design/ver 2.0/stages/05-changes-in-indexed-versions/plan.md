# NuGet Indexer Version Policy

## Summary
Change NuGet indexing version selection from "latest N versions" to a two-major default policy, with explicit per-package version pins available in package JSON.

No database schema change is required; existing indexed data does not need to be migrated.

Default behavior:
- `MaxVersionsPerPackage`: `2`
- Automatic indexing selects at most two stable/listed versions: the newest version from the newest major version, and the newest version from the next major version.

Explicit behavior:
- Users can set `Versions` as a comma-separated string, e.g. `"Versions": "2.1.0,2.0.5"`.
- Explicit `Versions` selects only the listed versions, including prerelease or unlisted versions if they are named directly.
- `IncludePrerelease` and `IncludeUnlisted` are removed from package policy and are no longer supported.
- If any explicit version is missing from the feed, fail indexing.

## Key Changes
- Extend NuGet package policy JSON/options with:
  - `Versions: string?`
- keep `MaxVersionsPerPackage`, changing default from `3` to `2`
- Remove `IncludePrerelease` and `IncludeUnlisted` from NuGet package policy options, fixtures, demo JSON, and validation.
- Remove `MaxMajorVersions` and `MaxMinorVersionsPerMajor` from NuGet package policy options, fixtures, demo JSON, and validation.
- Extend `PackageSelectionDefinition` to carry explicit versions and the total version limit.
- Update `NuGetPackageSourceClient.DiscoverAsync`:
  - Always fetch metadata with prerelease and unlisted metadata included so explicit versions can be found.
  - If `Versions` is non-empty: select exact requested versions and fail if any are unavailable.
  - Otherwise: consider only listed stable versions, sort by semantic version descending, take the newest version from each of the newest two major groups, capped by `MaxVersionsPerPackage`.
- Keep JSON property handling strict; malformed fields, invalid version strings, duplicate explicit versions, and non-positive version limits should fail validation.

## Public Interfaces / Config
Example default grouped policy:
```json
{
  "Environment": "qa",
  "PackageId": "Demo.Cities",
  "MaxVersionsPerPackage": 2
}
```

Example explicit policy:
```json
{
  "Environment": "qa",
  "PackageId": "Demo.Cities",
  "Versions": "1.1.0,1.0.0"
}
```

Example explicit prerelease policy:
```json
{
  "Environment": "qa",
  "PackageId": "Demo.Cities",
  "Versions": "2.0.0-beta.1"
}
```

No server API, MCP tool contract, OpenAPI, or UI contract changes are required.

## Test Plan
- Unit test grouped selection:
  - selects at most 2 versions by default
  - selects the newest version from the newest major version
  - selects the newest version from the next major version
  - excludes prerelease and unlisted versions unless explicitly named
- Unit test explicit selection:
  - comma-split `Versions` are selected exactly
  - explicit prerelease versions are selected when named
  - explicit unlisted versions are selected when named
  - missing explicit versions fail indexing
  - duplicate/invalid explicit versions fail validation
- Update existing configuration tests for new defaults and validation.
- Run `dotnet test` with alternate output path if normal bin files are locked.

## Assumptions
- "2 versions" means max 2 automatic versions: one from the newest major and one from the next major.
- Default grouped indexing is stable/listed only; prerelease and unlisted versions are indexed only when explicitly listed in `Versions`.
