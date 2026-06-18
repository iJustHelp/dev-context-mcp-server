namespace DevContextMcp.Server.Core.Models;

/// <summary>
/// The version chosen for a library, the reason it was selected, and any warning codes.
/// </summary>
public sealed record VersionResolution(
    IndexedVersionRecord Version,
    string Reason,
    IReadOnlyList<string> WarningCodes);
