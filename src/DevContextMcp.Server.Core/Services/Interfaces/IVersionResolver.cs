using DevContextMcp.Server.Core.Models;

namespace DevContextMcp.Server.Core.Services;

/// <summary>
/// Selects the best library version from the indexed versions by precedence rules.
/// </summary>
public interface IVersionResolver
{
    VersionResolution? Resolve(
        IReadOnlyList<IndexedVersionRecord> versions,
        string? requestedVersion,
        string? projectVersion,
        string? recommendedVersion);
}
