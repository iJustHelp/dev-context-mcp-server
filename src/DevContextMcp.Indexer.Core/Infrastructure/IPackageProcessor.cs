using DevContextMcp.Indexer.Core.Models;

namespace DevContextMcp.Indexer.Core.Infrastructure;

/// <summary>
/// Processes a downloaded package into its fully indexable representation.
/// </summary>
public interface IPackageProcessor
{
    Task<PackageIndexData> ProcessAsync(
        PackageVersionCandidate candidate,
        DownloadedPackage package,
        PackageProcessingLimits limits,
        CancellationToken cancellationToken);
}
