using System.Text;
using System.Text.Json;
using DevContextMcp.Server.Core.Contracts.Common;
using DevContextMcp.Server.Core.Models.Analytics;

namespace DevContextMcp.Server.Tools;

/// <summary>
/// Builds bounded detail payloads for not-ok analytics events.
/// </summary>
internal static class ToolInvocationDetailCapture
{
    private const int MaxMetadataBytes = 4_096;

    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web)
        {
            WriteIndented = false
        };

    private sealed record TruncatedPayloadEnvelope(
        string Preview,
        bool Truncated,
        int OriginalUtf8Bytes);

    internal static string? SerializeResultDetail<TRequest, TResponse>(
        TRequest? request,
        TResponse? response,
        int maxPayloadBytes,
        string transportStatus,
        string toolResultStatus,
        string? errorType)
    {
        if (IsOk(transportStatus, toolResultStatus, errorType))
        {
            return null;
        }

        try
        {
            var detail = BuildDetail(request, response, maxPayloadBytes);
            if (detail is null)
            {
                return null;
            }

            return SerializeBounded(detail, maxPayloadBytes);
        }
        catch
        {
            return null;
        }
    }

    internal static ToolInvocationResultDetail? DeserializeResultDetail(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ToolInvocationResultDetail>(json, SerializerOptions);
        }
        catch
        {
            return null;
        }
    }

    internal static bool HasDetail(string transportStatus, string toolResultStatus) =>
        !string.Equals(transportStatus, AnalyticsStatus.Success, StringComparison.Ordinal)
        || !string.Equals(toolResultStatus, "ok", StringComparison.Ordinal);

    private static bool IsOk(
        string transportStatus,
        string toolResultStatus,
        string? errorType) =>
        string.Equals(transportStatus, AnalyticsStatus.Success, StringComparison.Ordinal)
        && string.Equals(toolResultStatus, "ok", StringComparison.Ordinal)
        && errorType is null;

    private static ToolInvocationResultDetail? BuildDetail<TRequest, TResponse>(
        TRequest? request,
        TResponse? response,
        int maxPayloadBytes)
    {
        var requestPayload = SerializePayload(request, maxPayloadBytes);
        var responsePayload = SerializePayload(response, maxPayloadBytes);
        var errors = ReadErrors(response);
        var resolvedContext = ReadResolvedContext(response);

        if (requestPayload is null
            && responsePayload is null
            && errors.Count == 0
            && resolvedContext is null)
        {
            return null;
        }

        return new ToolInvocationResultDetail(
            errors,
            resolvedContext,
            requestPayload,
            responsePayload);
    }

    private static IReadOnlyList<ToolInvocationErrorDetail> ReadErrors<TResponse>(TResponse? response)
    {
        if (response is null)
        {
            return [];
        }

        var envelopeType = GetToolResponseEnvelopeType(response.GetType());
        if (envelopeType is null)
        {
            return [];
        }

        var errorsProperty = envelopeType.GetProperty(nameof(ToolResponse<object>.Errors));
        if (errorsProperty?.GetValue(response) is not IEnumerable<ToolError> errors)
        {
            return [];
        }

        return errors
            .Select(error => new ToolInvocationErrorDetail(error.Code, error.Message))
            .ToArray();
    }

    private static ToolInvocationResolvedContextDetail? ReadResolvedContext<TResponse>(TResponse? response)
    {
        if (response is null)
        {
            return null;
        }

        var envelopeType = GetToolResponseEnvelopeType(response.GetType());
        if (envelopeType is null)
        {
            return null;
        }

        var contextProperty = envelopeType.GetProperty(nameof(ToolResponse<object>.ResolvedContext));
        if (contextProperty?.GetValue(response) is not ResolvedContext context)
        {
            return null;
        }

        if (context.LibraryId is null
            && context.SourceId is null
            && context.Environment is null
            && context.Version is null
            && context.VersionSelectionReason is null)
        {
            return null;
        }

        return new ToolInvocationResolvedContextDetail(
            context.LibraryId,
            context.SourceId,
            context.Environment,
            context.Version,
            context.VersionSelectionReason);
    }

    private static Type? GetToolResponseEnvelopeType(Type type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType
                && current.GetGenericTypeDefinition() == typeof(ToolResponse<>))
            {
                return current;
            }
        }

        return null;
    }

    private static ToolInvocationPayloadDetail? SerializePayload<TPayload>(
        TPayload? payload,
        int maxPayloadBytes)
    {
        if (payload is null)
        {
            return null;
        }

        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(payload, SerializerOptions);
            if (bytes.Length <= maxPayloadBytes)
            {
                return new ToolInvocationPayloadDetail(
                    Encoding.UTF8.GetString(bytes),
                    Truncated: false,
                    OriginalUtf8Bytes: bytes.Length);
            }

            var json = Encoding.UTF8.GetString(bytes);
            var previewLength = FindLargestPreviewLength(json, bytes.Length, maxPayloadBytes);
            var envelope = new TruncatedPayloadEnvelope(
                json[..previewLength],
                Truncated: true,
                OriginalUtf8Bytes: bytes.Length);
            return new ToolInvocationPayloadDetail(
                JsonSerializer.Serialize(envelope, SerializerOptions),
                Truncated: true,
                OriginalUtf8Bytes: bytes.Length);
        }
        catch
        {
            return null;
        }
    }

    private static string SerializeBounded(
        ToolInvocationResultDetail detail,
        int maxPayloadBytes)
    {
        var errors = detail.Errors.ToList();
        var request = detail.Request;
        var response = detail.Response;
        var maxDetailBytes = MaxMetadataBytes + (maxPayloadBytes * 2);

        while (true)
        {
            var candidate = new ToolInvocationResultDetail(
                errors,
                detail.ResolvedContext,
                request,
                response);
            var json = JsonSerializer.Serialize(candidate, SerializerOptions);
            if (Encoding.UTF8.GetByteCount(json) <= maxDetailBytes || errors.Count == 0)
            {
                if (Encoding.UTF8.GetByteCount(json) <= maxDetailBytes)
                {
                    return json;
                }

                var shrunk = ShrinkPayloads(request, response, maxPayloadBytes);
                request = shrunk.Request;
                response = shrunk.Response;
                if (request is null && response is null)
                {
                    return json;
                }

                continue;
            }

            errors.RemoveAt(errors.Count - 1);
        }
    }

    private static (ToolInvocationPayloadDetail? Request, ToolInvocationPayloadDetail? Response) ShrinkPayloads(
        ToolInvocationPayloadDetail? request,
        ToolInvocationPayloadDetail? response,
        int maxPayloadBytes)
    {
        if (response?.Truncated == true)
        {
            return (request, HalvePayloadPreview(response, maxPayloadBytes));
        }

        if (request?.Truncated == true)
        {
            return (HalvePayloadPreview(request, maxPayloadBytes), response);
        }

        return (null, response);
    }

    private static ToolInvocationPayloadDetail? HalvePayloadPreview(
        ToolInvocationPayloadDetail payload,
        int maxPayloadBytes)
    {
        try
        {
            var envelope = JsonSerializer.Deserialize<TruncatedPayloadEnvelope>(
                payload.Json,
                SerializerOptions);
            if (envelope is null || envelope.Preview.Length == 0)
            {
                return null;
            }

            var previewLength = Math.Max(0, envelope.Preview.Length / 2);
            previewLength = AvoidSplitSurrogate(envelope.Preview, previewLength);
            var shrunk = new TruncatedPayloadEnvelope(
                envelope.Preview[..previewLength],
                Truncated: true,
                OriginalUtf8Bytes: envelope.OriginalUtf8Bytes);
            var json = JsonSerializer.Serialize(shrunk, SerializerOptions);
            if (Encoding.UTF8.GetByteCount(json) > maxPayloadBytes && previewLength > 0)
            {
                return HalvePayloadPreview(
                    new ToolInvocationPayloadDetail(json, true, payload.OriginalUtf8Bytes),
                    maxPayloadBytes);
            }

            return new ToolInvocationPayloadDetail(json, true, payload.OriginalUtf8Bytes);
        }
        catch
        {
            return null;
        }
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
            var length = JsonSerializer.SerializeToUtf8Bytes(envelope, SerializerOptions).Length;
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
