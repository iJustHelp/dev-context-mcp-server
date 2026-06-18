namespace DevContextMcp.Server.Core.Models;

// The outcome of resolving a library reference: a status and, when resolved, the selection.
public sealed record LibraryResolutionResult(
    LibraryResolutionStatus Status,
    ResolvedLibrarySelection? Selection = null);
