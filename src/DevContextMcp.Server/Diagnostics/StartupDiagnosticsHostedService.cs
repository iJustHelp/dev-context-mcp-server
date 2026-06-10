using DevContextMcp.Server.Core.Retrieval.Services;
using DevContextMcp.Server.Configuration;
using DevContextMcp.Server.Tools;
using DevContextMcp.Infrastructure.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevContextMcp.Server.Diagnostics;

internal sealed class StartupDiagnosticsHostedService(
    IOptions<DevContextMcpOptions> options,
    ToolRegistrationCatalog toolCatalog,
    ILocalDependencyCheck localDependencyCheck,
    IResolveLibraryHandler resolveLibraryHandler,
    IQueryDocsHandler queryDocsHandler,
    IGetSymbolHandler getSymbolHandler,
    IListVersionsHandler listVersionsHandler,
    ILogger<StartupDiagnosticsHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _ = resolveLibraryHandler;
        _ = queryDocsHandler;
        _ = getSymbolHandler;
        _ = listVersionsHandler;

        if (!toolCatalog.Names.SequenceEqual(ToolRegistrationCatalog.ExpectedNames, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("The registered MCP tool catalog is incomplete.");
        }

        var localResults = await localDependencyCheck.CheckAsync(
            options.Value.DatabasePath,
            cancellationToken);

        foreach (var result in localResults)
        {
            if (result.Succeeded)
            {
                logger.LogInformation(
                    "Startup diagnostic {CheckName} succeeded: {Message}",
                    result.Name,
                    result.Message);
            }
            else
            {
                logger.LogError(
                    "Startup diagnostic {CheckName} failed: {Message}",
                    result.Name,
                    result.Message);
            }
        }

        if (localResults.Any(result => !result.Succeeded))
        {
            throw new InvalidOperationException("One or more startup diagnostics failed.");
        }

        logger.LogInformation(
            "MCP documentation server startup checks completed. ToolCount={ToolCount}, LocalCheckCount={LocalCheckCount}, DatabasePath={DatabasePath}",
            toolCatalog.Names.Count,
            localResults.Count,
            Path.GetFullPath(options.Value.DatabasePath));
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
