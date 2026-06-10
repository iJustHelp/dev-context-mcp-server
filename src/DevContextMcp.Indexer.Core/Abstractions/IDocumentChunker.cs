using DevContextMcp.Indexer.Core.Models;

namespace DevContextMcp.Indexer.Core.Abstractions;

public interface IDocumentChunker
{
    IReadOnlyList<DocumentChunkRecord> Chunk(
        string path,
        string kind,
        string content,
        int maxCharacters);
}
