using DevContextMcp.Server.Core.Models;

namespace DevContextMcp.Server.Core.Services;

/// <summary>
/// Resolves a library reference to a single best selection using priority order and version rules.
/// </summary>
public interface ILibraryResolver
{
    Task<LibraryResolutionResult> ResolveAsync(
        string databasePath,
        LibraryId libraryId,
        IReadOnlyList<string> environmentOrder,
        IReadOnlyList<string> sourceOrder,
        string? requestedVersion,
        string? projectVersion,
        bool includePrerelease,
        CancellationToken cancellationToken);
}
