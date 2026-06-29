using System.Text;
using System.Text.Json;
using DevContextMcp.Server.Core.Contracts.Common;
using DevContextMcp.Server.Core.Models.Analytics;

namespace DevContextMcp.Server.Tools;

/// <summary>
/// Builds bounded metadata-only detail payloads for not-ok analytics events.
/// </summary>
internal static class ToolInvocationDetailCapture
{
    private const int MaxResultDetailBytes = 4_096;

    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    internal static string? SerializeResultDetail<TResponse>(
        TResponse? response,
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
            var detail = ExtractDetail(response);
            if (detail is null)
            {
                return null;
            }

            return SerializeBounded(detail);
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

    private static ToolInvocationResultDetail? ExtractDetail<TResponse>(TResponse? response)
    {
        if (response is null)
        {
            return null;
        }

        var responseType = response.GetType();
        if (!responseType.IsAssignableTo(typeof(ToolResponse<object>)))
        {
            return null;
        }

        var errors = ReadErrors(responseType, response);
        var resolvedContext = ReadResolvedContext(responseType, response);
        if (errors.Count == 0 && resolvedContext is null)
        {
            return null;
        }

        return new ToolInvocationResultDetail(errors, resolvedContext);
    }

    private static IReadOnlyList<ToolInvocationErrorDetail> ReadErrors(
        Type responseType,
        object response)
    {
        var errorsProperty = responseType.GetProperty(nameof(ToolResponse<object>.Errors));
        if (errorsProperty?.GetValue(response) is not IEnumerable<ToolError> errors)
        {
            return [];
        }

        return errors
            .Select(error => new ToolInvocationErrorDetail(error.Code, error.Message))
            .ToArray();
    }

    private static ToolInvocationResolvedContextDetail? ReadResolvedContext(
        Type responseType,
        object response)
    {
        var contextProperty = responseType.GetProperty(nameof(ToolResponse<object>.ResolvedContext));
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

    private static string SerializeBounded(ToolInvocationResultDetail detail)
    {
        var errors = detail.Errors.ToList();
        while (true)
        {
            var candidate = new ToolInvocationResultDetail(errors, detail.ResolvedContext);
            var json = JsonSerializer.Serialize(candidate, SerializerOptions);
            if (Encoding.UTF8.GetByteCount(json) <= MaxResultDetailBytes || errors.Count == 0)
            {
                return json;
            }

            errors.RemoveAt(errors.Count - 1);
        }
    }
}
