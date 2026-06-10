using DevContextMcp.Server.Core.Retrieval.Models;

namespace DevContextMcp.Server.Core.Retrieval.Services;

public interface IVersionResolver
{
    VersionResolution? Resolve(
        IReadOnlyList<IndexedVersionRecord> versions,
        string? requestedVersion,
        string? projectVersion,
        string? recommendedVersion,
        bool includePrerelease);
}
