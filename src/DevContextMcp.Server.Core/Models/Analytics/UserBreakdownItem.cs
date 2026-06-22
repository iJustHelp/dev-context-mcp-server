namespace DevContextMcp.Server.Core.Models.Analytics;

/// <summary>
/// Call count grouped by resolved analytics user.
/// </summary>
public sealed record UserBreakdownItem(string UserName, long Count);
