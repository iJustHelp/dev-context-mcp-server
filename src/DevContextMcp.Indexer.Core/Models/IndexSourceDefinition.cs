namespace DevContextMcp.Indexer.Core.Models;

/// <summary>
/// Defines a NuGet source to index: its feed, environment, package selections, and limits.
/// </summary>
public sealed record IndexSourceDefinition(
    string Name,
    string Environment,
    string ServiceIndex,
    IReadOnlyList<PackageSelectionDefinition> Packages,
    int MaxPackages);
