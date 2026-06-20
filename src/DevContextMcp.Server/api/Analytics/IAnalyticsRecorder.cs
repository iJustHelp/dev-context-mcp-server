using DevContextMcp.Server.Core.Models.Analytics;

namespace DevContextMcp.Server.api.Analytics;

/// <summary>
/// Non-blocking sink for captured tool-invocation events. Implementations must never
/// block or throw into the tool invocation path.
/// </summary>
internal interface IAnalyticsRecorder
{
    /// <summary>
    /// Whether capture is active. When false, callers should skip building events.
    /// </summary>
    bool Enabled { get; }

    /// <summary>
    /// Enqueues an event for background persistence, dropping it under back-pressure.
    /// </summary>
    void Record(ToolInvocationRecord record);
}
