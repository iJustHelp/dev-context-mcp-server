using DevContextMcp.Indexer.Configuration;
using DevContextMcp.Indexer.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevContextMcp.Indexer;

/// <summary>
/// Drives a single indexing run: indexes every configured source, publishes the run snapshot,
/// and logs the run report. Returns whether the run succeeded.
/// </summary>
internal sealed class IndexerRunner(
    IOptions<IndexerOptions> options,
    IIndexCoordinator indexCoordinator,
    IIndexRunSnapshotPublisher snapshotPublisher,
    ILogger<IndexerRunner> logger)
{
    public async Task<bool> RunAsync(CancellationToken cancellationToken)
    {
        if (options.Value.NugetPackages.Count == 0)
        {
            logger.LogInformation("No NuGet indexing sources are configured; indexing was skipped.");
            return true;
        }

        try
        {
            var result = await indexCoordinator.IndexAllAsync(cancellationToken);
            await snapshotPublisher.PublishAsync(result, cancellationToken);

            foreach (var entry in IndexRunReport.Build(result))
            {
                logger.Log(entry.Level, "{IndexerReport}", entry.Message);
            }

            return result.Succeeded;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "NuGet indexing failed.");
            return false;
        }
    }
}
