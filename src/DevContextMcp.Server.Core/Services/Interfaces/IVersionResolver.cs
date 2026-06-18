using DevContextMcp.Server.Core.Models;

namespace DevContextMcp.Server.Core.Services;

// Selects the best library version from the indexed versions by precedence rules.
public interface IVersionResolver
{
    VersionResolution? Resolve(
        IReadOnlyList<IndexedVersionRecord> versions,
        string? requestedVersion,
        string? projectVersion,
        string? recommendedVersion,
        bool includePrerelease);
}
