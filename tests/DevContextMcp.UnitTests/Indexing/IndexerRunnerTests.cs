using DevContextMcp.Indexer;
using DevContextMcp.Indexer.Configuration;
using DevContextMcp.Indexer.Core.Models;
using DevContextMcp.Indexer.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DevContextMcp.UnitTests.Indexing;

public sealed class IndexerRunnerTests
{
    [Fact]
    public async Task NoConfiguredSourcesSucceedsWithoutInvokingCoordinator()
    {
        var coordinator = new UnexpectedCoordinator();
        var executor = new IndexerRunner(
            Options.Create(new IndexerOptions()),
            coordinator,
            NullLogger<IndexerRunner>.Instance,
            new CapturingReportWriter());

        var succeeded = await executor.RunAsync(CancellationToken.None);

        Assert.True(succeeded);
        Assert.False(coordinator.WasCalled);
    }

    [Theory]
    [InlineData("succeeded", true)]
    [InlineData("partial_success", false)]
    [InlineData("failed", false)]
    public async Task ResultReflectsRunStatus(string status, bool expected)
    {
        var runner = CreateRunner(new StubCoordinator(
        [
            Summary(status)
        ]));

        var succeeded = await runner.RunAsync(CancellationToken.None);

        Assert.Equal(expected, succeeded);
    }

    [Fact]
    public async Task ExceptionReturnsFailure()
    {
        var runner = CreateRunner(new StubCoordinator(
            exception: new InvalidOperationException("failure")));

        var succeeded = await runner.RunAsync(CancellationToken.None);

        Assert.False(succeeded);
    }

    [Fact]
    public async Task CancellationIsPropagated()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var runner = CreateRunner(new StubCoordinator(
            exception: new OperationCanceledException(cancellation.Token)));

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            runner.RunAsync(cancellation.Token));
    }

    [Fact]
    public async Task SummaryListsSortedPackageChangesAndEmptySections()
    {
        var logger = new CapturingLogger();
        var reportWriter = new CapturingReportWriter();
        var summary = new IndexRunSummary(
            "fixture",
            "succeeded",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            4,
            4,
            3,
            1,
            [
                new PackageIdentityKey("Zulu.Package", "2.0.0"),
                new PackageIdentityKey("Alpha.Package", "1.0.0")
            ],
            [new PackageIdentityKey("Updated.Package", "3.0.0")],
            [],
            []);
        var runner = CreateRunner(
            new StubCoordinator([summary]),
            logger,
            reportWriter);

        var succeeded = await runner.RunAsync(CancellationToken.None);

        Assert.True(succeeded);
        var message = Assert.Single(logger.Messages);
        Assert.Contains(
            $"Added (2):{Environment.NewLine}        Alpha.Package 1.0.0{Environment.NewLine}        Zulu.Package 2.0.0",
            message);
        Assert.Contains(
            $"Updated (1):{Environment.NewLine}        Updated.Package 3.0.0",
            message);
        Assert.Contains(
            $"Deleted (0):{Environment.NewLine}        (none)",
            message);
        Assert.DoesNotContain("\r\nChanged:", message);
        Assert.DoesNotContain("\r\nUnchanged:", message);
        Assert.Equal(message, Assert.Single(reportWriter.Reports));
    }

    [Fact]
    public async Task FileReportWriterAppendsReportsToConfiguredPath()
    {
        var root = Path.Combine(
            AppContext.BaseDirectory,
            $"indexer-report-{Guid.NewGuid():N}");
        var relativePath = Path.Combine(
            Path.GetFileName(root),
            "reports",
            "indexer.log");
        var expectedPath = Path.GetFullPath(relativePath, AppContext.BaseDirectory);
        var writer = new FileIndexerReportWriter(
            Options.Create(new IndexerOptions { ReportPath = relativePath }));

        try
        {
            await writer.WriteAsync("first report", CancellationToken.None);
            await writer.WriteAsync("second report", CancellationToken.None);

            var content = await File.ReadAllTextAsync(expectedPath);
            Assert.Equal(
                $"first report{Environment.NewLine}{Environment.NewLine}"
                + $"second report{Environment.NewLine}{Environment.NewLine}",
                content);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static IndexerRunner CreateRunner(
        IIndexCoordinator coordinator,
        ILogger<IndexerRunner>? logger = null,
        IIndexerReportWriter? reportWriter = null) =>
        new(
            Options.Create(new IndexerOptions
            {
                Environments =
                [
                    new NuGetEnvironmentOptions
                    {
                        Name = "test",
                        ServiceIndex = "fixture"
                    }
                ]
            }),
            coordinator,
            logger ?? NullLogger<IndexerRunner>.Instance,
            reportWriter ?? new CapturingReportWriter());

    private static IndexRunSummary Summary(string status) =>
        new(
            "fixture",
            status,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            1,
            status == "failed" ? 0 : 1,
            status == "succeeded" ? 1 : 0,
            0,
            status == "succeeded"
                ? [new PackageIdentityKey("Fixture.Package", "1.0.0")]
                : [],
            [],
            [],
            []);

    private sealed class UnexpectedCoordinator : IIndexCoordinator
    {
        public bool WasCalled { get; private set; }

        public Task<IReadOnlyList<IndexRunSummary>> IndexAllAsync(
            CancellationToken cancellationToken)
        {
            WasCalled = true;
            throw new InvalidOperationException("Coordinator should not be called.");
        }
    }

    private sealed class StubCoordinator(
        IReadOnlyList<IndexRunSummary>? summaries = null,
        Exception? exception = null) : IIndexCoordinator
    {
        public Task<IReadOnlyList<IndexRunSummary>> IndexAllAsync(
            CancellationToken cancellationToken)
        {
            if (exception is not null)
            {
                throw exception;
            }

            return Task.FromResult(summaries ?? []);
        }
    }

    private sealed class CapturingLogger : ILogger<IndexerRunner>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull =>
            null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }

    private sealed class CapturingReportWriter : IIndexerReportWriter
    {
        public List<string> Reports { get; } = [];

        public Task WriteAsync(
            string report,
            CancellationToken cancellationToken)
        {
            Reports.Add(report);
            return Task.CompletedTask;
        }
    }
}
