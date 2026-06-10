using DevContextMcp.Server.Core.Diagnostics;

namespace DevContextMcp.Infrastructure.Diagnostics;

internal sealed class LocalDependencyCheck : ILocalDependencyCheck
{
    public Task<IReadOnlyList<LocalDependencyCheckResult>> CheckAsync(
        string databasePath,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolvedDatabasePath = Path.GetFullPath(databasePath);
        var parent = Path.GetDirectoryName(resolvedDatabasePath);
        var results = new List<LocalDependencyCheckResult>
        {
            new(true, "database_path_resolves", $"Database path resolves to '{resolvedDatabasePath}'.")
        };

        if (string.IsNullOrWhiteSpace(parent))
        {
            results.Add(new(false, "database_parent_resolves", "Database path does not have a parent directory."));
        }
        else
        {
            _ = Path.GetFullPath(parent);
            results.Add(new(true, "database_parent_resolves", $"Database parent resolves to '{parent}'."));
        }

        return Task.FromResult<IReadOnlyList<LocalDependencyCheckResult>>(results);
    }
}
