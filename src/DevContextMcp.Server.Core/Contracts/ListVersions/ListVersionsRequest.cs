namespace DevContextMcp.Server.Core.Contracts.ListVersions;

/// <summary>
/// Request to list indexed versions for a library.
/// </summary>
public sealed record ListVersionsRequest(string LibraryId);
