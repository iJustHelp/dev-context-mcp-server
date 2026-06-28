namespace DevContextMcp.Server.Core.Contracts.ResolveLibrary;

/// <summary>
/// Request to resolve an indexed NuGet package from a name or concept.
/// </summary>
public sealed record ResolveLibraryRequest(
    string Query,
    int Limit = 10,
    string? Environment = null);
