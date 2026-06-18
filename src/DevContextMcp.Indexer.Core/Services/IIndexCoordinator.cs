using DevContextMcp.Indexer.Core.Models;

namespace DevContextMcp.Indexer.Core.Services;

/// <summary>
/// Runs a full indexing pass across all configured sources and documentation.
/// </summary>
public interface IIndexCoordinator
{
    Task<IndexRunResult> IndexAllAsync(CancellationToken cancellationToken);
}
