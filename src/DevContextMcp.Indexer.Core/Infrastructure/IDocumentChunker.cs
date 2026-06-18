using DevContextMcp.Indexer.Core.Models;

namespace DevContextMcp.Indexer.Core.Infrastructure;

// Splits document content into searchable chunks bounded by a maximum character count.
public interface IDocumentChunker
{
    IReadOnlyList<DocumentChunkRecord> Chunk(
        string path,
        string kind,
        string content,
        int maxCharacters);
}
