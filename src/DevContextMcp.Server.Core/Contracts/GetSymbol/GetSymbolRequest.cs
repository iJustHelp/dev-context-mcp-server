namespace DevContextMcp.Server.Core.Contracts.GetSymbol;

/// <summary>
/// Request to find and describe a type or member in an indexed library.
/// </summary>
public sealed record GetSymbolRequest(
    string LibraryId,
    string Symbol,
    string? Version = null,
    string? TargetFramework = null,
    string? ProjectVersion = null,
    bool IncludePrerelease = false);
