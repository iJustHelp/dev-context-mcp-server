namespace DevContextMcp.Indexer.Core.Infrastructure;

/// <summary>
/// Configures authentication on a NuGet package source before it is queried.
/// </summary>
public interface INuGetSourceAuthenticationProvider
{
    void Configure(object packageSource, string sourceName);
}
