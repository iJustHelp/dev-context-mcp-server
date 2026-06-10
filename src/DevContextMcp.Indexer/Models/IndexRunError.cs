namespace DevContextMcp.Indexer.Models;

public sealed record IndexRunError(
    string Code,
    string Message,
    string? PackageId = null,
    string? Version = null);
