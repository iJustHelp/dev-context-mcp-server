using DevContextMcp.Indexer.Core.Infrastructure;
using DevContextMcp.Indexer.Configuration;
using DevContextMcp.Indexer.Core.Models;
using Microsoft.Extensions.Options;

namespace DevContextMcp.Indexer;

/// <summary>
/// Builds resolved IndexingSettings from bound IndexerOptions and the external package policy files.
/// </summary>
internal sealed class OptionsIndexingConfigurationProvider(
    IOptions<IndexerOptions> options,
    INuGetPackageOptionsLoader packageOptionsLoader) : IIndexingConfigurationProvider
{
    public IndexingSettings GetSettings()
    {
        var value = options.Value;
        var limits = value.Indexing;
        var packages = value.NugetPackages.Count == 0
            ? []
            : packageOptionsLoader.Load(value.IndexerSource.NugetsPath);

        return new IndexingSettings(
            DatabasePath: Path.GetFullPath(value.DatabasePath, AppContext.BaseDirectory),
            Limits: new PackageProcessingLimits(
                MaxPackageBytes: limits.MaxPackageBytes,
                MaxDocumentBytes: limits.MaxDocumentBytes,
                MaxArchiveEntries: limits.MaxArchiveEntries,
                MaxExtractedBytes: limits.MaxExtractedBytes,
                MaxCompressionRatio: limits.MaxCompressionRatio,
                MaxDocumentChars: limits.MaxDocumentChars,
                PackageDownloadTimeout: limits.PackageDownloadTimeout),
            Sources: value.NugetPackages
                .Select(source => new
                {
                    Source = source,
                    Packages = packages
                        .Where(package => string.Equals(
                            package.Environment,
                            source.Environment,
                            StringComparison.OrdinalIgnoreCase))
                        .Where(package => !package.Delete)
                        .Select(package => new PackageSelectionDefinition(
                            PackageId: package.PackageId,
                            IncludePrerelease: package.IncludePrerelease,
                            IncludeUnlisted: package.IncludeUnlisted,
                            MaxVersions: package.MaxVersionsPerPackage))
                        .ToArray(),
                    DeletedPackageIds = packages
                        .Where(package => string.Equals(
                            package.Environment,
                            source.Environment,
                            StringComparison.OrdinalIgnoreCase))
                        .Where(package => package.Delete)
                        .Select(package => package.PackageId)
                        .ToArray()
                })
                .Where(item =>
                    item.Packages.Length > 0
                    || item.DeletedPackageIds.Length > 0)
                .Select(item => new IndexSourceDefinition(
                    Name: item.Source.Name,
                    Environment: item.Source.Environment,
                    ServiceIndex: ResolveSource(item.Source.ServiceIndex),
                    Packages: item.Packages,
                    DeletedPackageIds: item.DeletedPackageIds,
                    MaxPackages: item.Source.MaxPackages))
                .ToArray(),
            Documentation: value.IndexerSource.Documents is null
                ? null
                : new DocumentationSourceDefinition(
                    Path.GetFullPath(
                        value.IndexerSource.Documents.RootPath,
                        AppContext.BaseDirectory),
                    value.IndexerSource.Documents.Extensions
                        .Select(NormalizeExtension)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase)));
    }

    private static string NormalizeExtension(string extension) =>
        extension.Trim().StartsWith('.')
            ? extension.Trim()
            : $".{extension.Trim()}";

    private static string ResolveSource(string source)
    {
        return Uri.TryCreate(source, UriKind.Absolute, out var uri)
            && uri.Scheme is "http" or "https"
                ? source
                : Path.GetFullPath(source);
    }
}
