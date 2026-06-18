namespace DevContextMcp.Indexer.Core.Models;

/// <summary>
/// A package dependency declared for a specific target framework.
/// </summary>
public sealed record DependencyRecord(
    string PackageId,
    string VersionRange,
    string? TargetFramework);
