using DevContextMcp.Infrastructure.Indexer.NuGet;
using NuGet.Versioning;

namespace DevContextMcp.UnitTests.Indexing;

public sealed class NuGetPackageSourceClientVersionSelectionTests
{
    // Purpose: default policy keeps the two newest minors (highest patch each) of the two newest majors.
    [Fact]
    public void SelectDefaultVersions_KeepsTwoMinorsOfTwoNewestMajors()
    {
        var selected = NuGetPackageSourceClient.SelectDefaultVersions(
            Versions(
                "3.3.2",
                "3.3.1",
                "3.2.5",
                "3.1.0",
                "2.4.3",
                "2.3.7",
                "2.2.0",
                "1.9.0"));

        Assert.Equal(
            ["3.3.2", "3.2.5", "2.4.3", "2.3.7"],
            selected.Select(version => version.ToNormalizedString()));
    }

    // Purpose: a single major returns up to its two newest minors.
    [Fact]
    public void SelectDefaultVersions_SingleMajorKeepsTwoNewestMinors()
    {
        var selected = NuGetPackageSourceClient.SelectDefaultVersions(
            Versions("3.4.0", "3.3.0", "3.2.0", "3.1.0"));

        Assert.Equal(
            ["3.4.0", "3.3.0"],
            selected.Select(version => version.ToNormalizedString()));
    }

    // Purpose: packages with fewer minors than the window return all eligible versions.
    [Fact]
    public void SelectDefaultVersions_SparseVersionsReturnAll()
    {
        var selected = NuGetPackageSourceClient.SelectDefaultVersions(
            Versions("2.1.0", "2.0.0", "1.0.0"));

        Assert.Equal(
            ["2.1.0", "2.0.0", "1.0.0"],
            selected.Select(version => version.ToNormalizedString()));
    }

    private static IReadOnlyList<NuGetVersion> Versions(params string[] versions) =>
        versions.Select(NuGetVersion.Parse).ToArray();
}
