namespace DevContextMcp.Server.Core.Models.Analytics;

/// <summary>
/// Call count grouped by tool-level result status.
/// </summary>
public sealed record ToolResultBreakdownItem(string ToolResultStatus, long Count);
