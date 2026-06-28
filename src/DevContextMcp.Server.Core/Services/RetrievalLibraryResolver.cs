using DevContextMcp.Server.Core.Infrastructure;
using DevContextMcp.Server.Core.Models;

namespace DevContextMcp.Server.Core.Services;

/// <summary>
/// Resolves a library reference to a single best selection, applying environment and source
/// priority order and delegating version selection to the version resolver.
/// </summary>
internal sealed class RetrievalLibraryResolver(
    INuGetReadStore store,
    IVersionResolver versionResolver) :
    ILibraryResolver
{
    public async Task<LibraryResolutionResult> ResolveAsync(
        string databasePath,
        LibraryId libraryId,
        IReadOnlyList<string> environmentOrder,
        IReadOnlyList<string> sourceOrder,
        string? requestedVersion,
        string? projectVersion,
        bool includePrerelease,
        CancellationToken cancellationToken)
    {
        if (libraryId.Environment is not null
            && !await store.EnvironmentExistsAsync(
                databasePath,
                libraryId.Environment,
                cancellationToken))
        {
            return new LibraryResolutionResult(LibraryResolutionStatus.EnvironmentNotFound);
        }

        var libraries = await store.FindLibrariesAsync(
            databasePath: databasePath,
            kind: libraryId.Kind,
            packageId: libraryId.PackageId,
            cancellationToken: cancellationToken);
        var matchingLibraries = libraryId.Environment is null
            ? libraries
            : libraries
                .Where(library => string.Equals(
                    library.Environment,
                    libraryId.Environment,
                    StringComparison.OrdinalIgnoreCase))
                .ToArray();
        if (matchingLibraries.Count == 0)
        {
            return new LibraryResolutionResult(LibraryResolutionStatus.LibraryNotFound);
        }

        var candidates = new List<ResolvedLibrarySelection>();
        foreach (var library in matchingLibraries)
        {
            var versions = await store.ListVersionsAsync(
                databasePath,
                library.LibraryId,
                cancellationToken);
            candidates.Add(new ResolvedLibrarySelection(
                library,
                versions,
                versionResolver.Resolve(
                    versions: versions,
                    requestedVersion: requestedVersion,
                    projectVersion: projectVersion,
                    recommendedVersion: null,
                    includePrerelease: includePrerelease)));
        }

        var selected = candidates
            .OrderBy(candidate => candidate.Version is null)
            .ThenBy(candidate => OrderIndex(environmentOrder, candidate.Library.Environment))
            .ThenBy(candidate => candidate.Version?.WarningCodes.Contains(
                "recommended_version_not_indexed",
                StringComparer.Ordinal) ?? false)
            .ThenBy(candidate => OrderIndex(sourceOrder, candidate.Library.SourceName))
            .ThenBy(candidate => candidate.Library.Environment, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.Library.SourceName, StringComparer.Ordinal)
            .First();

        return new LibraryResolutionResult(LibraryResolutionStatus.Resolved, selected);
    }

    internal static int OrderIndex(IReadOnlyList<string> order, string? value)
    {
        if (value is null)
        {
            return int.MaxValue;
        }
        for (var index = 0; index < order.Count; index++)
        {
            if (order[index].Equals(value, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return int.MaxValue;
    }
}
