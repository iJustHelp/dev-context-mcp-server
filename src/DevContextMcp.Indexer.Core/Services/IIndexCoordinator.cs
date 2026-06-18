using DevContextMcp.Indexer.Core.Models;

namespace DevContextMcp.Indexer.Core.Services;

// Runs a full indexing pass across all configured sources and documentation.
public interface IIndexCoordinator
{
    Task<IndexRunResult> IndexAllAsync(CancellationToken cancellationToken);
}
