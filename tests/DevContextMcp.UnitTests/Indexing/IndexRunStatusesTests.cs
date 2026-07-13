using DevContextMcp.Indexer.Core.Models;

namespace DevContextMcp.UnitTests.Indexing;

public sealed class IndexRunStatusesTests
{
    // Purpose: nothing indexed alongside errors is a failure, anything indexed is partial
    [Theory]
    [InlineData(0, 0, IndexRunStatus.Succeeded)]
    [InlineData(3, 0, IndexRunStatus.Succeeded)]
    [InlineData(0, 1, IndexRunStatus.Failed)]
    [InlineData(3, 1, IndexRunStatus.PartialSuccess)]
    public void FromOutcome_IndexedAndErrorCounts_DerivesStatus(
        int indexedPackages,
        int errors,
        IndexRunStatus expected)
    {
        // act
        var actual = IndexRunStatuses.FromOutcome(indexedPackages, errors);

        // assert
        Assert.Equal(expected, actual);
    }

    // Purpose: a run succeeds only when every source succeeded, and fails only when all failed
    [Theory]
    [InlineData(new IndexRunStatus[0], IndexRunStatus.Succeeded)]
    [InlineData(new[] { IndexRunStatus.Succeeded, IndexRunStatus.Succeeded }, IndexRunStatus.Succeeded)]
    [InlineData(new[] { IndexRunStatus.Failed, IndexRunStatus.Failed }, IndexRunStatus.Failed)]
    [InlineData(new[] { IndexRunStatus.Succeeded, IndexRunStatus.Failed }, IndexRunStatus.PartialSuccess)]
    [InlineData(new[] { IndexRunStatus.Succeeded, IndexRunStatus.PartialSuccess }, IndexRunStatus.PartialSuccess)]
    public void Aggregate_SourceStatuses_DerivesRunStatus(
        IndexRunStatus[] statuses,
        IndexRunStatus expected)
    {
        // act
        var actual = IndexRunStatuses.Aggregate(statuses);

        // assert
        Assert.Equal(expected, actual);
    }

    // Purpose: the persisted form is the wire contract for the run history and snapshot
    [Theory]
    [InlineData(IndexRunStatus.Succeeded, "succeeded")]
    [InlineData(IndexRunStatus.PartialSuccess, "partial_success")]
    [InlineData(IndexRunStatus.Failed, "failed")]
    public void ToPersistedValue_Status_ReturnsStoredValue(
        IndexRunStatus status,
        string expected)
    {
        // act
        var actual = status.ToPersistedValue();

        // assert
        Assert.Equal(expected, actual);
    }
}
