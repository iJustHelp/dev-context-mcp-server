namespace DevContextMcp.Indexer.Cli.Configuration;

/// <summary>
/// Approved source repository root configuration.
/// </summary>
public sealed class RepositorySourceOptions
{
    public string Name { get; set; } = string.Empty;

    public string RootPath { get; set; } = string.Empty;
}
