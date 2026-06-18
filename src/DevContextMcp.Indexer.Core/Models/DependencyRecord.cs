namespace DevContextMcp.Indexer.Core.Models;

// A package dependency declared for a specific target framework.
public sealed record DependencyRecord(
    string PackageId,
    string VersionRange,
    string? TargetFramework);
