namespace DevContextMcp.Server.Core.Models;

// Resolved retrieval settings: database path, environment/source priority order, and limits.
public sealed record RetrievalSettings(
    string DatabasePath,
    IReadOnlyList<string> EnvironmentOrder,
    IReadOnlyList<string> SourceOrder,
    RetrievalLimits Limits);
