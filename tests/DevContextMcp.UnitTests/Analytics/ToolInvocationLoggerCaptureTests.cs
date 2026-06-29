using DevContextMcp.Server.Analytics;
using DevContextMcp.Server.Configuration;
using DevContextMcp.Server.Core.Contracts.Common;
using DevContextMcp.Server.Core.Models.Analytics;
using DevContextMcp.Server.Tools;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace DevContextMcp.UnitTests.Analytics;

// IAnalyticsRecorder is the injected interface collaborator and is mocked. The logger
// (NullLogger) and the sealed AnalyticsUserResolver are framework/value collaborators
// supplied as real instances per the test standard.
public sealed class ToolInvocationLoggerCaptureTests
{
    private readonly Mock<IAnalyticsRecorder> _recorder = new();
    private readonly ToolInvocationLogger _target;
    private ToolInvocationRecord? _captured;

    public ToolInvocationLoggerCaptureTests()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-User-Name"] = "alice";
        var resolver = new AnalyticsUserResolver(
            new HttpContextAccessor { HttpContext = context },
            Options.Create(new DevContextMcpOptions()));
        _recorder
            .Setup(recorder => recorder.Record(It.IsAny<ToolInvocationRecord>()))
            .Callback<ToolInvocationRecord>(record => _captured = record);

