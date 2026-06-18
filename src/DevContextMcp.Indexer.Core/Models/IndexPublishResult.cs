namespace DevContextMcp.Indexer.Core.Models;

/// <summary>
/// The outcome of publishing a source to the index store, counting changed and unchanged packages.
/// </summary>
public sealed record IndexPublishResult(
    int Changed,
    int Unchanged,
    IReadOnlyList<PackageIdentityKey> Added,
    IReadOnlyList<PackageIdentityKey> Updated,
    IReadOnlyList<PackageIdentityKey> Deleted);
