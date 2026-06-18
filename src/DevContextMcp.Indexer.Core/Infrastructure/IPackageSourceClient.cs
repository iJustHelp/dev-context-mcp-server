using DevContextMcp.Indexer.Core.Models;

namespace DevContextMcp.Indexer.Core.Infrastructure;

// Discovers and downloads package versions from a configured NuGet source.
public interface IPackageSourceClient
{
    Task<IReadOnlyList<PackageVersionCandidate>> DiscoverAsync(
        IndexSourceDefinition source,
        CancellationToken cancellationToken);

    Task<DownloadedPackage> DownloadAsync(
        IndexSourceDefinition source,
        PackageVersionCandidate package,
        PackageProcessingLimits limits,
        CancellationToken cancellationToken);
}
