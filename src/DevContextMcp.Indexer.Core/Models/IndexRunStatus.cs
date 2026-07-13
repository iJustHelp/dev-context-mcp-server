namespace DevContextMcp.Indexer.Core.Models;

/// <summary>
/// The outcome of an indexing run, either for a single source or aggregated across a whole run.
/// </summary>
public enum IndexRunStatus
{
    Succeeded,
    PartialSuccess,
    Failed
}

/// <summary>
/// Owns the run-status rules: how a status follows from an indexing outcome, how per-source
/// statuses aggregate into a run status, and the persisted string form. Callers ask for these
/// rules instead of re-deriving them.
/// </summary>
public static class IndexRunStatuses
{
    public const string SucceededValue = "succeeded";
    public const string PartialSuccessValue = "partial_success";
    public const string FailedValue = "failed";

    /// <summary>
    /// Derives the status of one source: nothing indexed alongside errors is a failure,
    /// anything indexed alongside errors is a partial success.
    /// </summary>
    public static IndexRunStatus FromOutcome(int indexedPackages, int errors) =>
        indexedPackages == 0 && errors > 0
            ? IndexRunStatus.Failed
            : errors > 0
                ? IndexRunStatus.PartialSuccess
                : IndexRunStatus.Succeeded;

    /// <summary>
    /// Aggregates per-source statuses into the status of the whole run. A run with no sources
    /// succeeded.
    /// </summary>
    public static IndexRunStatus Aggregate(IEnumerable<IndexRunStatus> statuses)
    {
        var values = statuses.ToArray();

        return values.All(status => status == IndexRunStatus.Succeeded)
            ? IndexRunStatus.Succeeded
            : values.All(status => status == IndexRunStatus.Failed)
                ? IndexRunStatus.Failed
                : IndexRunStatus.PartialSuccess;
    }

    /// <summary>
    /// The stored form of the status, written to the run history and the last-run snapshot.
    /// </summary>
    public static string ToPersistedValue(this IndexRunStatus status) => status switch
    {
        IndexRunStatus.Succeeded => SucceededValue,
        IndexRunStatus.PartialSuccess => PartialSuccessValue,
        IndexRunStatus.Failed => FailedValue,
        _ => throw new ArgumentOutOfRangeException(
            nameof(status),
            status,
            "Unknown index run status.")
    };
}
