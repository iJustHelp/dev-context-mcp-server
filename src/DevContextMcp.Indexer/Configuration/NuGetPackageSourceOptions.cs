namespace DevContextMcp.Indexer.Configuration;

/// <summary>
/// Approved NuGet package source configuration.
/// </summary>
public sealed class NuGetPackageSourceOptions
{
    public string Name { get; set; } = string.Empty;

    public string Environment { get; set; } = string.Empty;

    public string ServiceIndex { get; set; } = string.Empty;

    public int MaxPackages { get; set; } = 100;
}
