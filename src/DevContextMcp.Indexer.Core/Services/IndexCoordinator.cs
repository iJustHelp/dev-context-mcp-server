using DevContextMcp.Indexer.Core.Infrastructure;
using DevContextMcp.Indexer.Core.Models;

namespace DevContextMcp.Indexer.Core.Services;

/// <summary>
/// Coordinates a full indexing run across configured package sources and documentation,
/// publishing each result to the index store and aggregating the run summaries.
/// </summary>
internal sealed class IndexCoordinator(
    IIndexingConfigurationProvider configurationProvider,
    IPackageSourceClient sourceClient,
    IPackageProcessor packageProcessor,
    IDocumentationSourceReader documentationReader,
    IIndexStore indexStore) : IIndexCoordinator
{
    private sealed record DocumentationIndexResult(
        IndexRunSummary Summary,
        IReadOnlyList<string> Paths);

    public async Task<IndexRunResult> IndexAllAsync(
        CancellationToken cancellationToken)
    {
        var settings = configurationProvider.GetSettings();
        await indexStore.InitializeAsync(settings.DatabasePath, cancellationToken);

        var summaries = new List<IndexRunSummary>(
            settings.Sources.Count + (settings.Documentation is null ? 0 : 1));
        IReadOnlyList<string> indexedDocuments = [];
        foreach (var source in settings.Sources)
        {
            summaries.Add(await IndexSourceAsync(settings, source, cancellationToken));
        }

        if (settings.Documentation is not null)
        {
            var documentationResult = await IndexDocumentationAsync(
                settings,
                settings.Documentation,
                cancellationToken);
            summaries.Add(documentationResult.Summary);
            indexedDocuments = documentationResult.Paths;
        }

        var indexedLibraries = await indexStore.GetIndexedLibrariesAsync(
            settings.DatabasePath,
            cancellationToken);

        return new IndexRunResult(summaries, indexedLibraries, indexedDocuments);
    }

    private async Task<DocumentationIndexResult> IndexDocumentationAsync(
        IndexingSettings settings,
        DocumentationSourceDefinition source,
        CancellationToken cancellationToken)
    {
        var startedAt = DateTimeOffset.UtcNow;
        try
        {
            var documentation = await documentationReader.ReadAsync(
                source,
                settings.Limits,
                cancellationToken);
            var publish = await indexStore.PublishDocumentationAsync(
                databasePath: settings.DatabasePath,
                source: source,
                startedAt: startedAt,
                documentation: documentation,
                cancellationToken: cancellationToken);

            return new DocumentationIndexResult(
                new IndexRunSummary(
                    SourceName: "company-docs",
                    Status: "succeeded",
                    StartedAt: startedAt,
                    CompletedAt: DateTimeOffset.UtcNow,
                    Discovered: documentation.Artifacts.Count,
                    Indexed: documentation.Artifacts.Count,
                    Changed: publish.Changed,
                    Unchanged: publish.Unchanged,
                    Added: publish.Added,
                    Updated: publish.Updated,
                    Deleted: publish.Deleted,
                    Errors: [],
                    Environment: "Documents"),
                documentation.Artifacts
                    .Select(artifact => artifact.Path)
                    .ToArray());
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            var error = new IndexRunError(
                Code: "documentation_index_failed",
                Message: exception.Message,
                PackageId: "company-docs",
                Version: null);
            return new DocumentationIndexResult(
                new IndexRunSummary(
                    SourceName: "company-docs",
                    Status: "failed",
                    StartedAt: startedAt,
                    CompletedAt: DateTimeOffset.UtcNow,
                    Discovered: 0,
                    Indexed: 0,
                    Changed: 0,
                    Unchanged: 0,
                    Added: [],
                    Updated: [],
                    Deleted: [],
                    Errors: [error],
                    Environment: "Documents"),
                []);
        }
    }

    private async Task<IndexRunSummary> IndexSourceAsync(
        IndexingSettings settings,
        IndexSourceDefinition source,
        CancellationToken cancellationToken)
    {
        var startedAt = DateTimeOffset.UtcNow;
        IReadOnlyList<PackageVersionCandidate> candidates = [];

        try
        {
            if (source.Packages.Count > 0)
            {
                candidates = await sourceClient.DiscoverAsync(source, cancellationToken);
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
                source: source with { DeletedPackageIds = [] },
                startedAt: startedAt,
                packages: [],
                errors: [discoveryError],
                cancellationToken: cancellationToken);

            return new IndexRunSummary(
                SourceName: source.Name,
                Environment: source.Environment,
                Status: "failed",
                StartedAt: startedAt,
                CompletedAt: completedAt,
                Discovered: 0,
                Indexed: 0,
                Changed: 0,
                Unchanged: 0,
                Added: [],
                Updated: [],
                Deleted: [],
                Errors: [discoveryError]);
        }

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
            cancellationToken: cancellationToken);

        var status = indexedPackages.Count == 0 && errors.Count > 0
            ? "failed"
            : errors.Count > 0 ? "partial_success" : "succeeded";

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
            Errors: errors);
    }
}
