namespace DevContextMcp.Indexer.Core.Models;

/// <summary>
/// The indexable result of reading a documentation source: its artifacts and chunked documents.
/// </summary>
public sealed record DocumentationIndexData(
    string ContentHash,
    IReadOnlyList<ArtifactRecord> Artifacts,
    IReadOnlyList<DocumentChunkRecord> Documents);
