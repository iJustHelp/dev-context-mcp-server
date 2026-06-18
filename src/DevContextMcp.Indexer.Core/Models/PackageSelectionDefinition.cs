namespace DevContextMcp.Indexer.Core.Models;

// Selects which versions of a package to index (prerelease, unlisted, and a version cap).
public sealed record PackageSelectionDefinition(
    string PackageId,
    bool IncludePrerelease,
    bool IncludeUnlisted,
    int MaxVersions);
