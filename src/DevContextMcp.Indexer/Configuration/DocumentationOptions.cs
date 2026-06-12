namespace DevContextMcp.Indexer.Configuration;

/// <summary>
/// Configuration for the single company documentation library.
/// </summary>
public sealed class DocumentationOptions
{
    public string RootPath { get; set; } = string.Empty;

    public List<string> Extensions { get; set; } = [];
}
