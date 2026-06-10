using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace DevContextMcp.Indexer.Cli.Configuration;

/// <summary>
/// Validates Indexer configuration before indexing starts.
/// </summary>
public sealed class IndexerOptionsValidator :
    IValidateOptions<IndexerOptions>
{
    private static readonly Regex EnvironmentPattern = new(
        "^[A-Za-z0-9._-]+$",
        RegexOptions.CultureInvariant);

    public ValidateOptionsResult Validate(string? name, IndexerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();
        ConfigurationValidation.ValidatePath(
            options.DatabasePath,
            "DevContextMcp:DatabasePath",
            failures);
        ValidateLimits(options.Indexing, failures);
        ValidateSourceNames(options, failures);
        ValidateNuGetSources(options.NuGetSources, failures);
        ValidateRepositorySources(options.RepositorySources, failures);

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
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
        IndexerOptions options,
        List<string> failures)
    {
        var names = options.NuGetSources.Select(source => source.Name)
            .Concat(options.RepositorySources.Select(source => source.Name))
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

    private static void ValidateNuGetSources(
        IEnumerable<NuGetSourceOptions> sources,
        List<string> failures)
    {
        foreach (var source in sources)
        {
            if (!IsEnvironment(source.Environment))
            {
                failures.Add(
                    $"NuGet source '{source.Name}' Environment must contain only letters, numbers, '.', '_', or '-'.");
            }

            if (!ConfigurationValidation.IsNuGetSource(source.ServiceIndex))
            {
                failures.Add(
                    $"NuGet source '{source.Name}' must have an absolute HTTP/HTTPS ServiceIndex URI or a valid local path.");
            }

            if (source.PackagePrefixes.Any(string.IsNullOrWhiteSpace))
            {
                failures.Add($"NuGet source '{source.Name}' contains an empty package prefix.");
            }

            if (source.PackageIds.Any(string.IsNullOrWhiteSpace))
            {
                failures.Add($"NuGet source '{source.Name}' contains an empty package ID.");
            }

            if (source.PackagePrefixes.Count == 0 && source.PackageIds.Count == 0)
            {
                failures.Add(
                    $"NuGet source '{source.Name}' must configure at least one package prefix or package ID.");
            }

            if (source.MaxVersionsPerPackage <= 0)
            {
                failures.Add(
                    $"NuGet source '{source.Name}' MaxVersionsPerPackage must be positive.");
            }

            if (source.MaxPackages <= 0)
            {
                failures.Add($"NuGet source '{source.Name}' MaxPackages must be positive.");
            }
        }
    }

    private static bool IsEnvironment(string value) =>
        !string.IsNullOrWhiteSpace(value)
        && EnvironmentPattern.IsMatch(value);

    private static void ValidateRepositorySources(
        IEnumerable<RepositorySourceOptions> sources,
        List<string> failures)
    {
        foreach (var source in sources)
        {
            ConfigurationValidation.ValidatePath(
                source.RootPath,
                $"Repository source '{source.Name}' root path",
                failures);
        }
    }
}
