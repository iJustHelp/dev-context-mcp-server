using System.Text.Json;
using DevContextMcp.Server.Core.Contracts.Common;
using DevContextMcp.Server.Core.Contracts.ResolveLibrary;

namespace DevContextMcp.IntegrationTests.Mcp;

public sealed class MissingIndexInvocationTests
{
    [Fact]
    public async Task NuGetToolReturnsStructuredIndexUnavailableResponse()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var missingPath = Path.Combine(
            Path.GetTempPath(),
            $"missing-mcp-index-{Guid.NewGuid():N}",
            "docs.db");
        await using var server = await McpTestServer.StartAsync(
            timeout.Token,
            new Dictionary<string, string?>
            {
                ["DevContextMcp:DatabasePath"] = missingPath
            });

        var result = await server.Client.CallToolAsync(
            "resolve_library",
            new Dictionary<string, object?> { ["query"] = "customer" },
            cancellationToken: timeout.Token);
        var response = result.StructuredContent!.Value.Deserialize<ResolveLibraryResponse>(
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Equal(ToolResultStatus.NotFound, response!.Status);
        Assert.Equal("index_unavailable", Assert.Single(response.Errors).Code);
        Assert.False(File.Exists(missingPath));
    }
}
