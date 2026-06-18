namespace DevContextMcp.Indexer.Core.Models;

// A single non-document file extracted from a package, with its content hash and size.
public sealed record ArtifactRecord(
    string Path,
    string Kind,
    string ContentHash,
    long Size,
    string? Content = null);
