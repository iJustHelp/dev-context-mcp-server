namespace DevContextMcp.Indexer.Core.Abstractions;

public interface INuGetSourceAuthenticationProvider
{
    void Configure(object packageSource, string sourceName);
}
