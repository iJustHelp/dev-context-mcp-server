namespace DevContextMcp.Server.Core.Models;

/// <summary>
/// Resolved retrieval settings: database path, environment/source priority order, and limits.
/// </summary>
public sealed record RetrievalSettings(
    string DatabasePath,
    IReadOnlyList<string> EnvironmentOrder,
    IReadOnlyList<string> SourceOrder,
    RetrievalLimits Limits);
