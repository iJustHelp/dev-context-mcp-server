namespace DevContextMcp.Server.Core.Models.Analytics;

/// <summary>
/// Range-scoped call counts grouped by analytics user.
/// </summary>
public sealed record UserBreakdownResponse(IReadOnlyList<UserBreakdownItem> Users);
