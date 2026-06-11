using DevContextMcp.Indexer.Configuration;
using DevContextMcp.Indexer.Core.Models;
using DevContextMcp.Indexer.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevContextMcp.Indexer;

internal sealed class IndexerRunner(
    IOptions<IndexerOptions> options,
    IIndexCoordinator indexCoordinator,
    ILogger<IndexerRunner> logger)
{
    public async Task<bool> RunAsync(CancellationToken cancellationToken)
    {
        if (options.Value.Environments.Count == 0)
        {
            logger.LogInformation("No NuGet environments are configured; indexing was skipped.");
            return true;
        }

        try
        {
            var summaries = await indexCoordinator.IndexAllAsync(cancellationToken);
            foreach (var summary in summaries)
            {
                LogSummary(summary);
            }

            return summaries.All(summary =>
                summary.Status.Equals("succeeded", StringComparison.Ordinal));
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

    private void LogSummary(IndexRunSummary summary)
    {
        var logLevel = summary.Status switch
        {
            "succeeded" => LogLevel.Information,
            "partial_success" => LogLevel.Warning,
            _ => LogLevel.Error
        };
        var report = FormatSummary(summary);

        logger.Log(logLevel, "{IndexerReport}", report);
    }

    private static string FormatSummary(IndexRunSummary summary) =>
        $@"
        Environment: {summary.SourceName}
        Status: {summary.Status}
        NuGets
            Total: {summary.Discovered}
            Indexed: {summary.Indexed}
            Errors: {summary.Errors.Count}
            Added ({summary.Added.Count}): {FormatPackages(summary.Added)}
            Updated ({summary.Updated.Count}): {FormatPackages(summary.Updated)}
            Deleted ({summary.Deleted.Count}):{FormatPackages(summary.Deleted)}
        ";

    private static string FormatPackages(
        IReadOnlyList<PackageIdentityKey> packages) =>
        packages.Count == 0
            ? ""
            : string.Join(
                "; ",
                packages
                    .OrderBy(package => package.PackageId, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(package => package.PackageId, StringComparer.Ordinal)
                    .ThenBy(package => package.Version, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(package => package.Version, StringComparer.Ordinal)
                    .Select(package => $"        {package.PackageId} {package.Version}"));
}
