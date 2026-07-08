# Version Configuration BRD: Default Version Retention

## Purpose

Replace the per-package `MaxVersionsPerPackage` setting with a fixed default
version-retention window, and extend the explicit `Versions` override to support
minor-level wildcards.

## Business Outcome

Each package keeps a useful, predictable window of stable versions with no
per-package tuning, while teams can still pin or widen coverage through the
`Versions` override when needed.

## Current Behavior

`SelectDefaultVersions` in
[NuGetPackageSourceClient.cs](../../../src/DevContextMcp.Infrastructure/Indexer/NuGet/NuGetPackageSourceClient.cs)
keeps the two most recent **major** versions but only the single latest version
within each major, then truncates to `MaxVersionsPerPackage` (default `2`).

`MaxVersionsPerPackage` and a comma-separated explicit `Versions` allowlist are
configured per package in
[NuGetPackageOptions.cs](../../../src/DevContextMcp.Indexer/Configuration/NuGetPackageOptions.cs).
Explicit versions are matched **exactly** today
([NuGetPackageSourceClient.cs](../../../src/DevContextMcp.Infrastructure/Indexer/NuGet/NuGetPackageSourceClient.cs));
wildcards are not supported.

## In Scope

- Remove `MaxVersionsPerPackage` from configuration and the selection pipeline.
- Define a fixed default version window (2 majors × 2 minors).
- Extend the `Versions` override to accept minor wildcards (`2.4.*`).
- Update configuration validation and documentation.

## Functional Requirements

### FR-1: Remove `MaxVersionsPerPackage`

`MaxVersionsPerPackage` must be removed from `NuGetPackageOptions`, from
`PackageSelectionDefinition.MaxVersions`, from validation, and from the default
selection logic. Configuration files must no longer set it.

### FR-2: Default version window

When a package has no `Versions` override, the indexer must retain, by default:

- The **2 most recent major** versions, and
- The **2 most recent minor** versions within each retained major.

Only stable, listed versions are eligible, compared with NuGet
semantic-version rules. For `3.3, 3.2, 3.1, 2.4, 2.3, 2.2` the indexer retains
`3.3, 3.2, 2.4, 2.3`. The window is fixed and not configurable.

### FR-3: `Versions` override with wildcards

When `Versions` is set, it restricts the eligible version set; the default
window (FR-2) is then applied to that eligible set. Each entry may be:

- A **minor wildcard** `MAJOR.MINOR.*` — makes every stable patch of that minor
  eligible. Because the window keeps the highest patch of a minor, `2.4.*`
  resolves to the latest `2.4.x` (such as `2.4.11`).
- A **full version** `MAJOR.MINOR.PATCH` — makes that exact version eligible
  (e.g. `2.3.12`).

Entries remain comma-separated. A wildcard or exact entry that matches no
stable, listed version must surface as a configuration/index error (consistent
with today's "explicit versions were not found" behavior). Eligible versions
beyond the two-major/two-minor window are not indexed even when listed.

### FR-4: Validation and documentation

Validation must reject malformed `Versions` entries (neither a valid full
version nor a `MAJOR.MINOR.*` wildcard). Configuration documentation in
[docs/indexer-configuration.md](../../../docs/indexer-configuration.md) must
describe the fixed default window and the wildcard override, and remove
`MaxVersionsPerPackage`.

## Non-Functional Requirements

- Selection is deterministic for an unchanged feed.
- A package with fewer majors or minors than the window returns all eligible
  stable, listed versions without error.
- Prerelease and unlisted versions remain excluded from the default window and
  from wildcard resolution.

## Deliverables

- `NuGetPackageOptions` and `PackageSelectionDefinition` without `MaxVersions`.
- Updated `SelectDefaultVersions` (2 majors × 2 minors).
- Wildcard-aware explicit `Versions` resolution.
- Updated validation and `docs/indexer-configuration.md`.
- Unit tests for the window, wildcard resolution, exact pinning, sparse-version
  packages, and unmatched-version errors.

## Acceptance Criteria

1. The default selection returns the 2 latest minors of each of the 2 latest
   majors; `3.3, 3.2, 2.4, 2.3` is produced for the documented input.
2. `2.4.*` resolves to the highest stable `2.4.x`; `2.3.12` selects that exact
   version.
3. `Versions` restricts eligibility, then the window applies: a list spanning
   more than two minors of a major keeps only the two newest of those minors.
4. An unmatched wildcard or exact version fails with a clear error.
5. No configuration or code references `MaxVersionsPerPackage`.
6. `dotnet build` and `dotnet test` succeed.

## Out of Scope

- Retrieval-side version resolution and recommendation logic.
- Prerelease or unlisted retention policy changes.
- Major-level wildcards (e.g. `3.*`) or ranges.
