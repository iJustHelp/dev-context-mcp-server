using DevContextMcp.Server.Core.Models;
using DevContextMcp.Server.Core.Services;

namespace DevContextMcp.UnitTests.Retrieval;

public sealed class VersionResolverTests
{
    private readonly VersionResolver _resolver = new();

    [Fact]
    public void RequestedVersionWins()
    {
        var result = _resolver.Resolve(
            versions: Versions(),
            requestedVersion: "1.0.0",
            projectVersion: "2.0.0",
            recommendedVersion: "2.0.0");

        Assert.NotNull(result);
        Assert.Equal("1.0.0", result.Version.Version);
        Assert.Equal("requested", result.Reason);
    }

    [Fact]
    public void RecommendationWinsWhenNoContextVersionExists()
    {
        var result = _resolver.Resolve(
            versions: Versions(),
            requestedVersion: null,
            projectVersion: null,
            recommendedVersion: "2.0.0");

        Assert.NotNull(result);
        Assert.Equal("2.0.0", result.Version.Version);
        Assert.Equal("configured_recommendation", result.Reason);
    }

    [Fact]
    public void LatestStableUsesSemanticVersionOrdering()
    {
        var versions = Versions()
            .Append(new("four", "10.0.0", true, false, false, null))
            .ToArray();

        var result = _resolver.Resolve(
            versions: versions,
            requestedVersion: null,
            projectVersion: null,
            recommendedVersion: null);

        Assert.NotNull(result);
        Assert.Equal("10.0.0", result.Version.Version);
    }

    [Fact]
    public void PrereleaseVersionsAreExcluded()
    {
        var prerelease = new[]
        {
            new IndexedVersionRecord(
                LibraryVersionId: "one",
                Version: "3.0.0-beta.1",
                Listed: true,
                Prerelease: true,
                Deprecated: false,
                PublishedAt: null)
        };

        Assert.Null(_resolver.Resolve(
            versions: prerelease,
            requestedVersion: null,
            projectVersion: null,
            recommendedVersion: null));
    }

    private static IReadOnlyList<IndexedVersionRecord> Versions() =>
    [
        new("one", "1.0.0", true, false, false, null),
        new("two", "2.0.0", true, false, false, null),
        new("three", "3.0.0-beta.1", true, true, false, null)
    ];
}
