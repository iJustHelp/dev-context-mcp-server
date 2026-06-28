using System.Text.Json;
using DevContextMcp.Indexer;
using DevContextMcp.Indexer.Core.Services;
using DevContextMcp.IntegrationTests.Indexing;
using DevContextMcp.IntegrationTests.Mcp;
using DevContextMcp.Server;
using DevContextMcp.Server.Core.Contracts.Common;
using DevContextMcp.Server.Core.Contracts.ListVersions;
using DevContextMcp.Server.Core.Contracts.QueryDocs;
using DevContextMcp.Server.Core.Contracts.ResolveLibrary;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DevContextMcp.IntegrationTests.Retrieval;

public sealed class QueryDocsCallFixture : IAsyncLifetime
{
    private string _root = "";

    internal McpTestServer Server { get; private set; } = null!;

    public string LibraryId { get; } = $"nuget:test/{FixtureNuGetPackage.PackageId}";

    public async Task InitializeAsync()
    {
        _root = Path.Combine(Path.GetTempPath(), $"mcp-sim-{Guid.NewGuid():N}");
        var feed = Path.Combine(_root, "feed");
        var databasePath = Path.Combine(_root, "index", "docs.db");

        // Standard Markdown blank lines after headings; chunker keeps each heading with its body.
        const string readmeV1 =
            "# Fixture Documentation\n\n" +
            "## Getting Started\n\n" +
            "Install the package via NuGet and call Initialize() to begin using the library.\n" +
            "Configure the options before making any API calls.\n\n" +
            "## Authentication\n\n" +
            "Use AuthToken to authenticate each request. Set the BearerToken property on the client.\n" +
            "Authentication errors return status 401 and should be retried after refreshing the token.\n\n" +
            "## Error Handling\n\n" +
            "All errors are wrapped in McpException with a Code property that identifies the error type.\n" +
            "Transient failures can be retried using the built-in retry policy.";

        const string readmeV2 =
            "# Fixture Documentation\n\n" +
            "## Getting Started\n\n" +
            "Install the package via NuGet and call Initialize() to begin using the library.\n" +
            "Configure the options before making any API calls.\n\n" +
            "## Authentication\n\n" +
            "Use AuthToken to authenticate each request. Set the BearerToken property on the client.\n" +
            "Authentication errors return status 401 and should be retried after refreshing the token.\n\n" +
            "## Error Handling\n\n" +
            "All errors are wrapped in McpException with a Code property that identifies the error type.\n" +
            "Transient failures can be retried using the built-in retry policy.\n\n" +
            "## Version 2.0.0 Streaming API\n\n" +
            "The streaming API is new in version 2.0.0. Call CreateStreamAsync() for real-time updates.\n" +
            "Connect a StreamHandler to receive events as they arrive from the server.";

        FixtureNuGetPackage.Create(feed, "1.2.3", readmeV1);
        FixtureNuGetPackage.Create(feed, "2.0.0", readmeV2);

        var sourcesPath = FixtureNuGetConfiguration.CreatePackageFolder(
            _root,
            new FixtureNuGetConfiguration.PackagePolicy(
                "test",
                FixtureNuGetPackage.PackageId,
                Versions: "2.0.0,1.2.3"));

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DevContextMcp:DatabasePath"] = databasePath,
                ["DevContextMcp:IndexerSource:NugetsPath"] = sourcesPath,
                ["DevContextMcp:NugetPackages:0:Name"] = "test",
                ["DevContextMcp:NugetPackages:0:Environment"] = "test",
                ["DevContextMcp:NugetPackages:0:ServiceIndex"] = feed,
                ["DevContextMcp:NugetPackages:0:MaxPackages"] = "10",
                ["DevContextMcp:Indexing:MaxCompressionRatio"] = "10000"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDevContextMcpCore(configuration);
        services.AddIndexerCli(configuration);
        using var provider = services.BuildServiceProvider(validateScopes: true);
        await provider.GetRequiredService<IIndexCoordinator>()
            .IndexAllAsync(CancellationToken.None);

        Server = await McpTestServer.StartAsync(
            CancellationToken.None,
            new Dictionary<string, string?> { ["DevContextMcp:DatabasePath"] = databasePath });
    }

    public async Task DisposeAsync()
    {
        await Server.DisposeAsync();
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}

public sealed class QueryDocsSimulatedCallsTests(QueryDocsCallFixture fixture)
    : IClassFixture<QueryDocsCallFixture>
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task SimulatedCall_BroadQuestion_ReturnsFragments()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var result = await fixture.Server.Client.CallToolAsync(
            "query_docs",
            new Dictionary<string, object?>
            {
                ["libraryId"] = fixture.LibraryId,
                ["question"] = "getting started"
            },
            cancellationToken: timeout.Token);

