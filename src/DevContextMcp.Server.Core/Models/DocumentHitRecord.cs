namespace DevContextMcp.Server.Core.Models;

/// <summary>
/// A document chunk that matched a documentation search, with its relevance rank.
/// </summary>
public sealed record DocumentHitRecord(
    string Path,
    string Kind,
    string? MemberName,
    string Content,
    string ContentHash,
    double Rank);
