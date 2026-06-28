namespace DevContextMcp.Indexer.Core.Models;

/// <summary>
/// The aggregate result of a full indexing run across all NuGet sources.
/// </summary>
public sealed record IndexRunResult(
    IReadOnlyList<IndexRunSummary> Summaries,
    IReadOnlyList<IndexedLibrary> IndexedLibraries);
