namespace DevContextMcp.Indexer.Core.Models;

/// <summary>
/// Resolved settings for an indexing run: database path, limits, and NuGet sources.
/// </summary>
public sealed record IndexingSettings(
    string DatabasePath,
    PackageProcessingLimits Limits,
    IReadOnlyList<IndexSourceDefinition> Sources);
