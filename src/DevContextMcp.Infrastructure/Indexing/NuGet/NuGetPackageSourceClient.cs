using DevContextMcp.Infrastructure.Indexing.Abstractions;
using DevContextMcp.Indexer.Abstractions;
using DevContextMcp.Indexer.Models;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace DevContextMcp.Infrastructure.Indexing.NuGet;

internal sealed class NuGetPackageSourceClient(
    INuGetSourceAuthenticationProvider authenticationProvider,
    IContentHasher contentHasher) : IPackageSourceClient
{
    private const int SearchPageSize = 100;

    public async Task<IReadOnlyList<PackageVersionCandidate>> DiscoverAsync(
        IndexSourceDefinition source,
        CancellationToken cancellationToken)
    {
        var repository = CreateRepository(source);
        using var cache = new SourceCacheContext();
        var packageIds = new HashSet<string>(
            source.PackageIds.Where(id => !string.IsNullOrWhiteSpace(id)),
            StringComparer.OrdinalIgnoreCase);

        if (source.PackagePrefixes.Count > 0)
        {
            var search = await repository.GetResourceAsync<PackageSearchResource>(
                cancellationToken);

            foreach (var prefix in source.PackagePrefixes)
            {
                for (var skip = 0; packageIds.Count < source.MaxPackages;)
                {
                    var results = (await search.SearchAsync(
                        prefix,
                        new SearchFilter(source.IncludePrerelease),
                        skip,
                        Math.Min(SearchPageSize, source.MaxPackages - packageIds.Count),
                        NullLogger.Instance,
                        cancellationToken)).ToArray();

                    foreach (var result in results)
                    {
                        if (result.Identity.Id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        {
                            packageIds.Add(result.Identity.Id);
                        }
                    }

                    if (results.Length < SearchPageSize)
                    {
                        break;
                    }

                    skip += results.Length;
                }
            }
        }

        var metadataResource = await repository.GetResourceAsync<PackageMetadataResource>(
            cancellationToken);
        var candidates = new List<PackageVersionCandidate>();

        foreach (var packageId in packageIds
                     .Order(StringComparer.OrdinalIgnoreCase)
                     .Take(source.MaxPackages))
        {
            var metadata = await metadataResource.GetMetadataAsync(
                packageId,
                source.IncludePrerelease,
                source.IncludeUnlisted,
                cache,
                NullLogger.Instance,
                cancellationToken);

            var selectedMetadata = metadata
                .Where(item => source.IncludePrerelease || !item.Identity.Version.IsPrerelease)
                .Where(item => source.IncludeUnlisted || item.IsListed)
                .OrderByDescending(item => item.Identity.Version, VersionComparer.VersionRelease)
                .Take(source.MaxVersionsPerPackage)
                .ToArray();

            foreach (var item in selectedMetadata)
            {
                var deprecation = await item.GetDeprecationMetadataAsync();
                candidates.Add(new PackageVersionCandidate(
                    item.Identity.Id,
                    item.Identity.Version.ToNormalizedString(),
                    item.IsListed,
                    deprecation is not null,
                    item.Published));
            }
        }

        return candidates;
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
                             tempPath,
                             FileMode.CreateNew,
                             FileAccess.Write,
                             FileShare.None,
                             81_920,
                             FileOptions.Asynchronous | FileOptions.SequentialScan))
            await using (var bounded = new LengthLimitedStream(file, limits.MaxPackageBytes))
            {
                var copied = await resource.CopyNupkgToStreamAsync(
                    package.PackageId,
                    NuGetVersion.Parse(package.Version),
                    bounded,
                    cache,
                    NullLogger.Instance,
                    timeout.Token);

                if (!copied)
                {
                    throw new InvalidDataException(
                        $"NuGet source did not return {package.PackageId} {package.Version}.");
                }

                await bounded.FlushAsync(timeout.Token);
                length = bounded.Length;
            }

            await using var readStream = new FileStream(
                tempPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                81_920,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
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
