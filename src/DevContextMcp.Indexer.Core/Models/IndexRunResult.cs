namespace DevContextMcp.Indexer.Core.Models;

/// <summary>
/// The aggregate result of a full indexing run across all NuGet sources.
/// </summary>
public sealed record IndexRunResult(
    IReadOnlyList<IndexRunSummary> Summaries,
    IReadOnlyList<IndexedLibrary> IndexedLibraries)
{
    /// <summary>
    /// The status of the run as a whole, aggregated from the per-source summaries.
    /// </summary>
    public IndexRunStatus Status =>
        IndexRunStatuses.Aggregate(Summaries.Select(summary => summary.Status));

    /// <summary>
    /// True only when every source succeeded. Drives the indexer's process exit code.
    /// </summary>
    public bool Succeeded => Status == IndexRunStatus.Succeeded;
}
