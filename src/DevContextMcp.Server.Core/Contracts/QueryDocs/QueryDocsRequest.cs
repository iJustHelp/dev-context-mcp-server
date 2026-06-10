namespace DevContextMcp.Server.Core.Contracts.QueryDocs;

/// <summary>
/// Request to query indexed package documentation for one library and version.
/// </summary>
public sealed record QueryDocsRequest(
    string LibraryId,
    string Question,
    string? Version = null,
    string? TargetFramework = null,
    int MaxResults = 8,
    string? ProjectVersion = null,
    bool IncludePrerelease = false);
