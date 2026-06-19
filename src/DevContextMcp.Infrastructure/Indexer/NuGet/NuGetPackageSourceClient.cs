using DevContextMcp.Indexer.Core.Infrastructure;
using DevContextMcp.Indexer.Core.Models;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace DevContextMcp.Infrastructure.Indexer.NuGet;

/// <summary>
/// Discovers and downloads package versions from a NuGet V3 feed, applying selection and size limits.
/// </summary>
internal sealed class NuGetPackageSourceClient(
    INuGetSourceAuthenticationProvider authenticationProvider,
    IContentHasher contentHasher) : IPackageSourceClient
{
    private sealed class PackageVersionComparer :
        IEqualityComparer<(string PackageId, string Version)>
    {
        public static PackageVersionComparer Instance { get; } = new PackageVersionComparer();

        public bool Equals(
            (string PackageId, string Version) x,
            (string PackageId, string Version) y) =>
            string.Equals(x.PackageId, y.PackageId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(x.Version, y.Version, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode((string PackageId, string Version) obj) =>
            HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.PackageId),
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Version));
    }

    public async Task<IReadOnlyList<PackageVersionCandidate>> DiscoverAsync(
        IndexSourceDefinition source,
        CancellationToken cancellationToken)
    {
        var repository = CreateRepository(source);
        using var cache = new SourceCacheContext();
        var metadataResource = await repository.GetResourceAsync<PackageMetadataResource>(
            cancellationToken);
        var candidates = new List<PackageVersionCandidate>();

        foreach (var package in source.Packages
                     .OrderBy(item => item.PackageId, StringComparer.OrdinalIgnoreCase))
        {
            var metadata = await metadataResource.GetMetadataAsync(
                packageId: package.PackageId,
                includePrerelease: package.IncludePrerelease,
                includeUnlisted: package.IncludeUnlisted,
                sourceCacheContext: cache,
                log: NullLogger.Instance,
                token: cancellationToken);

            var selectedMetadata = metadata
                .Where(item => package.IncludePrerelease || !item.Identity.Version.IsPrerelease)
                .Where(item => package.IncludeUnlisted || item.IsListed)
                .OrderByDescending(item => item.Identity.Version, VersionComparer.VersionRelease)
                .Take(package.MaxVersions)
                .ToArray();

            foreach (var item in selectedMetadata)
            {
                var deprecation = await item.GetDeprecationMetadataAsync();
                candidates.Add(new PackageVersionCandidate(
                    PackageId: item.Identity.Id,
                    Version: item.Identity.Version.ToNormalizedString(),
                    IsListed: item.IsListed,
                    IsDeprecated: deprecation is not null,
                    PublishedAt: item.Published));
            }
        }

        return candidates
            .GroupBy(
                candidate => (candidate.PackageId, candidate.Version),
                PackageVersionComparer.Instance)
            .Select(group => group.First())
            .ToArray();
    }

    public async Task<DownloadedPackage> DownloadAsync(
        IndexSourceDefinition source,
        PackageVersionCandidate package,
        PackageProcessingLimits limits,
        CancellationToken cancellationToken)
    {
        var repository = CreateRepository(source);
        var resource = await repository.GetResourceAsync<FindPackageByIdResource>(
            cancellationToken);
        using var cache = new SourceCacheContext();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(limits.PackageDownloadTimeout);

        var tempPath = Path.Combine(
            Path.GetTempPath(),
            $"mcp-doc-server-{Guid.NewGuid():N}.nupkg");

        try
        {
            long length;
            await using (var file = new FileStream(
                             path: tempPath,
                             mode: FileMode.CreateNew,
                             access: FileAccess.Write,
                             share: FileShare.None,
                             bufferSize: 81_920,
                             options: FileOptions.Asynchronous | FileOptions.SequentialScan))
            await using (var bounded = new LengthLimitedStream(file, limits.MaxPackageBytes))
            {
                var copied = await resource.CopyNupkgToStreamAsync(
                    id: package.PackageId,
                    version: NuGetVersion.Parse(package.Version),
                    destination: bounded,
                    cacheContext: cache,
                    logger: NullLogger.Instance,
                    cancellationToken: timeout.Token);

                if (!copied)
                {
                    throw new InvalidDataException(
                        $"NuGet source did not return {package.PackageId} {package.Version}.");
                }

                await bounded.FlushAsync(timeout.Token);
                length = bounded.Length;
            }

            await using var readStream = new FileStream(
                path: tempPath,
                mode: FileMode.Open,
                access: FileAccess.Read,
                share: FileShare.Read,
                bufferSize: 81_920,
                options: FileOptions.Asynchronous | FileOptions.SequentialScan);
            var hash = await contentHasher.HashAsync(readStream, timeout.Token);
            return new DownloadedPackage(tempPath, hash, length);
        }
        catch
        {
            File.Delete(tempPath);
            throw;
        }
    }

    private SourceRepository CreateRepository(IndexSourceDefinition source)
    {
        var packageSource = new PackageSource(source.ServiceIndex, source.Name);
        authenticationProvider.Configure(packageSource, source.Name);
        return Repository.Factory.GetCoreV3(packageSource);
    }
}
