using DevContextMcp.Server.Analytics;
using DevContextMcp.Server.Configuration;
using DevContextMcp.Server.Core.Models.Analytics;
using DevContextMcp.Server.Tools;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DevContextMcp.UnitTests.Analytics;

public sealed class ToolInvocationLoggerCaptureTests
{
    [Fact]
    public async Task SuccessRecordsSuccessEventWithUser()
    {
        var recorder = new FakeRecorder();
        var target = CreateTarget(recorder, "alice");

        var actual = await target.InvokeAsync(
            toolName: "query_docs",
            request: "request",
            invoke: _ => Task.FromResult("response"),
            cancellationToken: CancellationToken.None);

        Assert.Equal("response", actual);
        var record = Assert.Single(recorder.Records);
        Assert.Equal("query_docs", record.ToolName);
        Assert.Equal(AnalyticsStatus.Success, record.Status);
        Assert.Equal("alice", record.UserName);
        Assert.Null(record.ErrorType);
        Assert.True(record.DurationMs >= 0);
    }

    [Fact]
    public async Task FaultRecordsErrorEventWithExceptionType()
    {
        var recorder = new FakeRecorder();
        var target = CreateTarget(recorder, "alice");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            target.InvokeAsync<string, string>(
                toolName: "query_docs",
                request: "request",
                invoke: _ => throw new InvalidOperationException("boom"),
                cancellationToken: CancellationToken.None));

        var record = Assert.Single(recorder.Records);
        Assert.Equal(AnalyticsStatus.Error, record.Status);
        Assert.Equal(nameof(InvalidOperationException), record.ErrorType);
    }

    [Fact]
    public async Task CancellationRecordsCanceledEvent()
    {
        var recorder = new FakeRecorder();
        var target = CreateTarget(recorder, "alice");
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            target.InvokeAsync<string, string>(
                toolName: "query_docs",
                request: "request",
                invoke: _ => Task.FromCanceled<string>(cancellation.Token),
                cancellationToken: cancellation.Token));

        var record = Assert.Single(recorder.Records);
        Assert.Equal(AnalyticsStatus.Canceled, record.Status);
        Assert.Null(record.ErrorType);
    }

    [Fact]
    public async Task DisabledRecorderCapturesNothing()
    {
        var recorder = new FakeRecorder { Enabled = false };
        var target = CreateTarget(recorder, "alice");

        await target.InvokeAsync(
            toolName: "query_docs",
            request: "request",
            invoke: _ => Task.FromResult("response"),
            cancellationToken: CancellationToken.None);

        Assert.Empty(recorder.Records);
    }

    private static ToolInvocationLogger CreateTarget(IAnalyticsRecorder recorder, string user)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-User-Name"] = user;
        var resolver = new AnalyticsUserResolver(
            new HttpContextAccessor { HttpContext = context },
            Options.Create(new DevContextMcpOptions()));

        return new ToolInvocationLogger(
            Options.Create(new DevContextMcpOptions()),
            NullLogger<ToolInvocationLogger>.Instance,
            recorder,
            resolver);
    }

    private sealed class FakeRecorder : IAnalyticsRecorder
    {
        public List<ToolInvocationRecord> Records { get; } = [];

        public bool Enabled { get; init; } = true;

        public void Record(ToolInvocationRecord record) => Records.Add(record);
    }
}
