using DevContextMcp.Indexer.Core.Models;

namespace DevContextMcp.Indexer.Core.Infrastructure;

/// <summary>
/// Discovers and downloads package versions from a configured NuGet source.
/// </summary>
public interface IPackageSourceClient
{
    Task<PackageDiscovery> DiscoverAsync(
        IndexSourceDefinition source,
        CancellationToken cancellationToken);

    Task<DownloadedPackage> DownloadAsync(
        IndexSourceDefinition source,
        PackageVersionCandidate package,
        PackageProcessingLimits limits,
        CancellationToken cancellationToken);
}
