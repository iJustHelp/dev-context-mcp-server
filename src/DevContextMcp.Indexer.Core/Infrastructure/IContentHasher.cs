namespace DevContextMcp.Indexer.Core.Infrastructure;

/// <summary>
/// Computes stable content hashes for byte spans and streams.
/// </summary>
public interface IContentHasher
{
    string Hash(ReadOnlySpan<byte> content);

    Task<string> HashAsync(Stream stream, CancellationToken cancellationToken);
}
