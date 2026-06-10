using DevContextMcp.Indexer.Models;

namespace DevContextMcp.Indexer.Services;

public interface IIndexCoordinator
{
    Task<IReadOnlyList<IndexRunSummary>> IndexAllAsync(CancellationToken cancellationToken);
}
