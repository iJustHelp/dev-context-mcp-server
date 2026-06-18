namespace DevContextMcp.Server.Core.Models;

/// <summary>
/// The outcome of resolving a library reference: a status and, when resolved, the selection.
/// </summary>
public sealed record LibraryResolutionResult(
    LibraryResolutionStatus Status,
    ResolvedLibrarySelection? Selection = null);
