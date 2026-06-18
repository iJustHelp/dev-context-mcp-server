namespace DevContextMcp.Server.Core.Models;

// A resolved library together with its available versions and the chosen version resolution.
public sealed record ResolvedLibrarySelection(
    ResolvedLibraryRecord Library,
    IReadOnlyList<IndexedVersionRecord> Versions,
    VersionResolution? Version);
