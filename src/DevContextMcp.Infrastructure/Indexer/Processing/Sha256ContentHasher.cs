using System.Security.Cryptography;
using DevContextMcp.Indexer.Core.Infrastructure;

namespace DevContextMcp.Infrastructure.Indexer.Processing;

/// <summary>
/// Computes lowercase hex SHA-256 content hashes for byte spans and streams.
/// </summary>
internal sealed class Sha256ContentHasher : IContentHasher
{
    public string Hash(ReadOnlySpan<byte> content) =>
        Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();

    public async Task<string> HashAsync(Stream stream, CancellationToken cancellationToken)
    {
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
