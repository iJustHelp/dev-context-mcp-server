using System.Text.RegularExpressions;

namespace DevContextMcp.Server.Core.Services;

// A parsed library identifier ('nuget:env/package' or 'docs:id') with parsing and formatting helpers.
public readonly partial record struct LibraryId(
    string PackageId,
    string? Environment = null,
    string Kind = "nuget")
{
    private const string NuGetPrefix = "nuget:";
    private const string DocsPrefix = "docs:";

    public static bool TryParse(string value, out LibraryId libraryId)
    {
        if (!string.IsNullOrWhiteSpace(value)
            && value.StartsWith(DocsPrefix, StringComparison.OrdinalIgnoreCase)
            && value.Length > DocsPrefix.Length)
        {
            var docsId = value[DocsPrefix.Length..].Trim();
            if (docsId.Length > 0
                && docsId.IndexOf('/') < 0
                && IsValidEnvironment(docsId))
            {
                libraryId = new LibraryId(docsId, null, "docs");
                return true;
            }
        }

        if (!string.IsNullOrWhiteSpace(value)
            && value.StartsWith(NuGetPrefix, StringComparison.OrdinalIgnoreCase)
            && value.Length > NuGetPrefix.Length)
        {
            var payload = value[NuGetPrefix.Length..].Trim();
            var separator = payload.IndexOf('/');
            if (separator < 0)
            {
                libraryId = new LibraryId(payload, null, "nuget");
                return libraryId.PackageId.Length > 0;
            }

            if (separator == 0
                || separator == payload.Length - 1
                || payload.IndexOf('/', separator + 1) >= 0)
            {
                libraryId = default;
                return false;
            }

            var environment = payload[..separator];
            var packageId = payload[(separator + 1)..].Trim();
            if (!IsValidEnvironment(environment) || packageId.Length == 0)
            {
                libraryId = default;
                return false;
            }

            libraryId = new LibraryId(packageId, environment, "nuget");
            return true;
        }

        libraryId = default;
        return false;
    }

    public static bool IsValidEnvironment(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && EnvironmentPattern().IsMatch(value);

    public override string ToString() =>
        Kind.Equals("docs", StringComparison.OrdinalIgnoreCase)
            ? $"{DocsPrefix}{PackageId}"
            : Environment is null
                ? $"{NuGetPrefix}{PackageId}"
                : $"{NuGetPrefix}{Environment}/{PackageId}";

    [GeneratedRegex("^[A-Za-z0-9._-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex EnvironmentPattern();
}
