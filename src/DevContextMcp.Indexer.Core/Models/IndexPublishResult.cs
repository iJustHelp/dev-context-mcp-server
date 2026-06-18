namespace DevContextMcp.Indexer.Core.Models;

// The outcome of publishing a source to the index store, counting changed and unchanged packages.
public sealed record IndexPublishResult(
    int Changed,
    int Unchanged,
    IReadOnlyList<PackageIdentityKey> Added,
    IReadOnlyList<PackageIdentityKey> Updated,
    IReadOnlyList<PackageIdentityKey> Deleted);
