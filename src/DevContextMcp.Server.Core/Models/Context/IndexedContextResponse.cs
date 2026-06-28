namespace DevContextMcp.Server.Core.Models.Context;

public sealed record IndexedContextResponse(
    DateTimeOffset GeneratedAt,
    IndexedContextTotals Totals,
    IReadOnlyList<IndexedNuGetInventoryItem> Nugets);

public sealed record IndexedContextTotals(
    long SourceCount,
    long EnvironmentCount,
    long LibraryCount,
    long NuGetLibraryCount,
    long NuGetVersionCount);

public sealed record IndexedNuGetInventoryItem(
    string LibraryId,
    string PackageId,
    string DisplayName,
    string SourceName,
    string? Environment,
    string? LatestVersion,
    IReadOnlyList<string> Versions,
    long VersionCount,
    long ArtifactCount,
    long DocumentCount,
    long SymbolCount,
    DateTimeOffset? LastIndexedAt);
