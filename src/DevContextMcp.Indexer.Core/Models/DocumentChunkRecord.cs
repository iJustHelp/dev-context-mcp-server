namespace DevContextMcp.Indexer.Core.Models;

/// <summary>
/// A searchable chunk of document text, addressed by path, member, and ordinal.
/// </summary>
public sealed record DocumentChunkRecord(
    string Path,
    string Kind,
    string? MemberName,
    int Ordinal,
    string Content,
    string ContentHash);
