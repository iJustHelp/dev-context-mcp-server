using DevContextMcp.Infrastructure.Indexer.NuGet;
using NuGet.Versioning;

namespace DevContextMcp.UnitTests.Indexing;

public sealed class NuGetPackageSourceClientVersionSelectionTests
{
    // Purpose: default policy keeps the newest version from each of the newest two major groups.
    [Fact]
    public void SelectDefaultVersions_SelectsNewestVersionFromNewestTwoMajors()
    {
        var selected = NuGetPackageSourceClient.SelectDefaultVersions(
            Versions(
                "4.0.0",
                "3.2.1",
                "3.2.0",
                "3.1.5",
                "3.0.9",
                "2.5.4",
                "2.4.3",
                "1.9.0"),
            maxVersions: 2);

        Assert.Equal(
            ["4.0.0", "3.2.1"],
            selected.Select(version => version.ToNormalizedString()));
    }

    // Purpose: total cap can be stricter than the two-major default.
    [Fact]
    public void SelectDefaultVersions_TotalCapWins()
    {
        var selected = NuGetPackageSourceClient.SelectDefaultVersions(
            Versions("3.2.0", "3.1.0", "2.2.0", "2.1.0"),
            maxVersions: 1);

        Assert.Equal(
            ["3.2.0"],
            selected.Select(version => version.ToNormalizedString()));
    }

    private static IReadOnlyList<NuGetVersion> Versions(params string[] versions) =>
        versions.Select(NuGetVersion.Parse).ToArray();
}
