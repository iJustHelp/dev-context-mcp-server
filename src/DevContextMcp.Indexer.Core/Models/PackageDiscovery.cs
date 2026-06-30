namespace DevContextMcp.Indexer.Core.Models;

/// <summary>
/// The result of discovering versions for a source: the selected candidates to download and
/// index, plus how many stable, listed versions the feed offered per package (before the
/// default version window is applied).
/// </summary>
public sealed record PackageDiscovery(
    IReadOnlyList<PackageVersionCandidate> Candidates,
    IReadOnlyList<PackageAvailability> Availability);

/// <summary>
/// The number of stable, listed versions a feed offered for a single package.
/// </summary>
public sealed record PackageAvailability(
    string PackageId,
    int AvailableVersions);
