using System.Diagnostics;
using System.Text;
using System.Text.Json;
using DevContextMcp.Server.Core.Contracts.Common;
using DevContextMcp.Server.Analytics;
using DevContextMcp.Server.Configuration;
using DevContextMcp.Server.Core.Models.Analytics;
using Microsoft.Extensions.Options;

namespace DevContextMcp.Server.Tools;

/// <summary>
/// Wraps tool invocations to log request/response payloads (size-bounded) and timing at debug level,
/// and to capture one analytics event per invocation.
/// </summary>
internal sealed class ToolInvocationLogger(
    IOptions<DevContextMcpOptions> options,
    ILogger<ToolInvocationLogger> logger,
    IAnalyticsRecorder analyticsRecorder,
    AnalyticsUserResolver userResolver)
{
    private sealed record TruncatedPayloadEnvelope(
        string Preview,
        bool Truncated,
        int OriginalUtf8Bytes);

    private sealed record SerializedPayload(
        string Json,
        int OriginalUtf8Bytes,
        bool Truncated);

    private static readonly JsonSerializerOptions SerializerOptions =
        new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };

    public async Task<TResponse> InvokeAsync<TRequest, TResponse>(
        string toolName,
        TRequest request,
        Func<CancellationToken, Task<TResponse>> invoke,
        CancellationToken cancellationToken)
    {
        var invocationId = Guid.NewGuid().ToString("N");
        var maxPayloadBytes = options.Value.ToolLogging.MaxPayloadBytes;
        var debugEnabled = IsDebugEnabled();
        long? requestBytes = null;
        if (debugEnabled)
        {
            LogPayload(
                direction: "request",
                toolName: toolName,
                invocationId: invocationId,
                payload: request,
                elapsedMilliseconds: null);
            requestBytes = MeasureBytes(request);
        }

        var startedAtUtc = DateTimeOffset.UtcNow;
        var startedAt = Stopwatch.GetTimestamp();
        try
        {
            var response = await invoke(cancellationToken);
            var elapsedMilliseconds = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
            long? responseBytes = null;
            var toolResultStatus = ToWireStatus(GetToolResultStatus(response));
            var notOk = !string.Equals(toolResultStatus, "ok", StringComparison.Ordinal);
            if (debugEnabled || notOk)
            {
                if (notOk && !debugEnabled)
                {
                    LogPayload(
                        direction: "request",
                        toolName: toolName,
                        invocationId: invocationId,
                        payload: request,
                        elapsedMilliseconds: null);
                }

                LogPayload(
                    direction: "response",
                    toolName: toolName,
                    invocationId: invocationId,
                    payload: response,
                    elapsedMilliseconds: elapsedMilliseconds);
                responseBytes = MeasureBytes(response);
                requestBytes ??= MeasureBytes(request);
            }

            RecordAnalytics(
                toolName,
                invocationId,
                startedAtUtc,
                elapsedMilliseconds,
                AnalyticsStatus.Success,
                toolResultStatus,
                errorType: null,
                requestBytes,
                responseBytes,
                ToolInvocationDetailCapture.SerializeResultDetail(
                    request,
                    response,
                    maxPayloadBytes,
                    AnalyticsStatus.Success,
                    toolResultStatus,
                    errorType: null));
            return response;
        }
        catch (OperationCanceledException)
        {
            var elapsedMilliseconds = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
            SafeLog(
                LogLevel.Debug,
                null,
                "MCP tool {ToolName} invocation {InvocationId} was canceled after {ElapsedMilliseconds} ms.",
                toolName,
                invocationId,
                elapsedMilliseconds);
            LogPayload(
                direction: "request",
                toolName: toolName,
                invocationId: invocationId,
                payload: request,
                elapsedMilliseconds: null);
            RecordAnalytics(
                toolName,
                invocationId,
                startedAtUtc,
                elapsedMilliseconds,
                AnalyticsStatus.Canceled,
                ToWireStatus(ToolResultStatus.Error),
                errorType: null,
                requestBytes ?? MeasureBytes(request),
                responseBytes: null,
                ToolInvocationDetailCapture.SerializeResultDetail<TRequest, object?>(
                    request,
                    response: null,
                    maxPayloadBytes,
                    AnalyticsStatus.Canceled,
                    ToWireStatus(ToolResultStatus.Error),
                    errorType: null));
            throw;
        }
        catch (Exception exception)
        {
            var elapsedMilliseconds = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
            SafeLog(
                LogLevel.Error,
                exception,
                "MCP tool {ToolName} invocation {InvocationId} failed after {ElapsedMilliseconds} ms.",
                toolName,
                invocationId,
                elapsedMilliseconds);
            LogPayload(
                direction: "request",
                toolName: toolName,
                invocationId: invocationId,
                payload: request,
                elapsedMilliseconds: null);
            RecordAnalytics(
                toolName,
                invocationId,
                startedAtUtc,
                elapsedMilliseconds,
                AnalyticsStatus.Error,
                ToWireStatus(ToolResultStatus.Error),
                errorType: exception.GetType().Name,
                requestBytes ?? MeasureBytes(request),
                responseBytes: null,
                ToolInvocationDetailCapture.SerializeResultDetail<TRequest, object?>(
                    request,
                    response: null,
                    maxPayloadBytes,
                    AnalyticsStatus.Error,
                    ToWireStatus(ToolResultStatus.Error),
                    errorType: exception.GetType().Name));
            throw;
        }
    }

    private void RecordAnalytics(
        string toolName,
        string invocationId,
        DateTimeOffset startedAt,
        double durationMs,
        string status,
        string toolResultStatus,
        string? errorType,
        long? requestBytes,
        long? responseBytes,
        string? resultDetailJson)
    {
        if (!analyticsRecorder.Enabled)
        {
            return;
        }

        try
        {
            analyticsRecorder.Record(new ToolInvocationRecord(
                Id: invocationId,
                ToolName: toolName,
                UserName: userResolver.Resolve(),
                StartedAt: startedAt,
                DurationMs: durationMs,
                Status: status,
                ToolResultStatus: toolResultStatus,
                ErrorType: errorType,
                RequestBytes: requestBytes,
                ResponseBytes: responseBytes,
                ResultDetailJson: resultDetailJson));
        }
        catch
        {
            // Analytics capture must never change tool behavior.
        }
    }

    private static long? MeasureBytes<TPayload>(TPayload payload)
    {
        try
        {
            return JsonSerializer.SerializeToUtf8Bytes(payload, SerializerOptions).Length;
        }
        catch
        {
            return null;
        }
    }

    private static ToolResultStatus GetToolResultStatus<TResponse>(TResponse response)
    {
        var statusProperty = response?.GetType().GetProperty(nameof(ToolResponse<object>.Status));
        if (statusProperty?.PropertyType == typeof(ToolResultStatus)
            && statusProperty.GetValue(response) is ToolResultStatus status)
        {
            return status;
        }

        return ToolResultStatus.Ok;
    }

    private static string ToWireStatus(ToolResultStatus status) => status switch
    {
        ToolResultStatus.NotReady => "not_ready",
        ToolResultStatus.Ok => "ok",
        ToolResultStatus.NotFound => "not_found",
        ToolResultStatus.InsufficientEvidence => "insufficient_evidence",
        ToolResultStatus.Error => "error",
        _ => "error",
    };

    private bool IsDebugEnabled()
    {
        try
        {
            return logger.IsEnabled(LogLevel.Debug);
        }
        catch
        {
            return false;
        }
    }

    private void LogPayload<TPayload>(
        string direction,
        string toolName,
        string invocationId,
        TPayload payload,
        double? elapsedMilliseconds)
    {
        try
        {
            var serialized = Serialize(payload, options.Value.ToolLogging.MaxPayloadBytes);
            if (elapsedMilliseconds is null)
            {
                SafeLog(
                    LogLevel.Debug,
                    null,
                    "MCP tool {ToolName} invocation {InvocationId} {Direction}. PayloadBytes={PayloadBytes} PayloadTruncated={PayloadTruncated} Payload={Payload}",
                    toolName,
                    invocationId,
                    direction,
                    serialized.OriginalUtf8Bytes,
                    serialized.Truncated,
                    serialized.Json);
            }
            else
            {
                SafeLog(
                    LogLevel.Debug,
                    null,
                    "MCP tool {ToolName} invocation {InvocationId} {Direction} after {ElapsedMilliseconds} ms. PayloadBytes={PayloadBytes} PayloadTruncated={PayloadTruncated} Payload={Payload}",
                    toolName,
                    invocationId,
                    direction,
                    elapsedMilliseconds.Value,
                    serialized.OriginalUtf8Bytes,
                    serialized.Truncated,
                    serialized.Json);
            }
        }
        catch
        {
            // Diagnostic logging must never change tool behavior.
        }
    }

    private void SafeLog(
        LogLevel level,
        Exception? exception,
        string message,
        params object?[] values)
    {
        try
        {
            logger.Log(level, exception, message, values);
        }
        catch
        {
            // Diagnostic logging must never change tool behavior.
        }
    }

    private static SerializedPayload Serialize<TPayload>(
        TPayload payload,
        int maxPayloadBytes)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(payload, SerializerOptions);
        if (bytes.Length <= maxPayloadBytes)
        {
            return new SerializedPayload(Encoding.UTF8.GetString(bytes), bytes.Length, false);
        }

        var json = Encoding.UTF8.GetString(bytes);
        var previewLength = FindLargestPreviewLength(
            json,
            bytes.Length,
            maxPayloadBytes);
        var envelope = new TruncatedPayloadEnvelope(
            json[..previewLength],
            true,
            bytes.Length);
        return new SerializedPayload(
            JsonSerializer.Serialize(envelope, SerializerOptions),
            bytes.Length,
            true);
    }

    private static int FindLargestPreviewLength(
        string json,
        int originalUtf8Bytes,
        int maxPayloadBytes)
    {
        var low = 0;
        var high = json.Length;
        while (low < high)
        {
            var candidate = low + ((high - low + 1) / 2);
            var previewLength = AvoidSplitSurrogate(json, candidate);
            var envelope = new TruncatedPayloadEnvelope(
                json[..previewLength],
                true,
                originalUtf8Bytes);
            var length = JsonSerializer.SerializeToUtf8Bytes(
                envelope,
                SerializerOptions).Length;
            if (length <= maxPayloadBytes)
            {
                low = candidate;
            }
            else
            {
                high = Math.Max(0, candidate - 1);
            }
        }

        return AvoidSplitSurrogate(json, low);
    }

    private static int AvoidSplitSurrogate(string value, int length) =>
        length > 0
        && length < value.Length
        && char.IsHighSurrogate(value[length - 1])
            ? length - 1
            : length;
}
