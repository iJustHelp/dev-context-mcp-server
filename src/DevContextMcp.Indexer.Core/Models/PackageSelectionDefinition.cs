namespace DevContextMcp.Indexer.Core.Models;

/// <summary>
/// Selects which versions of a package to index, either by the default version window or by
/// explicit entries (full versions or "MAJOR.MINOR.*" wildcards).
/// </summary>
public sealed record PackageSelectionDefinition(
    string PackageId,
    IReadOnlyList<string>? Versions = null);
