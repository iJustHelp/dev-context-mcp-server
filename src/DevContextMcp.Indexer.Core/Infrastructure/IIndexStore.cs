using DevContextMcp.Indexer.Core.Models;

namespace DevContextMcp.Indexer.Core.Infrastructure;

/// <summary>
/// Persists indexed packages and reports what is currently indexed.
/// </summary>
public interface IIndexStore
{
    Task InitializeAsync(string databasePath, CancellationToken cancellationToken);

    Task<IReadOnlyList<IndexedLibrary>> GetIndexedLibrariesAsync(
        string databasePath,
        CancellationToken cancellationToken);

    Task<IndexPublishResult> PublishSourceAsync(
        string databasePath,
        IndexSourceDefinition source,
        DateTimeOffset startedAt,
        IReadOnlyList<PackageIndexData> packages,
        IReadOnlyList<IndexRunError> errors,
        bool pruneRemovedPackages,
        CancellationToken cancellationToken);
}
