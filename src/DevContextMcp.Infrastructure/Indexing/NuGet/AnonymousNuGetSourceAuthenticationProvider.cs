using DevContextMcp.Infrastructure.Indexing.Abstractions;

namespace DevContextMcp.Infrastructure.Indexing.NuGet;

internal sealed class AnonymousNuGetSourceAuthenticationProvider :
    INuGetSourceAuthenticationProvider
{
    public void Configure(object packageSource, string sourceName)
    {
        ArgumentNullException.ThrowIfNull(packageSource);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceName);
    }
}
