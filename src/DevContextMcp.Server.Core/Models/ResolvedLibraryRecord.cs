namespace DevContextMcp.Server.Core.Models;

// A single library resolved from the index, identified by source, environment, and package.
public sealed record ResolvedLibraryRecord(
    string LibraryId,
    string Kind,
    string DisplayName,
    string SourceName,
    string? Environment,
    string PackageId,
    string? Description);
