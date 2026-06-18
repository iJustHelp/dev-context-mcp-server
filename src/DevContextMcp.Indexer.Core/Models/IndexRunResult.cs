namespace DevContextMcp.Indexer.Core.Models;

/// <summary>
/// The aggregate result of a full indexing run across all sources and documentation.
/// </summary>
public sealed record IndexRunResult(
    IReadOnlyList<IndexRunSummary> Summaries,
    IReadOnlyList<IndexedLibrary> IndexedLibraries,
    IReadOnlyList<string> IndexedDocuments);
