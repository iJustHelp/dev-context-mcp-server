using DevContextMcp.Indexer.Configuration;
using DevContextMcp.Indexer.Core.Models;
using DevContextMcp.Server.Core.Infrastructure;
using DevContextMcp.Server.Core.Models.Context;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevContextMcp.Indexer;

/// <summary>
/// Publishes the last-run snapshot of an indexing run. Never fails the run.
/// </summary>
internal interface IIndexRunSnapshotPublisher
{
    Task PublishAsync(IndexRunResult result, CancellationToken cancellationToken);
}

/// <summary>
/// Projects an indexing run onto the last-run snapshot and replaces it in the analytics database.
/// A snapshot that cannot be written is logged as a warning and never fails the run.
/// </summary>
internal sealed class IndexRunSnapshotPublisher(
    IOptions<IndexerOptions> options,
    IIndexSnapshotWriteStore snapshotStore,
    ILogger<IndexRunSnapshotPublisher> logger) : IIndexRunSnapshotPublisher
{
    public async Task PublishAsync(
        IndexRunResult result,
        CancellationToken cancellationToken)
    {
        try
        {
            var databasePath = Path.GetFullPath(
                options.Value.Analytics.DatabasePath,
                AppContext.BaseDirectory);

            await snapshotStore.ReplaceAsync(
                databasePath,
                ToSnapshot(result),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to write the indexing snapshot.");
        }
    }

    private static IndexSnapshot ToSnapshot(IndexRunResult result) =>
        new IndexSnapshot(
            GeneratedAt: DateTimeOffset.UtcNow,
            Status: result.Status.ToPersistedValue(),
            Packages: result.Summaries
                .SelectMany(summary => summary.Packages ?? [])
                .Select(package => new IndexSnapshotPackage(
                    PackageId: package.PackageId,
                    Environment: package.Environment,
                    AvailableVersions: package.AvailableVersions,
                    IndexedVersions: package.IndexedVersions,
                    Status: package.Status,
                    Error: package.Error))
                .ToArray());
}
