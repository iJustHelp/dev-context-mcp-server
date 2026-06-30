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
                includePrerelease: false,
                includeUnlisted: false,
                sourceCacheContext: cache,
                log: NullLogger.Instance,
                token: cancellationToken);

            var metadataArray = metadata.ToArray();
            foreach (var item in SelectVersions(package, metadataArray))
            {
                candidates.Add(await ToCandidateAsync(item));
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

    /// <summary>
    /// Determines which package versions to index. When the package configures explicit
    /// <see cref="PackageSelectionDefinition.Versions"/> entries (full versions or
    /// "MAJOR.MINOR.*" wildcards), those entries restrict the eligible set; otherwise every
    /// stable, listed version is eligible. The default version window is then applied to the
    /// eligible set.
    /// </summary>
    private static IReadOnlyList<IPackageSearchMetadata> SelectVersions(
        PackageSelectionDefinition package,
        IReadOnlyList<IPackageSearchMetadata> metadata)
    {
        var stableListed = metadata
            .Where(item => item.IsListed)
            .Where(item => !item.Identity.Version.IsPrerelease)
            .ToArray();

        var eligible = package.Versions is { Count: > 0 }
            ? FilterToConfiguredVersions(package, stableListed)
            : stableListed;

        var selectedVersions = SelectDefaultVersions(
                eligible.Select(item => item.Identity.Version))
            .ToHashSet(VersionComparer.VersionRelease);

        return eligible
            .Where(item => selectedVersions.Contains(item.Identity.Version))
            .ToArray();
    }

    private static IReadOnlyList<IPackageSearchMetadata> FilterToConfiguredVersions(
        PackageSelectionDefinition package,
        IReadOnlyList<IPackageSearchMetadata> stableListed)
    {
        var eligible = new List<IPackageSearchMetadata>();
        var missing = new List<string>();
        foreach (var entry in package.Versions ?? [])
        {
            var matches = MinorVersionWildcard.TryParse(entry, out var major, out var minor)
                ? stableListed
                    .Where(item => item.Identity.Version.Major == major
                        && item.Identity.Version.Minor == minor)
                    .ToArray()
                : stableListed
                    .Where(item => VersionComparer.VersionRelease.Equals(
                        item.Identity.Version,
                        NuGetVersion.Parse(entry)))
                    .ToArray();

            if (matches.Length == 0)
            {
                missing.Add(entry);
            }
            else
            {
                eligible.AddRange(matches);
            }
        }

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"NuGet package '{package.PackageId}' explicit versions were not found: {string.Join(", ", missing)}.");
        }

        return eligible
            .DistinctBy(item => item.Identity.Version, VersionComparer.VersionRelease)
            .ToArray();
    }

    private static async Task<PackageVersionCandidate> ToCandidateAsync(IPackageSearchMetadata item)
    {
        var deprecation = await item.GetDeprecationMetadataAsync();
        return new PackageVersionCandidate(
            PackageId: item.Identity.Id,
            Version: item.Identity.Version.ToNormalizedString(),
            IsListed: item.IsListed,
            IsDeprecated: deprecation is not null,
            PublishedAt: item.Published);
    }

    /// <summary>
    /// Selects the default version window: the two most recent major versions and, within each,
    /// the two most recent minor versions (highest stable patch of each minor).
    /// </summary>
    internal static IReadOnlyList<NuGetVersion> SelectDefaultVersions(
        IEnumerable<NuGetVersion> versions)
    {
        return versions
            .GroupBy(version => version.Major)
            .OrderByDescending(group => group.Key)
            .Take(2)
            .SelectMany(majorGroup => majorGroup
                .GroupBy(version => version.Minor)
                .OrderByDescending(minorGroup => minorGroup.Key)
                .Take(2)
                .Select(minorGroup => minorGroup
                    .OrderByDescending(version => version, VersionComparer.VersionRelease)
                    .First()))
            .OrderByDescending(version => version, VersionComparer.VersionRelease)
            .ToArray();
    }

    private SourceRepository CreateRepository(IndexSourceDefinition source)
    {
        var packageSource = new PackageSource(source.ServiceIndex, source.Name);
        authenticationProvider.Configure(packageSource, source.Name);
        return Repository.Factory.GetCoreV3(packageSource);
    }
}