        _target = new ToolInvocationLogger(
            Options.Create(new DevContextMcpOptions()),
            NullLogger<ToolInvocationLogger>.Instance,
            _recorder.Object,
            resolver);
    }

    // Purpose: records one success event carrying the resolved user
    [Fact]
    public async Task InvokeAsync_Success_RecordsSuccessEvent()
    {
        // arrange
        _recorder.Setup(recorder => recorder.Enabled).Returns(true);

        // act
        var actual = await _target.InvokeAsync(
            "query_docs",
            "request",
            _ => Task.FromResult("response"),
            CancellationToken.None);

        // assert
        Assert.Equal("response", actual);
        Assert.NotNull(_captured);
        Assert.Equal("query_docs", _captured.ToolName);
        Assert.Equal("alice", _captured.UserName);
        Assert.Null(_captured.ErrorType);
        Assert.Equal("ok", _captured.ToolResultStatus);
        Assert.True(_captured.DurationMs >= 0);
        VerifyRecorded(AnalyticsStatus.Success);
    }

    // Purpose: records the tool response status separately from the analytics status
    [Fact]
    public async Task InvokeAsync_SuccessWithToolResponse_RecordsToolResultStatus()
    {
        // arrange
        _recorder.Setup(recorder => recorder.Enabled).Returns(true);

        // act
        var actual = await _target.InvokeAsync(
            "query_docs",
            "request",
            _ => Task.FromResult(new TestToolResponse { Status = ToolResultStatus.NotFound }),
            CancellationToken.None);

        // assert
        Assert.Equal(ToolResultStatus.NotFound, actual.Status);
        Assert.NotNull(_captured);
        Assert.Equal(AnalyticsStatus.Success, _captured.Status);
        Assert.Equal("not_found", _captured.ToolResultStatus);
        Assert.Null(_captured.ResultDetailJson);
        VerifyRecorded(AnalyticsStatus.Success);
    }

    // Purpose: persists bounded detail JSON when the tool response includes errors
    [Fact]
    public async Task InvokeAsync_NotOkToolResponseWithErrors_RecordsResultDetailJson()
    {
        // arrange
        _recorder.Setup(recorder => recorder.Enabled).Returns(true);
        var response = new TestToolResponse
        {
            Status = ToolResultStatus.NotFound,
            Errors =
            [
                new ToolError
                {
                    Code = "library_not_found",
                    Message = "Library was not found.",
                },
            ],
            ResolvedContext = new ResolvedContext
            {
                LibraryId = "nuget:qa/Demo.Cities",
                Environment = "qa",
                Version = "1.0.0",
            },
        };

        // act
        await _target.InvokeAsync(
            "query_docs",
            "request",
            _ => Task.FromResult(response),
            CancellationToken.None);

        // assert
        Assert.NotNull(_captured);
        Assert.Equal("not_found", _captured!.ToolResultStatus);
        Assert.NotNull(_captured.ResultDetailJson);
        Assert.Contains("library_not_found", _captured.ResultDetailJson, StringComparison.Ordinal);
        Assert.Contains("nuget:qa/Demo.Cities", _captured.ResultDetailJson, StringComparison.Ordinal);
    }

    // Purpose: does not persist detail JSON for ok tool responses
    [Fact]
    public async Task InvokeAsync_OkToolResponse_DoesNotRecordResultDetailJson()
    {
        // arrange
        _recorder.Setup(recorder => recorder.Enabled).Returns(true);

        // act
        await _target.InvokeAsync(
            "query_docs",
            "request",
            _ => Task.FromResult(new TestToolResponse { Status = ToolResultStatus.Ok }),
            CancellationToken.None);

        // assert
        Assert.NotNull(_captured);
        Assert.Null(_captured!.ResultDetailJson);
    }

    // Purpose: records an error event with the exception type when the call faults
    [Fact]
    public async Task InvokeAsync_WhenInvokeThrows_RecordsErrorEvent()
    {
        // arrange
        _recorder.Setup(recorder => recorder.Enabled).Returns(true);

        // act
        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _target.InvokeAsync<string, string>(
                "query_docs",
                "request",
                _ => throw new InvalidOperationException("boom"),
                CancellationToken.None));

        // assert
        Assert.Equal("boom", actual.Message);
        Assert.NotNull(_captured);
        Assert.Equal(nameof(InvalidOperationException), _captured.ErrorType);
        Assert.Equal("error", _captured.ToolResultStatus);
        VerifyRecorded(AnalyticsStatus.Error);
    }

    // Purpose: records a canceled event when the call is canceled
    [Fact]
    public async Task InvokeAsync_WhenCanceled_RecordsCanceledEvent()
    {
        // arrange
        _recorder.Setup(recorder => recorder.Enabled).Returns(true);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        // act
        var actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _target.InvokeAsync<string, string>(
                "query_docs",
                "request",
                _ => Task.FromCanceled<string>(cancellation.Token),
                cancellation.Token));

        // assert
        Assert.NotNull(actual);
        Assert.NotNull(_captured);
        Assert.Equal(AnalyticsStatus.Canceled, _captured.Status);
        Assert.Equal("error", _captured.ToolResultStatus);
        Assert.Null(_captured.ErrorType);
        VerifyRecorded(AnalyticsStatus.Canceled);
    }

    // Purpose: does not record when the recorder is disabled
    [Fact]
    public async Task InvokeAsync_RecorderDisabled_DoesNotRecord()
    {
        // arrange
        _recorder.Setup(recorder => recorder.Enabled).Returns(false);

        // act
        var actual = await _target.InvokeAsync(
            "query_docs",
            "request",
            _ => Task.FromResult("response"),
            CancellationToken.None);

        // assert
        Assert.Equal("response", actual);
        Assert.Null(_captured);
        _recorder.VerifyGet(recorder => recorder.Enabled, Times.Once);
        _recorder.Verify(
            recorder => recorder.Record(It.IsAny<ToolInvocationRecord>()),
            Times.Never);
        _recorder.VerifyNoOtherCalls();
    }

    private void VerifyRecorded(string expectedStatus)
    {
        _recorder.VerifyGet(recorder => recorder.Enabled, Times.Once);
        _recorder.Verify(
            recorder => recorder.Record(
                It.Is<ToolInvocationRecord>(record => record.Status == expectedStatus)),
            Times.Once);
        _recorder.VerifyNoOtherCalls();
    }

    private sealed record TestToolResponse : ToolResponse<object>;
}
