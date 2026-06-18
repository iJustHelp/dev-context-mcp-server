using DevContextMcp.Indexer.Core.Infrastructure;

namespace DevContextMcp.Infrastructure.Indexer.NuGet;

/// <summary>
/// No-op authentication provider for anonymous NuGet sources; validates arguments only.
/// </summary>
internal sealed class AnonymousNuGetSourceAuthenticationProvider :
    INuGetSourceAuthenticationProvider
{
    public void Configure(object packageSource, string sourceName)
    {
        ArgumentNullException.ThrowIfNull(packageSource);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceName);
    }
}
