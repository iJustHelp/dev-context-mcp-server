namespace DevContextMcp.Indexer.Core.Models;

/// <summary>
/// A discovered package version that is a candidate for download and indexing.
/// </summary>
public sealed record PackageVersionCandidate(
    string PackageId,
    string Version,
    bool IsListed,
    bool IsDeprecated,
    DateTimeOffset? PublishedAt);
