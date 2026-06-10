namespace DevContextMcp.Server.Core.Diagnostics;

/// <summary>
/// Result from a local dependency readiness check.
/// </summary>
public sealed record LocalDependencyCheckResult(bool Succeeded, string Name, string Message);
