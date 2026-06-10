namespace DevContextMcp.Indexer.Models;

public sealed record DependencyRecord(
    string PackageId,
    string VersionRange,
    string? TargetFramework);
