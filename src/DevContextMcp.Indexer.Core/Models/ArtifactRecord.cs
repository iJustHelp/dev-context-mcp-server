namespace DevContextMcp.Indexer.Core.Models;

/// <summary>
/// A single non-document file extracted from a package, with its content hash and size.
/// </summary>
public sealed record ArtifactRecord(
    string Path,
    string Kind,
    string ContentHash,
    long Size,
    string? Content = null);
