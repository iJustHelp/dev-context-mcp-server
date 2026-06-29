using System.Text.Json;
using DevContextMcp.Server.Tools;

namespace DevContextMcp.IntegrationTests.Mcp;

public sealed class ToolDiscoveryTests
{
    [Fact]
    public async Task ClientDiscoversExpectedToolsAndSchemas()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        await using var server = await McpTestServer.StartAsync(timeout.Token);

        var tools = await server.Client.ListToolsAsync(cancellationToken: timeout.Token);

        Assert.Equal(5, tools.Count);
        Assert.Equal(
            ToolRegistrationCatalog.ExpectedNames.Order(StringComparer.Ordinal),
            tools.Select(tool => tool.Name).Order(StringComparer.Ordinal));
        Assert.DoesNotContain(tools, tool => tool.Name == $"find_api_{"operation"}");

        foreach (var tool in tools)
        {
            Assert.False(string.IsNullOrWhiteSpace(tool.Description));
            Assert.NotEqual(JsonValueKind.Undefined, tool.JsonSchema.ValueKind);
            Assert.True(tool.ReturnJsonSchema.HasValue);
            Assert.NotEqual(JsonValueKind.Undefined, tool.ReturnJsonSchema.Value.ValueKind);
        }

        var resolveLibrary = tools.Single(tool => tool.Name == "resolve_library");
        var inputSchema = resolveLibrary.JsonSchema.GetRawText();
        Assert.Contains("query", inputSchema, StringComparison.Ordinal);
        Assert.Contains("limit", inputSchema, StringComparison.Ordinal);
        Assert.Contains("environment", inputSchema, StringComparison.Ordinal);

        var outputSchema = resolveLibrary.ReturnJsonSchema!.Value.GetRawText();
        Assert.Contains("status", outputSchema, StringComparison.Ordinal);
        Assert.Contains("errors", outputSchema, StringComparison.Ordinal);
        Assert.Contains("environment", outputSchema, StringComparison.Ordinal);
        Assert.DoesNotContain("sourceId", outputSchema, StringComparison.Ordinal);
    }
}
