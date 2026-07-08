using System.Text.Json;
using DevContextMcp.Server.Core.Models.Analytics;

namespace DevContextMcp.Infrastructure.Analytics;

internal static class AnalyticsDetailJson
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    internal static ToolInvocationResultDetail? Deserialize(string? json)
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
}
