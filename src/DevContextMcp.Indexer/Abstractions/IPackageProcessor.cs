using DevContextMcp.Indexer.Models;

namespace DevContextMcp.Indexer.Abstractions;

public interface IPackageProcessor
{
    Task<PackageIndexData> ProcessAsync(
        PackageVersionCandidate candidate,
        DownloadedPackage package,
        PackageProcessingLimits limits,
        CancellationToken cancellationToken);
}
