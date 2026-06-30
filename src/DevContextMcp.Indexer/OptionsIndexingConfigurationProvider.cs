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
                MinDocumentChars: limits.MinDocumentChars,
                PackageDownloadTimeout: limits.PackageDownloadTimeout),
            Sources: value.NugetPackages
                .Select(source => new IndexSourceDefinition(
                    Name: source.Name,
                    Environment: source.Environment,
                    ServiceIndex: ResolveSource(source.ServiceIndex),
                    Packages: packages
                        .Where(package => string.Equals(
                            package.Environment,
                            source.Environment,
                            StringComparison.OrdinalIgnoreCase))
                        .Select(package => new PackageSelectionDefinition(
                            PackageId: package.PackageId,
                            Versions: SplitVersions(package.Versions)))
                        .ToArray(),
                    MaxPackages: source.MaxPackages))
                .ToArray());
    }

    private static IReadOnlyList<string> SplitVersions(string? versions) =>
        string.IsNullOrWhiteSpace(versions)
            ? []
            : versions
                .Split(',', StringSplitOptions.TrimEntries)
                .Where(version => version.Length > 0)
                .ToArray();

    private static string ResolveSource(string source)
    {
        return Uri.TryCreate(source, UriKind.Absolute, out var uri)
            && uri.Scheme is "http" or "https"
                ? source
                : Path.GetFullPath(source);
    }
}
