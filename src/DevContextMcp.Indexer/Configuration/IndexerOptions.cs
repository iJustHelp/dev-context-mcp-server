namespace DevContextMcp.Indexer.Configuration;

/// <summary>
/// Root configuration for the Indexer console application.
/// </summary>
public sealed class IndexerOptions
{
    public const string SectionName = "DevContextMcp";

    public string DatabasePath { get; set; } = "data/docs.db";

    public IndexerSourceOptions IndexerSource { get; set; } = new IndexerSourceOptions();

    public List<NuGetPackageSourceOptions> NugetPackages { get; set; } = [];

    public IndexingOptions Indexing { get; set; } = new IndexingOptions();

    public IndexerAnalyticsOptions Analytics { get; set; } = new IndexerAnalyticsOptions();
}
