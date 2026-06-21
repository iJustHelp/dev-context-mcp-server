namespace DevContextMcp.Indexer.Core.Models;

/// <summary>
/// Selects which versions of a package to index, either by explicit version pins or stable-version limits.
/// </summary>
public sealed record PackageSelectionDefinition(
    string PackageId,
    int MaxVersions = 2,
    IReadOnlyList<string>? Versions = null);
