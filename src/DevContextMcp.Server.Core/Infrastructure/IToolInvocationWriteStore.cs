using DevContextMcp.Server.Core.Models.Analytics;

namespace DevContextMcp.Server.Core.Infrastructure;

/// <summary>
/// Append-only persistence for captured tool-invocation analytics events.
/// </summary>
public interface IToolInvocationWriteStore
{
    /// <summary>
    /// Persists a batch of events, creating the analytics database and schema on first use.
    /// </summary>
    Task AppendAsync(
        string databasePath,
        IReadOnlyList<ToolInvocationRecord> records,
        CancellationToken cancellationToken);
}
