namespace DevContextMcp.Indexer.Models;

public sealed record IndexSourceDefinition(
    string Name,
    string Environment,
    string ServiceIndex,
    IReadOnlyList<string> PackagePrefixes,
    IReadOnlyList<string> PackageIds,
    bool IncludePrerelease,
    bool IncludeUnlisted,
    int MaxVersionsPerPackage,
    int MaxPackages);
