namespace DevContextMcp.Server.Core.Models;

/// <summary>
/// The status of a library resolution attempt.
/// </summary>
public enum LibraryResolutionStatus
{
    Resolved,
    EnvironmentNotFound,
    LibraryNotFound
}
