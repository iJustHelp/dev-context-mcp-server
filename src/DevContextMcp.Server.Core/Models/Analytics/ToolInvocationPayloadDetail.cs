namespace DevContextMcp.Server.Core.Models.Analytics;

/// <summary>
/// Size-bounded JSON snapshot of a tool request or response payload.
/// </summary>
public sealed record ToolInvocationPayloadDetail(
    string Json,
    bool Truncated,
    int OriginalUtf8Bytes);
