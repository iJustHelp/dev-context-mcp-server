namespace DevContextMcp.Server.Core.Models;

public sealed record ResolvedLibraryRecord(
    string LibraryId,
    string Kind,
    string DisplayName,
    string SourceName,
    string? Environment,
    string PackageId,
    string? Description);
