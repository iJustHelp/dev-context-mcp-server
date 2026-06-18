namespace DevContextMcp.Indexer.Core.Models;

/// <summary>
/// Resolved settings for an indexing run: database path, limits, sources, and optional documentation.
/// </summary>
public sealed record IndexingSettings(
    string DatabasePath,
    PackageProcessingLimits Limits,
    IReadOnlyList<IndexSourceDefinition> Sources,
    DocumentationSourceDefinition? Documentation = null);
