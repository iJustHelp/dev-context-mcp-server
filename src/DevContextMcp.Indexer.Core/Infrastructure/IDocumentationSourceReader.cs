using DevContextMcp.Indexer.Core.Models;

namespace DevContextMcp.Indexer.Core.Infrastructure;

public interface IDocumentationSourceReader
{
    Task<DocumentationIndexData> ReadAsync(
        DocumentationSourceDefinition source,
        PackageProcessingLimits limits,
        CancellationToken cancellationToken);
}
