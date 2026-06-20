using DevContextMcp.Infrastructure;
using DevContextMcp.Server.Analytics;
using DevContextMcp.Server.Configuration;
using DevContextMcp.Server.Core;
using DevContextMcp.Server.Core.Models;
using DevContextMcp.Server.Diagnostics;
using DevContextMcp.Server.Resources;
using DevContextMcp.Server.Tools;
using Microsoft.Extensions.Options;

namespace DevContextMcp.Server;

/// <summary>
/// Host service registration.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddDevContextMcpCore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<IValidateOptions<DevContextMcpOptions>, DevContextMcpOptionsValidator>();
        var optionsSection = configuration.GetSection(DevContextMcpOptions.SectionName);
        services.AddOptions<DevContextMcpOptions>()
            .Bind(optionsSection)
            .ValidateOnStart();

        services.AddApplication();
        services.AddRetrievalInfrastructure();
        services.AddSingleton(provider =>
        {
            var value = provider.GetRequiredService<IOptions<DevContextMcpOptions>>().Value;
            var retrieval = value.Retrieval;
            return new RetrievalSettings(
                DatabasePath: Path.GetFullPath(value.DatabasePath, AppContext.BaseDirectory),
                EnvironmentOrder: retrieval.EnvironmentOrder.ToArray(),
                SourceOrder: retrieval.SourceOrder.ToArray(),
                Limits: new RetrievalLimits(
                    DefaultMaxResults: retrieval.DefaultMaxResults,
                    MaxResults: retrieval.MaxResults,
                    MaxResponseBytes: retrieval.MaxResponseBytes,
                    QueryTimeout: retrieval.QueryTimeout,
                    MinimumEvidenceScore: retrieval.MinimumEvidenceScore,
                    AmbiguousSymbolLimit: retrieval.AmbiguousSymbolLimit));
        });
        services.AddSingleton<ToolRegistrationCatalog>();
        services.AddSingleton<ToolInvocationLogger>();
        services.AddHostedService<StartupDiagnosticsHostedService>();
        AddAnalytics(services, optionsSection);

        return services;
    }

    private static void AddAnalytics(
        IServiceCollection services,
        IConfigurationSection optionsSection)
    {
        services.AddSingleton<AnalyticsUserResolver>();

        var enabled = optionsSection
            .GetSection(nameof(DevContextMcpOptions.Analytics))
            .GetValue(nameof(AnalyticsOptions.Enabled), true);
        if (!enabled)
        {
            services.AddSingleton<IAnalyticsRecorder, NullAnalyticsRecorder>();
            return;
        }

        services.AddSingleton<AnalyticsRecorder>();
        services.AddSingleton<IAnalyticsRecorder>(
            provider => provider.GetRequiredService<AnalyticsRecorder>());
        services.AddAnalyticsInfrastructure();
        services.AddHostedService<AnalyticsWriterHostedService>();
    }

    public static IMcpServerBuilder WithDevContextMcpTools(this IMcpServerBuilder builder)
    {
        return builder
            .WithTools<PingTool>()
            .WithTools<ResolveLibraryTool>()
            .WithTools<QueryDocsTool>()
            .WithTools<GetSymbolTool>()
            .WithTools<ListVersionsTool>()
            .WithResources<NuGetResources>()
            .WithResources<DocumentationResources>();
    }
}
