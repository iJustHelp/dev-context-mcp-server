namespace DevContextMcp.Server.Core.Retrieval.Models;

public sealed record ResolvedLibrarySelection(
    ResolvedLibraryRecord Library,
    IReadOnlyList<IndexedVersionRecord> Versions,
    VersionResolution? Version);
