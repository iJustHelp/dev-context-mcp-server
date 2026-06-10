using DevContextMcp.Server.Core.Retrieval.Models;

namespace DevContextMcp.Server.Core.Retrieval.Services;

public interface IRetrievalLibraryResolver
{
    Task<LibraryResolutionResult> ResolveAsync(
        string databasePath,
        LibraryId libraryId,
        IReadOnlyList<string> environmentOrder,
        IReadOnlyList<string> sourceOrder,
        IReadOnlyDictionary<string, string> recommendedVersions,
        string? requestedVersion,
        string? projectVersion,
        bool includePrerelease,
        CancellationToken cancellationToken);
}
