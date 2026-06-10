namespace DevContextMcp.Indexer.Cli.Configuration;

/// <summary>
/// Approved NuGet v3 source configuration.
/// </summary>
public sealed class NuGetSourceOptions
{
    public string Name { get; set; } = string.Empty;

    public string Environment { get; set; } = string.Empty;

    public string ServiceIndex { get; set; } = string.Empty;

    public List<string> PackagePrefixes { get; set; } = [];

    public List<string> PackageIds { get; set; } = [];

    public bool IncludePrerelease { get; set; }

    public bool IncludeUnlisted { get; set; }

    public int MaxVersionsPerPackage { get; set; } = 3;

    public int MaxPackages { get; set; } = 100;
}
