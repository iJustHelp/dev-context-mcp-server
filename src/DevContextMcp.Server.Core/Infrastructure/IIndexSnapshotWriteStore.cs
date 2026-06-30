using DevContextMcp.Server.Core.Models.Context;

namespace DevContextMcp.Server.Core.Infrastructure;

/// <summary>
/// Writes the last-run indexing snapshot into the self-creating analytics database. The indexer
/// owns writes; each call fully replaces the previous snapshot so only one run is ever stored.
/// </summary>
public interface IIndexSnapshotWriteStore
{
    Task ReplaceAsync(
        string databasePath,
        IndexSnapshot snapshot,
        CancellationToken cancellationToken);
}
