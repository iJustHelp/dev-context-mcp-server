using DevContextMcp.Indexer.Core.Infrastructure;
using DevContextMcp.Indexer.Core.Models;
using DevContextMcp.Indexer.Core.Services;
using Moq;

namespace DevContextMcp.UnitTests.Indexing;

public sealed class IndexCoordinatorTests
{
    private const string DatabasePath = "fixture.db";

    private readonly Mock<IIndexingConfigurationProvider> _configurationProvider = new();
    private readonly Mock<IPackageSourceClient> _sourceClient = new();
    private readonly Mock<IPackageProcessor> _packageProcessor = new();
    private readonly Mock<IDocumentationSourceReader> _documentationReader = new();
    private readonly Mock<IIndexStore> _indexStore = new();
    private readonly IndexCoordinator _target;

    public IndexCoordinatorTests()
    {
        _target = new IndexCoordinator(
            configurationProvider: _configurationProvider.Object,
            sourceClient: _sourceClient.Object,
            packageProcessor: _packageProcessor.Object,
            documentationReader: _documentationReader.Object,
            indexStore: _indexStore.Object);
    }

    // Purpose: publishes a failed source without applying configured delete tombstones
    [Fact]
    public async Task IndexAllAsync_DiscoveryFails_PublishesFailureWithoutDeleteTombstones()
    {
        // arrange
        var source = CreateSource(
            [new PackageSelectionDefinition(
                PackageId: "Active.Package",
                MaxVersions: 1)],
            ["Deleted.Package"]);
        SetupCommon(CreateSettings(source));
        _sourceClient
            .Setup(client => client.DiscoverAsync(
                It.IsAny<IndexSourceDefinition>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("discovery failed"));
        _indexStore
            .Setup(store => store.PublishSourceAsync(
                databasePath: It.IsAny<string>(),
                source: It.IsAny<IndexSourceDefinition>(),
                startedAt: It.IsAny<DateTimeOffset>(),
                packages: It.IsAny<IReadOnlyList<PackageIndexData>>(),
                errors: It.IsAny<IReadOnlyList<IndexRunError>>(),
                cancellationToken: It.IsAny<CancellationToken>()))
            .ReturnsAsync(EmptyPublishResult());

        // act
        var actual = await _target.IndexAllAsync(CancellationToken.None);

        // assert
        var summary = Assert.Single(actual.Summaries);
        Assert.Equal("failed", summary.Status);
        _configurationProvider.Verify(
            provider => provider.GetSettings(),
            Times.Once);
        _indexStore.Verify(
            store => store.InitializeAsync(
                DatabasePath,
                It.IsAny<CancellationToken>()),
            Times.Once);
        _sourceClient.Verify(
            client => client.DiscoverAsync(
                source,
                It.IsAny<CancellationToken>()),
            Times.Once);
        _indexStore.Verify(
            store => store.PublishSourceAsync(
                databasePath: DatabasePath,
                source: It.Is<IndexSourceDefinition>(published =>
                    published.Name == source.Name
                    && published.DeletedPackageIds.Count == 0),
                startedAt: It.IsAny<DateTimeOffset>(),
                packages: It.Is<IReadOnlyList<PackageIndexData>>(packages =>
                    packages.Count == 0),
                errors: It.Is<IReadOnlyList<IndexRunError>>(errors =>
                    errors.Count == 1
                    && errors[0].Code == "source_discovery_failed"),
                cancellationToken: It.IsAny<CancellationToken>()),
            Times.Once);
        _indexStore.Verify(
            store => store.GetIndexedLibrariesAsync(
                DatabasePath,
                It.IsAny<CancellationToken>()),
            Times.Once);
        _sourceClient.Verify(
            client => client.DownloadAsync(
                source: It.IsAny<IndexSourceDefinition>(),
                package: It.IsAny<PackageVersionCandidate>(),
                limits: It.IsAny<PackageProcessingLimits>(),
                cancellationToken: It.IsAny<CancellationToken>()),
            Times.Never);
        _packageProcessor.Verify(
            processor => processor.ProcessAsync(
                candidate: It.IsAny<PackageVersionCandidate>(),
                package: It.IsAny<DownloadedPackage>(),
                limits: It.IsAny<PackageProcessingLimits>(),
                cancellationToken: It.IsAny<CancellationToken>()),
            Times.Never);
        VerifyDocumentationNotCalled();
        VerifyNoOtherCalls();
    }

    // Purpose: counts discovered package identifiers case-insensitively when downloads fail
    [Fact]
    public async Task IndexAllAsync_DuplicatePackageIdCasing_CountsDistinctPackageIds()
    {
        // arrange
        IReadOnlyList<PackageVersionCandidate> candidates =
        [
            new("Alpha.Package", "1.0.0", true, false, null),
            new("alpha.package", "2.0.0", true, false, null),
            new("Beta.Package", "1.0.0", true, false, null)
        ];
        var source = CreateSource(
            [
                new PackageSelectionDefinition(
                    PackageId: "Alpha.Package",
                    MaxVersions: 2),
                new PackageSelectionDefinition(
                    PackageId: "Beta.Package",
                    MaxVersions: 1)
            ],
            []);
        SetupCommon(CreateSettings(source));
        _sourceClient
            .Setup(client => client.DiscoverAsync(
                It.IsAny<IndexSourceDefinition>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(candidates);
        _sourceClient
            .Setup(client => client.DownloadAsync(
                source: It.IsAny<IndexSourceDefinition>(),
                package: It.IsAny<PackageVersionCandidate>(),
                limits: It.IsAny<PackageProcessingLimits>(),
                cancellationToken: It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("download failed"));
        _indexStore
            .Setup(store => store.PublishSourceAsync(
                databasePath: It.IsAny<string>(),
                source: It.IsAny<IndexSourceDefinition>(),
                startedAt: It.IsAny<DateTimeOffset>(),
                packages: It.IsAny<IReadOnlyList<PackageIndexData>>(),
                errors: It.IsAny<IReadOnlyList<IndexRunError>>(),
                cancellationToken: It.IsAny<CancellationToken>()))
            .ReturnsAsync(EmptyPublishResult());

        // act
        var actual = await _target.IndexAllAsync(CancellationToken.None);

        // assert
        var summary = Assert.Single(actual.Summaries);
        Assert.Equal(2, summary.Discovered);
        Assert.Equal(0, summary.Indexed);
        _configurationProvider.Verify(
            provider => provider.GetSettings(),
            Times.Once);
        _indexStore.Verify(
            store => store.InitializeAsync(
                DatabasePath,
                It.IsAny<CancellationToken>()),
            Times.Once);
        _sourceClient.Verify(
            client => client.DiscoverAsync(
                source,
                It.IsAny<CancellationToken>()),
            Times.Once);
        _sourceClient.Verify(
            client => client.DownloadAsync(
                source: source,
                package: It.IsAny<PackageVersionCandidate>(),
                limits: It.IsAny<PackageProcessingLimits>(),
                cancellationToken: It.IsAny<CancellationToken>()),
            Times.Exactly(candidates.Count));
        _indexStore.Verify(
            store => store.PublishSourceAsync(
                databasePath: DatabasePath,
                source: source,
                startedAt: It.IsAny<DateTimeOffset>(),
                packages: It.Is<IReadOnlyList<PackageIndexData>>(packages =>
                    packages.Count == 0),
                errors: It.Is<IReadOnlyList<IndexRunError>>(errors =>
                    errors.Count == candidates.Count
                    && errors.All(error => error.Code == "package_index_failed")),
                cancellationToken: It.IsAny<CancellationToken>()),
            Times.Once);
        _indexStore.Verify(
            store => store.GetIndexedLibrariesAsync(
                DatabasePath,
                It.IsAny<CancellationToken>()),
            Times.Once);
        _packageProcessor.Verify(
            processor => processor.ProcessAsync(
                candidate: It.IsAny<PackageVersionCandidate>(),
                package: It.IsAny<DownloadedPackage>(),
                limits: It.IsAny<PackageProcessingLimits>(),
                cancellationToken: It.IsAny<CancellationToken>()),
            Times.Never);
        VerifyDocumentationNotCalled();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task IndexAllAsync_DocumentationSucceeds_ReturnsIndexedDocumentPaths()
    {
        var documentationSource = new DocumentationSourceDefinition(
            "docs",
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".md" });
        SetupCommon(new(
            DatabasePath,
            CreateLimits(),
            [],
            documentationSource));
        var documentation = new DocumentationIndexData(
            "hash",
            [
                new ArtifactRecord(
                    Path: "api.md",
                    Kind: "company_document",
                    ContentHash: "a",
                    Size: 1),
                new ArtifactRecord(
                    Path: "nested/design.md",
                    Kind: "company_document",
                    ContentHash: "b",
                    Size: 1)
            ],
            []);
        _documentationReader
            .Setup(reader => reader.ReadAsync(
                documentationSource,
                It.IsAny<PackageProcessingLimits>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(documentation);
        _indexStore
            .Setup(store => store.PublishDocumentationAsync(
                databasePath: DatabasePath,
                source: documentationSource,
                startedAt: It.IsAny<DateTimeOffset>(),
                documentation: documentation,
                cancellationToken: It.IsAny<CancellationToken>()))
            .ReturnsAsync(EmptyPublishResult());

        var actual = await _target.IndexAllAsync(CancellationToken.None);

        Assert.Equal(["api.md", "nested/design.md"], actual.IndexedDocuments);
        var summary = Assert.Single(actual.Summaries);
        Assert.Equal("company-docs", summary.SourceName);
        Assert.Equal(2, summary.Indexed);
    }

    private void SetupCommon(IndexingSettings settings)
    {
        _configurationProvider
            .Setup(provider => provider.GetSettings())
            .Returns(settings);
        _indexStore
            .Setup(store => store.InitializeAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _indexStore
            .Setup(store => store.GetIndexedLibrariesAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    private void VerifyDocumentationNotCalled()
    {
        _documentationReader.Verify(
            reader => reader.ReadAsync(
                It.IsAny<DocumentationSourceDefinition>(),
                It.IsAny<PackageProcessingLimits>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        _indexStore.Verify(
            store => store.PublishDocumentationAsync(
                databasePath: It.IsAny<string>(),
                source: It.IsAny<DocumentationSourceDefinition>(),
                startedAt: It.IsAny<DateTimeOffset>(),
                documentation: It.IsAny<DocumentationIndexData>(),
                cancellationToken: It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private void VerifyNoOtherCalls()
    {
        _configurationProvider.VerifyNoOtherCalls();
        _sourceClient.VerifyNoOtherCalls();
        _packageProcessor.VerifyNoOtherCalls();
        _documentationReader.VerifyNoOtherCalls();
        _indexStore.VerifyNoOtherCalls();
    }

    private static IndexingSettings CreateSettings(
        IndexSourceDefinition source) =>
        new(DatabasePath, CreateLimits(), [source]);

    private static IndexSourceDefinition CreateSource(
        IReadOnlyList<PackageSelectionDefinition> packages,
        IReadOnlyList<string> deletedPackageIds) =>
        new(
            "test",
            "test",
            "fixture",
            packages,
            deletedPackageIds,
            10);

    private static PackageProcessingLimits CreateLimits() =>
        new(
            1,
            1,
            1,
            1,
            1,
            1,
            TimeSpan.FromSeconds(1));

    private static IndexPublishResult EmptyPublishResult() =>
        new(0, 0, [], [], []);
}
