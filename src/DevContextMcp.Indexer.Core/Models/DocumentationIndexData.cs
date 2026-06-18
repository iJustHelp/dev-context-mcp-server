namespace DevContextMcp.Indexer.Core.Models;

// The indexable result of reading a documentation source: its artifacts and chunked documents.
public sealed record DocumentationIndexData(
    string ContentHash,
    IReadOnlyList<ArtifactRecord> Artifacts,
    IReadOnlyList<DocumentChunkRecord> Documents);
