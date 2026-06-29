namespace DevContextMcp.Server.Core.Models.Analytics;

/// <summary>
/// Metadata-only detail persisted for not-ok tool invocations.
/// </summary>
public sealed record ToolInvocationResultDetail(
    IReadOnlyList<ToolInvocationErrorDetail> Errors,
    ToolInvocationResolvedContextDetail? ResolvedContext);

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
