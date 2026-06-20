using DevContextMcp.Server.Core.Models.Analytics;

namespace DevContextMcp.Server.api.Analytics;

/// <summary>
/// No-op recorder registered when analytics is disabled.
/// </summary>
internal sealed class NullAnalyticsRecorder : IAnalyticsRecorder
{
    public bool Enabled => false;

    public void Record(ToolInvocationRecord record)
    {
        // Analytics is disabled; events are discarded.
    }
}
