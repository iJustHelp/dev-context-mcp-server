namespace DevContextMcp.Server.Core.Models.Analytics;

/// <summary>
/// Detail persisted for not-ok tool invocations, including bounded request/response payloads.
/// </summary>
public sealed record ToolInvocationResultDetail(
    IReadOnlyList<ToolInvocationErrorDetail> Errors,
    ToolInvocationResolvedContextDetail? ResolvedContext,
    ToolInvocationPayloadDetail? Request = null,
    ToolInvocationPayloadDetail? Response = null);

/// <summary>
/// Stable error code and message captured from a tool response envelope.
/// </summary>
public sealed record ToolInvocationErrorDetail(string Code, string Message);

/// <summary>
/// Resolved library and version context captured from a tool response envelope.
/// </summary>
public sealed record ToolInvocationResolvedContextDetail(
    string? LibraryId,
    string? SourceId,
    string? Environment,
    string? Version,
    string? VersionSelectionReason);
