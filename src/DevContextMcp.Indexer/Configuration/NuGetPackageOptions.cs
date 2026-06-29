namespace DevContextMcp.Indexer.Configuration;

/// <summary>
/// Package-specific NuGet indexing policy loaded from an external JSON file.
/// </summary>
public sealed class NuGetPackageOptions
{
    public string Environment { get; set; } = string.Empty;

    public string PackageId { get; set; } = string.Empty;

    public string? Versions { get; set; }

    public int MaxVersionsPerPackage { get; set; } = 2;
}
