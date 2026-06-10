namespace DevContextMcp.Infrastructure.Indexing.Abstractions;

internal interface INuGetSourceAuthenticationProvider
{
    void Configure(object packageSource, string sourceName);
}
