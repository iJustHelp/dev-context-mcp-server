namespace DevContextMcp.Indexer.Core.Models;

// Defines a NuGet source to index: its feed, environment, package selections, and limits.
public sealed record IndexSourceDefinition(
    string Name,
    string Environment,
    string ServiceIndex,
    IReadOnlyList<PackageSelectionDefinition> Packages,
    IReadOnlyList<string> DeletedPackageIds,
    int MaxPackages);
