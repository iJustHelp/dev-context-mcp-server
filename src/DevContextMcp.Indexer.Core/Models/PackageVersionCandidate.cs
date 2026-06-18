namespace DevContextMcp.Indexer.Core.Models;

// A discovered package version that is a candidate for download and indexing.
public sealed record PackageVersionCandidate(
    string PackageId,
    string Version,
    bool IsListed,
    bool IsDeprecated,
    DateTimeOffset? PublishedAt);
