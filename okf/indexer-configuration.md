---
type: Configuration
title: Indexer Configuration
description: appsettings.json sections and external NuGet package-policy files that drive DevContextMcp.Indexer.
resource: src/DevContextMcp.Indexer/appsettings.json
tags: [configuration, indexer, nuget, appsettings]
timestamp: 2026-07-15T00:00:00Z
---

# Indexer Configuration

Configuration for [DevContextMcp.Indexer](/projects/indexer.md) comes from two
places: the `DevContextMcp` section of `appsettings.json` (plus normal .NET
configuration overrides), and external per-package policy JSON files loaded from
`IndexerSource.NugetsPath` at startup.

Configuration is the source of truth: the package policy files present for an
environment define exactly what is indexed. See [Data Flows](/data-flows.md) for
how reconciliation prunes packages that are no longer configured.

# Schema

## appsettings.json — `DevContextMcp` section

| Key | Type | Notes |
|-----|------|-------|
| `DatabasePath` | string | Path to the SQLite index database written by the indexer. |
| `Analytics.DatabasePath` | string | Path to the host-owned analytics database. |
| `IndexerSource.NugetsPath` | string | Root folder containing per-package NuGet policy JSON files. |
| `NugetPackages[]` | array | Configured NuGet sources (feeds), one per environment. |
| `Indexing` | object | Safety and extraction limits for the indexing process. |

### `NugetPackages[]` — one entry per feed

| Key | Type | Notes |
|-----|------|-------|
| `Name` | string | Unique identifier for the NuGet source. |
| `Environment` | string | Environment slug used in library IDs and package selection (e.g. `public`, `prod`, `qa`). |
| `ServiceIndex` | string | NuGet v3 service endpoint URI, or a local folder path containing `.nupkg` files. |
| `MaxPackages` | int | Maximum number of package-policy entries applied to this source. |

### `Indexing` — limits

| Key | Type | Default | Notes |
|-----|------|---------|-------|
| `MaxPackageBytes` | int | `104857600` (100 MB) | Max size of a downloaded package archive. |
| `MaxDocumentBytes` | int | `20971520` (20 MB) | Max size of a documentation file. |
| `MaxArchiveEntries` | int | `10000` | Max entries allowed inside an archive. |
| `MaxExtractedBytes` | int | `524288000` (500 MB) | Max total bytes extracted during indexing. |
| `MaxCompressionRatio` | int | `200` | Max allowed compression ratio for archive entries. |
| `MaxDocumentChars` | int | `4000` | Max characters extracted from a document for indexing. |
| `PackageDownloadTimeout` | timespan | `00:02:00` | Max time to download a package. |

## Per-package NuGet policy files

Each indexed package has a JSON file under `IndexerSource.NugetsPath`.

| Key | Type | Notes |
|-----|------|-------|
| `Environment` | string | Must match an `Environment` in `NugetPackages` (e.g. `public`, `prod`, `qa`). |
| `PackageId` | string | Full NuGet package name. |
| `Versions` | string (optional) | Comma-separated version filter; each entry is a full version (`2.3.12`) or a minor wildcard `MAJOR.MINOR.*` (`2.4.*`). When omitted, every stable, listed version is eligible. |

### Version selection default

By default the indexer retains the two most recent major versions and, within
each, the two most recent minor versions — the highest stable patch of each
minor. For `3.3.x, 3.2.x, 3.1.x, 2.4.x, 2.3.x` it indexes the latest patch of
`3.3`, `3.2`, `2.4`, and `2.3`. Prerelease and unlisted versions are never
selected. A `Versions` filter restricts the eligible set before this window is
applied (so `2.4.*` indexes the highest stable patch of `2.4`).

## Logging

The indexer also configures a standard `Serilog` block (Console + File sinks;
default level `Information`, `Microsoft` overridden to `Warning`; daily rolling
file under `logs/indexer-.log`, retained 14 files, 10 MB size limit).

# Examples

`appsettings.json` (`DevContextMcp` section, checked-in demo configuration):

```json
"DevContextMcp": {
  "DatabasePath": "../../../../../database/docs.db",
  "Analytics": {
    "DatabasePath": "../../../../../database/analytics.db"
  },
  "IndexerSource": {
    "NugetsPath": "../../../../../demo/data/indexer/nugets"
  },
  "NugetPackages": [
    {
      "Name": "publicNuget",
      "Environment": "public",
      "ServiceIndex": "https://api.nuget.org/v3/index.json",
      "MaxPackages": 100
    },
    {
      "Name": "prodNuget",
      "Environment": "prod",
      "ServiceIndex": "../../../../../demo/data/nuget-repos/prod",
      "MaxPackages": 100
    },
    {
      "Name": "qaNuget",
      "Environment": "qa",
      "ServiceIndex": "../../../../../demo/data/nuget-repos/qa",
      "MaxPackages": 100
    }
  ],
  "Indexing": {
    "MaxPackageBytes": 104857600,
    "MaxDocumentBytes": 20971520,
    "MaxArchiveEntries": 10000,
    "MaxExtractedBytes": 524288000,
    "MaxCompressionRatio": 200,
    "MaxDocumentChars": 4000,
    "PackageDownloadTimeout": "00:02:00"
  }
}
```

A per-package policy file:

```json
{
  "Environment": "public",
  "PackageId": "Formula.SimpleRepo",
  "Versions": "3.2.*, 2.4.11"
}
```

Never put feed credentials or API tokens in package-policy files or the
checked-in settings; source authentication is isolated behind an infrastructure
interface.

See [Operations](/operations.md) to run the indexer and [Database Schema](/database-schema.md)
for what it writes.

# Citations

[1] [appsettings.json](../src/DevContextMcp.Indexer/appsettings.json)
[2] [Indexer configuration](../docs/indexer-configuration.md)
