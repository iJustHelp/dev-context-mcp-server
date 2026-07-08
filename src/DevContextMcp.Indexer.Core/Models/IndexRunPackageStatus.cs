namespace DevContextMcp.Indexer.Core.Models;

/// <summary>
/// Per-package outcome of an indexing run, used to build the last-run snapshot:
/// how many versions the feed offered, which versions were indexed, the status, and any error.
/// </summary>
public sealed record IndexRunPackageStatus(
    string PackageId,
    string Environment,
    int AvailableVersions,
    IReadOnlyList<string> IndexedVersions,
    string Status,
    string? Error);

/// <summary>
/// Per-package status values for an indexing run.
/// </summary>
public static class IndexRunPackageStatusKind
{
    public const string Added = "added";
    public const string Updated = "updated";
    public const string Unchanged = "unchanged";
    public const string Deleted = "deleted";
    public const string Failed = "failed";
    public const string NotFound = "not_found";
}
