namespace DevContextMcp.Indexer.Configuration;

/// <summary>
/// Indexer view of the analytics database configuration. The indexer writes the last-run
/// indexing snapshot here, sharing the database with the host the same way the documentation
/// index is shared. Bound from the same <c>DevContextMcp:Analytics</c> section the host uses.
/// </summary>
public sealed class IndexerAnalyticsOptions
{
    public string DatabasePath { get; set; } = "data/analytics.db";
}
