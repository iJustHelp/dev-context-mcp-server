namespace DevContextMcp.Indexer.Core.Models;

/// <summary>
/// Per-source summary of an indexing run: timing, counts, and any errors encountered.
/// </summary>
public sealed record IndexRunSummary(
    string SourceName,
    string Status,
    string Environment,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt,
    int Discovered,
    int Indexed,
    int Changed,
    int Unchanged,
    IReadOnlyList<PackageIdentityKey> Added,
    IReadOnlyList<PackageIdentityKey> Updated,
    IReadOnlyList<PackageIdentityKey> Deleted,
    IReadOnlyList<IndexRunError> Errors);