        var response = result.StructuredContent!.Value.Deserialize<QueryDocsResponse>(JsonOptions);
        Assert.Equal(ToolResultStatus.Ok, response!.Status);
        Assert.NotEmpty(response.Data!.Fragments);
        Assert.Contains(response.Data.Fragments, f =>
            f.Text.Contains("Initialize", StringComparison.OrdinalIgnoreCase)
            || f.Text.Contains("Install", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SimulatedCall_ApiQuestion_ReturnsRelevantFragment()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var result = await fixture.Server.Client.CallToolAsync(
            "query_docs",
            new Dictionary<string, object?>
            {
                ["libraryId"] = fixture.LibraryId,
                ["question"] = "authentication"
            },
            cancellationToken: timeout.Token);

        var response = result.StructuredContent!.Value.Deserialize<QueryDocsResponse>(JsonOptions);
        Assert.Equal(ToolResultStatus.Ok, response!.Status);
        Assert.Contains(response.Data!.Fragments, f =>
            f.Text.Contains("AuthToken", StringComparison.OrdinalIgnoreCase)
            || f.Text.Contains("BearerToken", StringComparison.OrdinalIgnoreCase)
            || f.Text.Contains("authentication", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SimulatedCall_VersionSpecific_ReturnsCorrectVersionContext()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var result = await fixture.Server.Client.CallToolAsync(
            "query_docs",
            new Dictionary<string, object?>
            {
                ["libraryId"] = fixture.LibraryId,
                ["question"] = "streaming api",
                ["version"] = "2.0.0"
            },
            cancellationToken: timeout.Token);

        var response = result.StructuredContent!.Value.Deserialize<QueryDocsResponse>(JsonOptions);
        Assert.Equal(ToolResultStatus.Ok, response!.Status);
        Assert.Equal("2.0.0", response.ResolvedContext!.Version);
        Assert.Contains(response.Data!.Fragments, f =>
            f.Text.Contains("2.0.0", StringComparison.Ordinal)
            || f.Text.Contains("streaming", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SimulatedCall_UnknownTopic_DoesNotThrow()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var result = await fixture.Server.Client.CallToolAsync(
            "query_docs",
            new Dictionary<string, object?>
            {
                ["libraryId"] = fixture.LibraryId,
                ["question"] = "xyznonexistenttopicabc"
            },
            cancellationToken: timeout.Token);

        var response = result.StructuredContent!.Value.Deserialize<QueryDocsResponse>(JsonOptions);
        Assert.NotNull(response);
        Assert.True(
            response.Status is ToolResultStatus.Ok or ToolResultStatus.InsufficientEvidence,
            $"Expected ok or insufficient_evidence but got {response.Status}");
    }

    [Fact]
    public async Task SimulatedCall_FullAiWorkflow_ResolveListVersionsThenQueryDocs()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var resolveResult = await fixture.Server.Client.CallToolAsync(
            "resolve_library",
            new Dictionary<string, object?>
            {
                ["query"] = FixtureNuGetPackage.PackageId
            },
            cancellationToken: timeout.Token);

        var resolveResponse = resolveResult.StructuredContent!.Value
            .Deserialize<ResolveLibraryResponse>(JsonOptions);
        Assert.Equal(ToolResultStatus.Ok, resolveResponse!.Status);
        var libraryId = Assert.Single(resolveResponse.Data!.Matches).LibraryId;

        var versionsResult = await fixture.Server.Client.CallToolAsync(
            "list_versions",
            new Dictionary<string, object?> { ["libraryId"] = libraryId },
            cancellationToken: timeout.Token);

        var versionsResponse = versionsResult.StructuredContent!.Value
            .Deserialize<ListVersionsResponse>(JsonOptions);
        Assert.Equal(ToolResultStatus.Ok, versionsResponse!.Status);
        Assert.Contains(
            versionsResponse.Data!.Versions,
            version => version.Version == "1.2.3" && version.Indexed);

        var docsResult = await fixture.Server.Client.CallToolAsync(
            "query_docs",
            new Dictionary<string, object?>
            {
                ["libraryId"] = libraryId,
                ["question"] = "error handling retry",
                ["projectVersion"] = "1.2.3"
            },
            cancellationToken: timeout.Token);

        var docsResponse = docsResult.StructuredContent!.Value
            .Deserialize<QueryDocsResponse>(JsonOptions);
        Assert.Equal(ToolResultStatus.Ok, docsResponse!.Status);
        Assert.Equal("1.2.3", docsResponse.ResolvedContext!.Version);
        Assert.NotEmpty(docsResponse.Data!.Fragments);
        Assert.Contains(docsResponse.Data.Fragments, f =>
            f.Text.Contains("McpException", StringComparison.OrdinalIgnoreCase)
            || f.Text.Contains("retry", StringComparison.OrdinalIgnoreCase)
            || f.Text.Contains("error", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SimulatedCall_FullAiWorkflow_ResolveLibraryThenQueryDocs()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        // Step 1: resolve_library (as an AI would, to get the stable libraryId)
        var resolveResult = await fixture.Server.Client.CallToolAsync(
            "resolve_library",
            new Dictionary<string, object?>
            {
                ["query"] = FixtureNuGetPackage.PackageId
            },
            cancellationToken: timeout.Token);

        var resolveResponse = resolveResult.StructuredContent!.Value
            .Deserialize<ResolveLibraryResponse>(JsonOptions);
        Assert.Equal(ToolResultStatus.Ok, resolveResponse!.Status);
        var libraryId = Assert.Single(resolveResponse.Data!.Matches).LibraryId;

        // Step 2: query_docs using the resolved libraryId
        var docsResult = await fixture.Server.Client.CallToolAsync(
            "query_docs",
            new Dictionary<string, object?>
            {
                ["libraryId"] = libraryId,
                ["question"] = "error handling retry"
            },
            cancellationToken: timeout.Token);

        var docsResponse = docsResult.StructuredContent!.Value
            .Deserialize<QueryDocsResponse>(JsonOptions);
        Assert.Equal(ToolResultStatus.Ok, docsResponse!.Status);
        Assert.NotEmpty(docsResponse.Data!.Fragments);
        Assert.Contains(docsResponse.Data.Fragments, f =>
            f.Text.Contains("McpException", StringComparison.OrdinalIgnoreCase)
            || f.Text.Contains("retry", StringComparison.OrdinalIgnoreCase)
            || f.Text.Contains("error", StringComparison.OrdinalIgnoreCase));
    }
}
