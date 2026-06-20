namespace DevContextMcp.Server.Core.Models.Analytics;

/// <summary>
/// Error envelope returned with a 400 response: serializes as { "error": "..." }.
/// </summary>
public sealed record ApiError(string Error);
