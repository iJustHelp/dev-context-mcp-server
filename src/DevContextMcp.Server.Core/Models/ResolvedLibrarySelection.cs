namespace DevContextMcp.Server.Core.Models;

/// <summary>
/// A resolved library together with its available versions and the chosen version resolution.
/// </summary>
public sealed record ResolvedLibrarySelection(
    ResolvedLibraryRecord Library,
    IReadOnlyList<IndexedVersionRecord> Versions,
    VersionResolution? Version);
