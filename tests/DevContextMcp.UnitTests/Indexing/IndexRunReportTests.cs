using DevContextMcp.Indexer;
using DevContextMcp.Indexer.Core.Models;
using Microsoft.Extensions.Logging;

namespace DevContextMcp.UnitTests.Indexing;

public sealed class IndexRunReportTests
{
    // Purpose: lists the changed packages in sorted order and keeps the empty sections
    [Fact]
    public void Build_SourceChanged_ListsSortedPackageChangesAndEmptySections()
    {
        // arrange
        var result = new IndexRunResult(
            [
                Summary(IndexRunStatus.Succeeded) with
                {
                    Added =
                    [
                        new PackageIdentityKey("Zulu.Package", "2.0.0"),
                        new PackageIdentityKey("Alpha.Package", "1.0.0")
                    ],
                    Updated = [new PackageIdentityKey("Updated.Package", "3.0.0")]
                }
            ],
            []);

        // act
        var actual = IndexRunReport.Build(result);

        // assert
        var summary = Assert.Single(
            actual,
            entry => entry.Message.Contains("Environment", StringComparison.Ordinal));
        Assert.Equal(LogLevel.Information, summary.Level);
        Assert.Contains("Added (2):", summary.Message);
        Assert.Contains("Alpha.Package 1.0.0", summary.Message);
        Assert.Contains("Zulu.Package 2.0.0", summary.Message);
        Assert.True(
            summary.Message.IndexOf("Alpha.Package", StringComparison.Ordinal)
            < summary.Message.IndexOf("Zulu.Package", StringComparison.Ordinal));
        Assert.Contains("Updated (1):", summary.Message);
        Assert.Contains("Updated.Package 3.0.0", summary.Message);
        Assert.Contains("Deleted (0):", summary.Message);
        Assert.DoesNotContain("Changed:", summary.Message);
        Assert.DoesNotContain("Unchanged:", summary.Message);
    }

    // Purpose: omits a source that succeeded without changing anything
    [Fact]
    public void Build_SourceSucceededUnchanged_OmitsSummary()
    {
        // arrange
        var result = new IndexRunResult([Summary(IndexRunStatus.Succeeded)], []);

        // act
        var actual = IndexRunReport.Build(result);

        // assert
        Assert.DoesNotContain(
            actual,
            entry => entry.Message.Contains("Environment", StringComparison.Ordinal));
        Assert.Contains(
            actual,
            entry => entry.Message.Contains("Indexed NuGets", StringComparison.Ordinal));
    }

    // Purpose: reports a failed source at error level, ahead of the inventory
    [Fact]
    public void Build_SourceFailed_ReportsErrorBeforeInventory()
    {
        // arrange
        var result = new IndexRunResult(
            [
                Summary(IndexRunStatus.Failed) with
                {
                    Errors = [new IndexRunError("fixture_error", "Fixture failure.")]
                }
            ],
            [
                new IndexedLibrary(
                    "Demo.Cities",
                    [
                        new IndexedLibraryEnvironment("prod", ["1.0.1", "1.0.0"]),
                        new IndexedLibraryEnvironment("qa", ["1.1.0"])
                    ])
            ]);

        // act
        var actual = IndexRunReport.Build(result);

        // assert
        var summaryIndex = actual.ToList().FindIndex(entry =>
            entry.Message.Contains("Environment", StringComparison.Ordinal));
        var inventoryIndex = actual.ToList().FindIndex(entry =>
            entry.Message.Contains("Indexed NuGets", StringComparison.Ordinal));
        Assert.True(inventoryIndex > summaryIndex);
        Assert.Equal(LogLevel.Error, actual[summaryIndex].Level);
        Assert.Contains("Status: failed", actual[summaryIndex].Message);

        var inventory = actual[inventoryIndex].Message;
        Assert.Contains("Demo.Cities versions (3)", inventory);
        Assert.Contains("    prod (2): 1.0.1, 1.0.0", inventory);
        Assert.Contains("    qa (1): 1.1.0", inventory);
    }

    // Purpose: reports a partially succeeded source as a warning
    [Fact]
    public void Build_SourcePartiallySucceeded_ReportsWarning()
    {
        // arrange
        var result = new IndexRunResult(
            [
                Summary(IndexRunStatus.PartialSuccess) with
                {
                    Errors = [new IndexRunError("fixture_error", "Fixture failure.")]
                }
            ],
            []);

        // act
        var actual = IndexRunReport.Build(result);

        // assert
        var summary = Assert.Single(
            actual,
            entry => entry.Message.Contains("Environment", StringComparison.Ordinal));
        Assert.Equal(LogLevel.Warning, summary.Level);
        Assert.Contains("Status: partial_success", summary.Message);
    }

    // Purpose: reports an empty inventory rather than an empty block
    [Fact]
    public void Build_NothingIndexed_InventoryPrintsNone()
    {
        // arrange
        var result = new IndexRunResult([], []);

        // act
        var actual = IndexRunReport.Build(result);

        // assert
        var inventory = Assert.Single(
            actual,
            entry => entry.Message.Contains("Indexed NuGets", StringComparison.Ordinal));
        Assert.Contains("(none)", inventory.Message);
    }

    private static IndexRunSummary Summary(IndexRunStatus status) =>
        new IndexRunSummary(
            SourceName: "fixture",
            Status: status,
            Environment: "qa",
            StartedAt: DateTimeOffset.UtcNow,
            CompletedAt: DateTimeOffset.UtcNow,
            Discovered: 1,
            Indexed: status == IndexRunStatus.Failed ? 0 : 1,
            Changed: 0,
            Unchanged: 1,
            Added: [],
            Updated: [],
            Deleted: [],
            Errors: []);
}
