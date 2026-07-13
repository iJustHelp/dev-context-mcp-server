using DevContextMcp.Indexer.Core.Models;
using Microsoft.Extensions.Logging;

namespace DevContextMcp.Indexer;

/// <summary>
/// One block of an indexing run report, with the severity it should be logged at.
/// </summary>
internal sealed record IndexRunReportEntry(LogLevel Level, string Message);

/// <summary>
/// Formats an indexing run as report entries: one entry for every source that failed or changed
/// something, followed by the inventory of everything currently indexed.
/// </summary>
internal static class IndexRunReport
{
    public static IReadOnlyList<IndexRunReportEntry> Build(IndexRunResult result)
    {
        var entries = result.Summaries
            .Where(summary => summary.Status != IndexRunStatus.Succeeded || summary.HasChanges)
            .Select(summary => new IndexRunReportEntry(
                LevelFor(summary.Status),
                FormatSummary(summary)))
            .ToList();

        entries.Add(new IndexRunReportEntry(
            LogLevel.Information,
            FormatIndexedLibraries(result.IndexedLibraries)));

        return entries;
    }

    private static LogLevel LevelFor(IndexRunStatus status) => status switch
    {
        IndexRunStatus.Succeeded => LogLevel.Information,
        IndexRunStatus.PartialSuccess => LogLevel.Warning,
        _ => LogLevel.Error
    };

    private static string FormatSummary(IndexRunSummary summary) =>
        $@"
        Environment: {(!string.IsNullOrWhiteSpace(summary.Environment) ? summary.Environment : summary.SourceName)}
        Status: {summary.Status.ToPersistedValue()}
        NuGets
            Total: {summary.Discovered}
            Indexed: {summary.Indexed}
            Errors: {summary.Errors.Count}
            Added ({summary.Added.Count}): {FormatPackages(summary.Added)}
            Updated ({summary.Updated.Count}): {FormatPackages(summary.Updated)}
            Deleted ({summary.Deleted.Count}):{FormatPackages(summary.Deleted)}
        ";

    private static string FormatPackages(
        IReadOnlyList<PackageIdentityKey> packages) =>
        packages.Count == 0
            ? ""
            : string.Join(
                "; ",
                packages
                    .OrderBy(package => package.PackageId, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(package => package.PackageId, StringComparer.Ordinal)
                    .ThenBy(package => package.Version, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(package => package.Version, StringComparer.Ordinal)
                    .Select(package => $"        {package.PackageId} {package.Version}"));

    private static string FormatIndexedLibraries(IReadOnlyList<IndexedLibrary> libraries)
    {
        var blocks = libraries.Select(library =>
            $"{library.PackageId} versions ({library.Environments.Sum(environment => environment.Versions.Count)})" +
            Environment.NewLine +
            string.Join(
                Environment.NewLine,
                library.Environments.Select(environment =>
                    $"    {environment.Environment} ({environment.Versions.Count}): " +
                    string.Join(", ", environment.Versions))));

        return $"{Environment.NewLine}Indexed NuGets{Environment.NewLine}{Environment.NewLine}" +
            (libraries.Count == 0
                ? "(none)"
                : string.Join($"{Environment.NewLine}{Environment.NewLine}", blocks)) +
            $"{Environment.NewLine}-----------------------------------------------------------------------------";
    }
}
