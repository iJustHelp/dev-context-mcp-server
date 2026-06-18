namespace DevContextMcp.Indexer.Core.Models;

/// <summary>
/// Selects which versions of a package to index (prerelease, unlisted, and a version cap).
/// </summary>
public sealed record PackageSelectionDefinition(
    string PackageId,
    bool IncludePrerelease,
    bool IncludeUnlisted,
    int MaxVersions);
