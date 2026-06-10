using System.IO.Pipelines;
using DevContextMcp.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DevContextMcp.IntegrationTests.Mcp;

internal sealed class McpTestServer : IAsyncDisposable
{
    private readonly IHost _host;
    private readonly McpTestServerBuilder _ownedPipes;

    private McpTestServer(IHost host, McpClient client, McpTestServerBuilder ownedPipes)
    {
        _host = host;
        Client = client;
        _ownedPipes = ownedPipes;
    }

    public McpClient Client { get; }

    public static async Task<McpTestServer> StartAsync(
        CancellationToken cancellationToken,
        IReadOnlyDictionary<string, string?>? configurationValues = null)
    {
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(
            new HostApplicationBuilderSettings
            {
                Args = [],
                DisableDefaults = true
            });

        var values = new Dictionary<string, string?>
            {
                ["DevContextMcp:DatabasePath"] = "data/docs.db"
            };
        if (configurationValues is not null)
        {
            foreach (var (key, value) in configurationValues)
            {
                values[key] = value;
            }
        }

        builder.Configuration.AddInMemoryCollection(values);

        builder.Logging.ClearProviders();
        builder.Services.AddDevContextMcpCore(builder.Configuration);

        var server = new McpTestServerBuilder(builder);
        var host = server.BuildHost();
        await host.StartAsync(cancellationToken);

        var client = await McpClient.CreateAsync(
            new StreamClientTransport(
                server.ClientToServerWriter,
                server.ServerToClientReader),
            cancellationToken: cancellationToken);

        return new McpTestServer(host, client, server);
    }

    public async ValueTask DisposeAsync()
    {
        await Client.DisposeAsync();
        await _host.StopAsync(TimeSpan.FromSeconds(5));
        _host.Dispose();
        await _ownedPipes.DisposeAsync();
    }

    private sealed class McpTestServerBuilder : IAsyncDisposable
    {
        private readonly Pipe _clientToServer = new();
        private readonly Pipe _serverToClient = new();
        private readonly HostApplicationBuilder _builder;

        public McpTestServerBuilder(HostApplicationBuilder builder)
        {
            _builder = builder;
        }

        public Stream ClientToServerWriter => _clientToServer.Writer.AsStream();

        public Stream ServerToClientReader => _serverToClient.Reader.AsStream();

        public IHost BuildHost()
        {
            _builder.Services.AddMcpServer()
                .WithStreamServerTransport(
                    _clientToServer.Reader.AsStream(),
                    _serverToClient.Writer.AsStream())
                .WithDevContextMcpTools();

            return _builder.Build();
        }

        public async ValueTask DisposeAsync()
        {
            await _clientToServer.Writer.CompleteAsync();
            await _clientToServer.Reader.CompleteAsync();
            await _serverToClient.Writer.CompleteAsync();
            await _serverToClient.Reader.CompleteAsync();
        }
    }
}
