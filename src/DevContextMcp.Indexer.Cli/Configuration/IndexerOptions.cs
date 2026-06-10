namespace DevContextMcp.Indexer.Cli.Configuration;

/// <summary>
/// Root configuration for the Indexer console application.
/// </summary>
public sealed class IndexerOptions
{
    public const string SectionName = "DevContextMcp";

    public string DatabasePath { get; set; } = "data/docs.db";

    public List<NuGetSourceOptions> NuGetSources { get; set; } = [];

    public List<RepositorySourceOptions> RepositorySources { get; set; } = [];

    public IndexingOptions Indexing { get; set; } = new();
}
