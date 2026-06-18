using DevContextMcp.Indexer.Core.Models;

namespace DevContextMcp.Indexer.Core.Infrastructure;

// Reads a documentation source from disk into indexable artifacts and document chunks.
public interface IDocumentationSourceReader
{
    Task<DocumentationIndexData> ReadAsync(
        DocumentationSourceDefinition source,
        PackageProcessingLimits limits,
        CancellationToken cancellationToken);
}
