namespace DevContextMcp.Server.Core.Models;

/// <summary>
/// An indexed version of a library, with its listing, prerelease, and deprecation flags.
/// </summary>
public sealed record IndexedVersionRecord(
    string LibraryVersionId,
    string Version,
    bool Listed,
    bool Prerelease,
    bool Deprecated,
    DateTimeOffset? PublishedAt);
