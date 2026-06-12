namespace DevContextMcp.Indexer.Core.Models;

public sealed record DocumentationIndexData(
    string ContentHash,
    IReadOnlyList<ArtifactRecord> Artifacts,
    IReadOnlyList<DocumentChunkRecord> Documents);
