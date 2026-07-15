---
type: Reference
title: Security & Safety Model
description: How the indexer treats package archives as untrusted input — extraction limits, path-traversal guards, and metadata-only symbol reading.
resource: src/DevContextMcp.Infrastructure/Indexer/NuGet/ArchiveSafetyValidator.cs
tags: [security, safety, untrusted-input, archive, symbols]
timestamp: 2026-07-15T00:00:00Z
---

# Security & Safety Model

Package archives are treated as untrusted input. The indexer never loads or
executes package assemblies; it inspects them through metadata APIs and bounds
every extraction step. See [Data Flows](data-flows.md) for where these guards
sit in the indexing pipeline and [Indexer Configuration](indexer-configuration.md)
for the configured limit values.

# Schema

## Extraction limits — `PackageProcessingLimits`

A record threaded into processing and validated at startup:

| Limit | Meaning |
|-------|---------|
| `MaxPackageBytes` | Max size of a downloaded package archive. |
| `MaxDocumentBytes` | Max size of a single documentation file. |
| `MaxArchiveEntries` | Max entries allowed inside an archive. |
| `MaxExtractedBytes` | Max cumulative bytes extracted from an archive. |
| `MaxCompressionRatio` | Max per-entry uncompressed/compressed ratio. |
| `MaxDocumentChars` / `MinDocumentChars` | Character bounds for an indexed document. |
| `PackageDownloadTimeout` | Max time to download a package. |

## Archive safety — `ArchiveSafetyValidator`

Runs before extraction and enforces:

- **Entry count** — rejects archives with more than `MaxArchiveEntries`.
- **Cumulative size** — rejects once total entry length would exceed `MaxExtractedBytes`
  (also rejects negative entry lengths).
- **Compression ratio** — per entry, `Length / CompressedLength`; a zero
  compressed length is treated as infinite ratio; rejects above `MaxCompressionRatio`
  (zip-bomb guard).
- **Path validation** (`ValidatePath`) — rejects blank/null/control-character
  paths, rooted paths, a leading `/`, and any `..` segment (zip-slip guard).

## Bounded download — `LengthLimitedStream`

A write-only stream wrapper that throws once more than the allowed number of
bytes is written, capping download/extraction defensively.

## Metadata-only symbols — `MetadataSymbolExtractor`

Reads public types and members via `PEReader` / `MetadataReader`
(`System.Reflection.Metadata`) **without loading or executing** the assembly,
skipping `<Module>` and compiler-generated types.

`NuGetPackageProcessor` wires these together: it validates the archive before
extraction and uses metadata-only extraction for symbols.

# Notes

- SQLite publication is transactional: a failed package refresh preserves the
  last successfully indexed data.
- Do not put feed credentials or API tokens in package-policy files or the
  checked-in settings. Source authentication is isolated behind an infrastructure
  interface for future approved credential providers.

See the tests `ArchiveSafetyValidatorTests` and `MetadataSymbolExtractorTests`
in [Testing Strategy](testing-strategy.md).

# Citations

[1] [ArchiveSafetyValidator.cs](../src/DevContextMcp.Infrastructure/Indexer/NuGet/ArchiveSafetyValidator.cs)
[2] [PackageProcessingLimits.cs](../src/DevContextMcp.Indexer.Core/Models/PackageProcessingLimits.cs)
[3] [MetadataSymbolExtractor.cs](../src/DevContextMcp.Infrastructure/Indexer/NuGet/MetadataSymbolExtractor.cs)
[4] [NuGetPackageProcessor.cs](../src/DevContextMcp.Infrastructure/Indexer/NuGet/NuGetPackageProcessor.cs)
