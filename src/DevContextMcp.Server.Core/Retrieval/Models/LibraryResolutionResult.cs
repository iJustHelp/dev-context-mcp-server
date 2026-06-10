namespace DevContextMcp.Server.Core.Retrieval.Models;

public sealed record LibraryResolutionResult(
    LibraryResolutionStatus Status,
    ResolvedLibrarySelection? Selection = null);
