using DevContextMcp.Server.Core.Models.Context;

namespace DevContextMcp.Server.Core.Infrastructure;

/// <summary>
/// Reads the last-run indexing snapshot from the analytics database. Returns null when no run
/// has been recorded or the analytics database does not yet exist.
/// </summary>
public interface IIndexSnapshotReadStore
{
    Task<IndexSnapshot?> GetAsync(
        string databasePath,
        CancellationToken cancellationToken);
}
