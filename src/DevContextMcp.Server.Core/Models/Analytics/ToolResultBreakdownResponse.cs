namespace DevContextMcp.Server.Core.Models.Analytics;

/// <summary>
/// Range-scoped call counts grouped by tool-level result status.
/// </summary>
public sealed record ToolResultBreakdownResponse(IReadOnlyList<ToolResultBreakdownItem> Results);
