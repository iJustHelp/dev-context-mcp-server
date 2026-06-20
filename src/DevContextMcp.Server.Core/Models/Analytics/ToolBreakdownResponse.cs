namespace DevContextMcp.Server.Core.Models.Analytics;

/// <summary>
/// Response envelope for the per-tool breakdown endpoint: serializes as { "tools": [...] }.
/// </summary>
public sealed record ToolBreakdownResponse(IReadOnlyList<ToolUsage> Tools);
