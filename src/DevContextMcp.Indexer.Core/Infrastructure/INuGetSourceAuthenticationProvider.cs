namespace DevContextMcp.Indexer.Core.Infrastructure;

// Configures authentication on a NuGet package source before it is queried.
public interface INuGetSourceAuthenticationProvider
{
    void Configure(object packageSource, string sourceName);
}
