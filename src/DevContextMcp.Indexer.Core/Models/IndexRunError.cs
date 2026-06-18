namespace DevContextMcp.Indexer.Core.Models;

// An error recorded during an indexing run, optionally scoped to a specific package version.
public sealed record IndexRunError(
    string Code,
    string Message,
    string? PackageId = null,
    string? Version = null);
