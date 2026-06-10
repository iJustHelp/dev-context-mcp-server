namespace DevContextMcp.Server.Core.Diagnostics;

/// <summary>
/// Checks local dependencies without contacting configured documentation sources.
/// </summary>
public interface ILocalDependencyCheck
{
    Task<IReadOnlyList<LocalDependencyCheckResult>> CheckAsync(
        string databasePath,
        CancellationToken cancellationToken);
}
