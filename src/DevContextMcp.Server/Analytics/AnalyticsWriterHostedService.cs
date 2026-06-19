using DevContextMcp.Server.Configuration;
using DevContextMcp.Server.Core.Infrastructure;
using DevContextMcp.Server.Core.Models.Analytics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevContextMcp.Server.Analytics;

/// <summary>
/// Drains the <see cref="AnalyticsRecorder"/> channel and persists events in batches.
/// Persistence failures are logged but never surface to tool callers.
/// </summary>
internal sealed class AnalyticsWriterHostedService(
    AnalyticsRecorder recorder,
    IToolInvocationWriteStore writeStore,
    IOptions<DevContextMcpOptions> options,
    ILogger<AnalyticsWriterHostedService> logger) : BackgroundService
{
    private const int BatchSize = 200;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var databasePath = Path.GetFullPath(
            options.Value.Analytics.DatabasePath,
            AppContext.BaseDirectory);
        var reader = recorder.Reader;
        var batch = new List<ToolInvocationRecord>(BatchSize);

        try
        {
            while (await reader.WaitToReadAsync(stoppingToken))
            {
                batch.Clear();
                while (batch.Count < BatchSize && reader.TryRead(out var record))
                {
                    batch.Add(record);
                }

                try
                {
                    await writeStore.AppendAsync(databasePath, batch, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    logger.LogError(
                        exception,
                        "Failed to persist {Count} analytics events.",
                        batch.Count);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Host is shutting down; remaining buffered events are discarded.
        }
    }
}
