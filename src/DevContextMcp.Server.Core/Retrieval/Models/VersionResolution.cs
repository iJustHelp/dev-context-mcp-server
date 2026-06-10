namespace DevContextMcp.Server.Core.Retrieval.Models;

public sealed record VersionResolution(
    IndexedVersionRecord Version,
    string Reason,
    IReadOnlyList<string> WarningCodes);
