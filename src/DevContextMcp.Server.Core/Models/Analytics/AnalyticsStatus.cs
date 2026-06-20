namespace DevContextMcp.Server.Core.Models.Analytics;

/// <summary>
/// The three terminal outcomes recorded for a tool invocation.
/// </summary>
public static class AnalyticsStatus
{
    public const string Success = "success";
    public const string Error = "error";
    public const string Canceled = "canceled";
}
