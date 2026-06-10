namespace DevContextMcp.Infrastructure.Indexing.Abstractions;

internal interface IContentHasher
{
    string Hash(ReadOnlySpan<byte> content);

    Task<string> HashAsync(Stream stream, CancellationToken cancellationToken);
}
