using DevContextMcp.Indexer.Core.Infrastructure;

namespace DevContextMcp.Infrastructure.Indexer.NuGet;

// No-op authentication provider for anonymous NuGet sources; validates arguments only.
internal sealed class AnonymousNuGetSourceAuthenticationProvider :
    INuGetSourceAuthenticationProvider
{
    public void Configure(object packageSource, string sourceName)
    {
        ArgumentNullException.ThrowIfNull(packageSource);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceName);
    }
}
