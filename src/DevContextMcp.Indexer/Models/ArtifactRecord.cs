namespace DevContextMcp.Indexer.Models;

public sealed record ArtifactRecord(
    string Path,
    string Kind,
    string ContentHash,
    long Size);
