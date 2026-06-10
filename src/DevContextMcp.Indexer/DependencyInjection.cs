using DevContextMcp.Indexer.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DevContextMcp.Indexer;

/// <summary>
/// Indexer use-case registration.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddIndexer(this IServiceCollection services)
    {
        services.AddSingleton<IIndexCoordinator, IndexCoordinator>();
        return services;
    }
}
