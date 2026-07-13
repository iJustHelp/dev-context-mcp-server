using DevContextMcp.Indexer.Core.Infrastructure;
using DevContextMcp.Indexer.Core.Models;

namespace DevContextMcp.Indexer.Core.Services;

/// <summary>
/// Coordinates a full indexing run across configured package sources,
/// publishing each result to the index store and aggregating the run summaries.
/// </summary>
internal sealed class IndexCoordinator(
    IIndexingConfigurationProvider configurationProvider,
    IPackageSourceClient sourceClient,
    IPackageProcessor packageProcessor,
    IIndexStore indexStore) : IIndexCoordinator
{
    public async Task<IndexRunResult> IndexAllAsync(
        CancellationToken cancellationToken)
    {
        var settings = configurationProvider.GetSettings();
        await indexStore.InitializeAsync(settings.DatabasePath, cancellationToken);

        var summaries = new List<IndexRunSummary>(settings.Sources.Count);
        foreach (var source in settings.Sources)
        {
            summaries.Add(await IndexSourceAsync(settings, source, cancellationToken));
        }

        var indexedLibraries = await indexStore.GetIndexedLibrariesAsync(
            settings.DatabasePath,
            cancellationToken);

        return new IndexRunResult(summaries, indexedLibraries);
    }

    private async Task<IndexRunSummary> IndexSourceAsync(
        IndexingSettings settings,
        IndexSourceDefinition source,
        CancellationToken cancellationToken)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var discovery = new PackageDiscovery([], []);

        try
        {
            if (source.Packages.Count > 0)
            {
                discovery = await sourceClient.DiscoverAsync(source, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            var discoveryError = new IndexRunError("source_discovery_failed", exception.Message);
            var completedAt = DateTimeOffset.UtcNow;
            await indexStore.PublishSourceAsync(
                databasePath: settings.DatabasePath,
                source: source,
                startedAt: startedAt,
                packages: [],
                errors: [discoveryError],
                pruneRemovedPackages: false,
                cancellationToken: cancellationToken);

            return new IndexRunSummary(
                SourceName: source.Name,
                Environment: source.Environment,
                Status: IndexRunStatus.Failed,
                StartedAt: startedAt,
                CompletedAt: completedAt,
                Discovered: 0,
                Indexed: 0,
                Changed: 0,
                Unchanged: 0,
                Added: [],
                Updated: [],
                Deleted: [],
                Errors: [discoveryError],
                Packages: source.Packages
                    .Select(package => new IndexRunPackageStatus(
                        PackageId: package.PackageId,
                        Environment: source.Environment,
                        AvailableVersions: 0,
                        IndexedVersions: [],
                        Status: IndexRunPackageStatusKind.Failed,
                        Error: discoveryError.Message))
                    .ToArray());
        }

        var candidates = discovery.Candidates;
        var indexedPackages = new List<PackageIndexData>(candidates.Count);
        var errors = new List<IndexRunError>();

        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await using var package = await sourceClient.DownloadAsync(
                    source: source,
                    package: candidate,
                    limits: settings.Limits,
                    cancellationToken: cancellationToken);

                indexedPackages.Add(await packageProcessor.ProcessAsync(
                    candidate: candidate,
                    package: package,
                    limits: settings.Limits,
                    cancellationToken: cancellationToken));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                errors.Add(new IndexRunError(
                    Code: "package_index_failed",
                    Message: exception.Message,
                    PackageId: candidate.PackageId,
                    Version: candidate.Version));
            }
        }

        var publish = await indexStore.PublishSourceAsync(
            databasePath: settings.DatabasePath,
            source: source,
            startedAt: startedAt,
            packages: indexedPackages,
            errors: errors,
            pruneRemovedPackages: true,
            cancellationToken: cancellationToken);

        var status = IndexRunStatuses.FromOutcome(indexedPackages.Count, errors.Count);

        return new IndexRunSummary(
            SourceName: source.Name,
            Environment: source.Environment,
            Status: status,
            StartedAt: startedAt,
            CompletedAt: DateTimeOffset.UtcNow,
            Discovered: candidates
                .Select(candidate => candidate.PackageId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count(),
            Indexed: indexedPackages.Count,
            Changed: publish.Changed,
            Unchanged: publish.Unchanged,
            Added: publish.Added,
            Updated: publish.Updated,
            Deleted: publish.Deleted,
            Errors: errors,
            Packages: BuildPackageStatuses(
                source,
                discovery.Availability,
                indexedPackages,
                errors,
                publish));
    }

    private static IReadOnlyList<IndexRunPackageStatus> BuildPackageStatuses(
        IndexSourceDefinition source,
        IReadOnlyList<PackageAvailability> availability,
        IReadOnlyList<PackageIndexData> indexedPackages,
        IReadOnlyList<IndexRunError> errors,
        IndexPublishResult publish)
    {
        var availableById = availability.ToDictionary(
            item => item.PackageId,
            item => item.AvailableVersions,
            StringComparer.OrdinalIgnoreCase);
        var indexedById = indexedPackages
            .GroupBy(package => package.PackageId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)group
                    .Select(package => package.Version)
                    .OrderByDescending(version => version, StringComparer.Ordinal)
                    .ToArray(),
                StringComparer.OrdinalIgnoreCase);
        var errorById = errors
            .Where(error => error.PackageId is not null)
            .GroupBy(error => error.PackageId!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Message, StringComparer.OrdinalIgnoreCase);
        var addedIds = publish.Added
            .Select(package => package.PackageId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var updatedIds = publish.Updated
            .Select(package => package.PackageId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var statuses = new List<IndexRunPackageStatus>();
        foreach (var package in source.Packages)
        {
            var available = availableById.GetValueOrDefault(package.PackageId, 0);
            var indexedVersions = indexedById.GetValueOrDefault(package.PackageId, []);
            var error = errorById.GetValueOrDefault(package.PackageId);
            var status = available == 0
                ? IndexRunPackageStatusKind.NotFound
                : indexedVersions.Count == 0 && error is not null
                    ? IndexRunPackageStatusKind.Failed
                    : addedIds.Contains(package.PackageId)
                        ? IndexRunPackageStatusKind.Added
                        : updatedIds.Contains(package.PackageId)
                            ? IndexRunPackageStatusKind.Updated
                            : IndexRunPackageStatusKind.Unchanged;

            statuses.Add(new IndexRunPackageStatus(
                PackageId: package.PackageId,
                Environment: source.Environment,
                AvailableVersions: available,
                IndexedVersions: indexedVersions,
                Status: status,
                Error: error));
        }

        var configuredIds = source.Packages
            .Select(package => package.PackageId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var deletedId in publish.Deleted
                     .Select(package => package.PackageId)
                     .Where(id => !configuredIds.Contains(id))
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            statuses.Add(new IndexRunPackageStatus(
                PackageId: deletedId,
                Environment: source.Environment,
                AvailableVersions: 0,
                IndexedVersions: [],
                Status: IndexRunPackageStatusKind.Deleted,
                Error: null));
        }

        return statuses;
    }
}
