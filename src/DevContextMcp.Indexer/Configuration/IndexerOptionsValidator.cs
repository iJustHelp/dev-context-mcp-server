using DevContextMcp.Indexer.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NuGet.Versioning;
using System.Text.RegularExpressions;

namespace DevContextMcp.Indexer.Configuration;

/// <summary>
/// Validates Indexer configuration before indexing starts.
/// </summary>
public sealed class IndexerOptionsValidator :
    IValidateOptions<IndexerOptions>
{
    private sealed class EnvironmentPackageComparer :
        IEqualityComparer<(string Environment, string PackageId)>
    {
        public static EnvironmentPackageComparer Instance { get; } = new EnvironmentPackageComparer();

        public bool Equals(
            (string Environment, string PackageId) x,
            (string Environment, string PackageId) y) =>
            string.Equals(x.Environment, y.Environment, StringComparison.OrdinalIgnoreCase)
            && string.Equals(x.PackageId, y.PackageId, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode((string Environment, string PackageId) obj) =>
            HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Environment),
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.PackageId));
    }

    private static readonly Regex EnvironmentPattern = new Regex(
        "^[A-Za-z0-9._-]+$",
        RegexOptions.CultureInvariant);

    private readonly INuGetPackageOptionsLoader _packageOptionsLoader;
    private readonly IConfiguration? _configuration;

    public IndexerOptionsValidator()
        : this(new NuGetPackageOptionsLoader(), null)
    {
    }

    internal IndexerOptionsValidator(
        INuGetPackageOptionsLoader packageOptionsLoader,
        IConfiguration? configuration = null)
    {
        _packageOptionsLoader = packageOptionsLoader;
        _configuration = configuration;
    }

    public ValidateOptionsResult Validate(string? name, IndexerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();
        ConfigurationValidation.ValidatePath(
            options.DatabasePath,
            "DevContextMcp:DatabasePath",
            failures);
        ValidateObsoleteConfiguration(failures);
        ValidateLimits(options.Indexing, failures);
        ValidateSourceNames(options.NugetPackages, failures);
        ValidateSources(options.NugetPackages, failures);

        if (options.NugetPackages.Count > 0)
        {
            ConfigurationValidation.ValidatePath(
                options.IndexerSource.NugetsPath,
                "DevContextMcp:IndexerSource:NugetsPath",
                failures);
            if (!string.IsNullOrWhiteSpace(options.IndexerSource.NugetsPath))
            {
                ValidatePackages(options, failures);
            }
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private void ValidateObsoleteConfiguration(List<string> failures)
    {
        if (_configuration is null)
        {
            return;
        }

        var root = _configuration.GetSection(IndexerOptions.SectionName);
        foreach (var obsoleteKey in new[] { "NugetsPath", "Documentation", "Environments" })
        {
            if (root.GetSection(obsoleteKey).Exists())
            {
                failures.Add(
                    $"DevContextMcp:{obsoleteKey} is obsolete; use the new IndexerSource or NugetPackages configuration.");
            }
        }
    }

    private static void ValidateLimits(IndexingOptions options, List<string> failures)
    {
        if (options.MaxPackageBytes <= 0)
        {
            failures.Add("DevContextMcp:Indexing:MaxPackageBytes must be positive.");
        }

        if (options.MaxDocumentBytes <= 0)
        {
            failures.Add("DevContextMcp:Indexing:MaxDocumentBytes must be positive.");
        }

        if (options.MaxArchiveEntries <= 0)
        {
            failures.Add("DevContextMcp:Indexing:MaxArchiveEntries must be positive.");
        }

        if (options.MaxExtractedBytes <= 0)
        {
            failures.Add("DevContextMcp:Indexing:MaxExtractedBytes must be positive.");
        }

        if (!double.IsFinite(options.MaxCompressionRatio) || options.MaxCompressionRatio <= 0)
        {
            failures.Add("DevContextMcp:Indexing:MaxCompressionRatio must be positive.");
        }

        if (options.MaxDocumentChars <= 0)
        {
            failures.Add("DevContextMcp:Indexing:MaxDocumentChars must be positive.");
        }

        if (options.PackageDownloadTimeout <= TimeSpan.Zero)
        {
            failures.Add("DevContextMcp:Indexing:PackageDownloadTimeout must be positive.");
        }
    }

    private static void ValidateSourceNames(
        IReadOnlyList<NuGetPackageSourceOptions> sources,
        List<string> failures)
    {
        var names = sources.Select(source => source.Name)
            .ToList();

        foreach (var name in names.Where(string.IsNullOrWhiteSpace))
        {
            failures.Add("Every configured source must have a non-empty name.");
        }

        var duplicates = names
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .GroupBy(name => name, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key);

        foreach (var duplicate in duplicates)
        {
            failures.Add($"Source name '{duplicate}' is configured more than once.");
        }
    }

    private static void ValidateSources(
        IEnumerable<NuGetPackageSourceOptions> sources,
        List<string> failures)
    {
        foreach (var source in sources)
        {
            if (!IsSlug(source.Name))
            {
                failures.Add(
                    $"NuGet source Name '{source.Name}' must contain only letters, numbers, '.', '_', or '-'.");
            }

            if (!IsSlug(source.Environment))
            {
                failures.Add(
                    $"NuGet source '{source.Name}' Environment '{source.Environment}' must contain only letters, numbers, '.', '_', or '-'.");
            }

            if (!ConfigurationValidation.IsNuGetSource(source.ServiceIndex))
            {
                failures.Add(
                    $"NuGet source '{source.Name}' must have an absolute HTTP/HTTPS ServiceIndex URI or a valid local path.");
            }

            if (source.MaxPackages <= 0)
            {
                failures.Add($"NuGet source '{source.Name}' MaxPackages must be positive.");
            }
        }
    }

    private void ValidatePackages(IndexerOptions options, List<string> failures)
    {
        IReadOnlyList<NuGetPackageOptions> packages;
        try
        {
            packages = _packageOptionsLoader.Load(options.IndexerSource.NugetsPath);
        }
        catch (Exception exception)
        {
            failures.Add(exception.Message);
            return;
        }

        foreach (var package in packages)
        {
            if (!IsSlug(package.Environment))
            {
                failures.Add(
                    $"NuGet package '{package.PackageId}' Environment must contain only letters, numbers, '.', '_', or '-'.");
            }

            if (string.IsNullOrWhiteSpace(package.PackageId))
            {
                failures.Add("Every NuGet package configuration must have a non-empty PackageId.");
            }

            ValidateExplicitVersions(package, failures);
        }

        var duplicates = packages
            .Where(package =>
                !string.IsNullOrWhiteSpace(package.Environment)
                && !string.IsNullOrWhiteSpace(package.PackageId))
            .GroupBy(
                package => (package.Environment, package.PackageId),
                EnvironmentPackageComparer.Instance)
            .Where(group => group.Count() > 1);

        foreach (var duplicate in duplicates)
        {
            failures.Add(
                $"NuGet package '{duplicate.Key.PackageId}' is configured more than once in environment '{duplicate.Key.Environment}'.");
        }

        var configuredEnvironments = options.NugetPackages
            .Select(source => source.Environment)
            .Where(environment => !string.IsNullOrWhiteSpace(environment))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var package in packages.Where(package =>
                     !string.IsNullOrWhiteSpace(package.Environment)
                     && !configuredEnvironments.Contains(package.Environment)))
        {
            failures.Add(
                $"NuGet package '{package.PackageId}' references undefined environment '{package.Environment}'.");
        }

        foreach (var source in options.NugetPackages)
        {
            var count = packages.Count(package =>
                string.Equals(
                    package.Environment,
                    source.Environment,
                    StringComparison.OrdinalIgnoreCase));
            if (source.MaxPackages > 0 && count > source.MaxPackages)
            {
                failures.Add(
                    $"NuGet source '{source.Name}' matches {count} package configurations, exceeding MaxPackages {source.MaxPackages}.");
            }
        }
    }

    private static bool IsSlug(string value) =>
        !string.IsNullOrWhiteSpace(value)
        && EnvironmentPattern.IsMatch(value);

    private static void ValidateExplicitVersions(
        NuGetPackageOptions package,
        List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(package.Versions))
        {
            return;
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var version in package.Versions.Split(',', StringSplitOptions.TrimEntries))
        {
            if (version.Length == 0)
            {
                failures.Add(
                    $"NuGet package '{package.PackageId}' Versions contains an empty version.");
                continue;
            }

            string normalized;
            if (MinorVersionWildcard.TryParse(version, out var major, out var minor))
            {
                normalized = $"{major}.{minor}.*";
            }
            else if (NuGetVersion.TryParse(version, out var parsed))
            {
                normalized = parsed.ToNormalizedString();
            }
            else
            {
                failures.Add(
                    $"NuGet package '{package.PackageId}' Versions contains invalid version '{version}'.");
                continue;
            }

            if (!seen.Add(normalized))
            {
                failures.Add(
                    $"NuGet package '{package.PackageId}' Versions contains duplicate version '{version}'.");
            }
        }
    }
}
