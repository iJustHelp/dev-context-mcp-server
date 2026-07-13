using DevContextMcp.Indexer;
using DevContextMcp.Indexer.Configuration;
using DevContextMcp.Indexer.Core.Models;
using DevContextMcp.Indexer.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace DevContextMcp.UnitTests.Indexing;

public sealed class IndexerRunnerTests
{
    private readonly Mock<IIndexCoordinator> _indexCoordinator = new();
    private readonly Mock<IIndexRunSnapshotPublisher> _snapshotPublisher = new();
    private readonly IndexerOptions _options = new IndexerOptions
    {
        NugetPackages =
        [
            new NuGetPackageSourceOptions
            {
                Name = "test",
                Environment = "test",
                ServiceIndex = "fixture"
            }
        ]
    };

    private readonly IndexerRunner _target;

    public IndexerRunnerTests()
    {
        _target = new IndexerRunner(
            Options.Create(_options),
            _indexCoordinator.Object,
            _snapshotPublisher.Object,
            NullLogger<IndexerRunner>.Instance);
    }

    // Purpose: skips indexing entirely when no NuGet source is configured
    [Fact]
    public async Task RunAsync_NoConfiguredSources_SucceedsWithoutIndexing()
    {
        // arrange
        _options.NugetPackages.Clear();

        // act
        var actual = await _target.RunAsync(CancellationToken.None);

        // assert
        Assert.True(actual);
        _indexCoordinator.Verify(
            coordinator => coordinator.IndexAllAsync(It.IsAny<CancellationToken>()),
            Times.Never);
        _snapshotPublisher.Verify(
            publisher => publisher.PublishAsync(
                It.IsAny<IndexRunResult>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        VerifyNoOtherCalls();
    }

    // Purpose: the exit result follows the aggregated run status
    [Theory]
    [InlineData(IndexRunStatus.Succeeded, true)]
    [InlineData(IndexRunStatus.PartialSuccess, false)]
    [InlineData(IndexRunStatus.Failed, false)]
    public async Task RunAsync_RunCompletes_ResultReflectsRunStatus(
        IndexRunStatus status,
        bool expected)
    {
        // arrange
        SetupRun(new IndexRunResult([Summary(status)], []));

        // act
        var actual = await _target.RunAsync(CancellationToken.None);

        // assert
        Assert.Equal(expected, actual);
        VerifyRunPublished();
        VerifyNoOtherCalls();
    }

    // Purpose: publishes the snapshot of the completed run
    [Fact]
    public async Task RunAsync_RunCompletes_PublishesSnapshot()
    {
        // arrange
        var result = new IndexRunResult([Summary(IndexRunStatus.Succeeded)], []);
        SetupRun(result);

        // act
        var actual = await _target.RunAsync(CancellationToken.None);

        // assert
        Assert.True(actual);
        _indexCoordinator.Verify(
            coordinator => coordinator.IndexAllAsync(It.IsAny<CancellationToken>()),
            Times.Once);
        _snapshotPublisher.Verify(
            publisher => publisher.PublishAsync(
                It.Is<IndexRunResult>(published => ReferenceEquals(published, result)),
                It.IsAny<CancellationToken>()),
            Times.Once);
        VerifyNoOtherCalls();
    }

    // Purpose: returns failure and publishes nothing when the coordinator throws
    [Fact]
    public async Task RunAsync_WhenCoordinatorThrows_ReturnsFailure()
    {
        // arrange
        _indexCoordinator
            .Setup(coordinator => coordinator.IndexAllAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("failure"));

        // act
        var actual = await _target.RunAsync(CancellationToken.None);

        // assert
        Assert.False(actual);
        _indexCoordinator.Verify(
            coordinator => coordinator.IndexAllAsync(It.IsAny<CancellationToken>()),
            Times.Once);
        _snapshotPublisher.Verify(
            publisher => publisher.PublishAsync(
                It.IsAny<IndexRunResult>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        VerifyNoOtherCalls();
    }

    // Purpose: propagates cancellation rather than reporting a failed run
    [Fact]
    public async Task RunAsync_WhenCancelled_PropagatesException()
    {
        // arrange
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        _indexCoordinator
            .Setup(coordinator => coordinator.IndexAllAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException(cancellation.Token));

        // act
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _target.RunAsync(cancellation.Token));

        // assert
        _indexCoordinator.Verify(
            coordinator => coordinator.IndexAllAsync(It.IsAny<CancellationToken>()),
            Times.Once);
        _snapshotPublisher.Verify(
            publisher => publisher.PublishAsync(
                It.IsAny<IndexRunResult>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        VerifyNoOtherCalls();
    }

    private void SetupRun(IndexRunResult result) =>
        _indexCoordinator
            .Setup(coordinator => coordinator.IndexAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

    private void VerifyRunPublished()
    {
        _indexCoordinator.Verify(
            coordinator => coordinator.IndexAllAsync(It.IsAny<CancellationToken>()),
            Times.Once);
        _snapshotPublisher.Verify(
            publisher => publisher.PublishAsync(
                It.IsAny<IndexRunResult>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private void VerifyNoOtherCalls()
    {
        _indexCoordinator.VerifyNoOtherCalls();
        _snapshotPublisher.VerifyNoOtherCalls();
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
            Changed: status == IndexRunStatus.Succeeded ? 1 : 0,
            Unchanged: 0,
            Added: status == IndexRunStatus.Succeeded
                ? [new PackageIdentityKey("Fixture.Package", "1.0.0")]
                : [],
            Updated: [],
            Deleted: [],
            Errors: []);
}
