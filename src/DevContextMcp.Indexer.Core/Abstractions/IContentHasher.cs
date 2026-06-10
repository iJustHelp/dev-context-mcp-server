namespace DevContextMcp.Indexer.Core.Abstractions;

public interface IContentHasher
{
    string Hash(ReadOnlySpan<byte> content);

    Task<string> HashAsync(Stream stream, CancellationToken cancellationToken);
}
