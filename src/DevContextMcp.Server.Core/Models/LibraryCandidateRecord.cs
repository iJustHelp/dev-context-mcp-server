namespace DevContextMcp.Server.Core.Models;

public sealed record LibraryCandidateRecord(
    string LibraryId,
    string Kind,
    string DisplayName,
    string SourceName,
    string? Environment,
    string PackageId,
    string? Description,
    string? LatestVersion,
    bool LatestListed,
    bool LatestPrerelease,
    bool LatestDeprecated,
    bool ExactId,
    bool PrefixId,
    double TextScore);
