namespace DevContextMcp.Server.Core.Models.Context;

/// <summary>
/// The most recent indexing run's per-package outcome. Replaced on every run; holds one run only.
/// </summary>
public sealed record IndexSnapshot(
    DateTimeOffset GeneratedAt,
    string Status,
    IReadOnlyList<IndexSnapshotPackage> Packages);

/// <summary>
/// A single package's outcome in the last indexing run.
/// </summary>
public sealed record IndexSnapshotPackage(
    string PackageId,
    string Environment,
    int AvailableVersions,
    IReadOnlyList<string> IndexedVersions,
    string Status,
    string? Error);
