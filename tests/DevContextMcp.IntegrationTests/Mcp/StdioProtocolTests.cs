using ModelContextProtocol.Client;

namespace DevContextMcp.IntegrationTests.Mcp;

public sealed class StdioProtocolTests
{
    [Fact]
    public async Task ChildProcessClientConnectsOverStdioAndCapturesLogsOnStandardError()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var stderrLines = new List<string>();

        await using var client = await McpClient.CreateAsync(
            new StdioClientTransport(
                new StdioClientTransportOptions
                {
                    Name = "mcp-doc-server-test",
                    Command = "dotnet",
                    Arguments =
                    [
                        HostAssemblyPath(),
                        "--DevContextMcp:Transport=stdio"
                    ],
                    WorkingDirectory = RepositoryRoot(),
                    ShutdownTimeout = TimeSpan.FromSeconds(10),
                    StandardErrorLines = stderrLines.Add
                }),
            cancellationToken: timeout.Token);

        var tools = await client.ListToolsAsync(cancellationToken: timeout.Token);

        Assert.Contains(tools, tool => tool.Name == "resolve_library");
        Assert.Contains(stderrLines, line => line.Contains("startup checks completed", StringComparison.OrdinalIgnoreCase));
    }

    private static string HostAssemblyPath()
    {
        return Path.Combine(AppContext.BaseDirectory, "DevContextMcp.Server.dll");
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "DevContextMcp.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root was not found.");
    }
}
