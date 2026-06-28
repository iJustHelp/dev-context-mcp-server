using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using DevContextMcp.Server.Core.Models.Analytics;
using DevContextMcp.Server.Core.Models.Context;
using Microsoft.Data.Sqlite;

namespace DevContextMcp.IntegrationTests.Context;

public sealed class ContextEndpointsTests
{
    [Fact]
    public async Task GetContext_WithSeededIndex_ReturnsInventory()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        var databasePath = Path.Combine(
            Path.GetTempPath(),
            $"context-endpoint-{Guid.NewGuid():N}",
            "docs.db");
        await SeedAsync(databasePath, timeout.Token);

        await using var server = await HttpServerProcess.StartAsync(databasePath, timeout.Token);

        var response = await server.Client.GetFromJsonAsync<IndexedContextResponse>(
            "/api/context",
            timeout.Token);

        Assert.NotNull(response);
        Assert.Equal(1, response.Totals.NuGetLibraryCount);
        Assert.Equal("Demo.Cities", Assert.Single(response.Nugets).PackageId);
    }

    [Fact]
    public async Task GetContext_WhenIndexMissing_ReturnsApiError()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        var databasePath = Path.Combine(
            Path.GetTempPath(),
            $"missing-context-index-{Guid.NewGuid():N}",
            "docs.db");

        await using var server = await HttpServerProcess.StartAsync(databasePath, timeout.Token);

        using var response = await server.Client.GetAsync("/api/context", timeout.Token);
        var error = await response.Content.ReadFromJsonAsync<ApiError>(
            cancellationToken: timeout.Token);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.NotNull(error);
        Assert.Contains("documentation index does not exist", error.Error);
        Assert.False(File.Exists(databasePath));
    }

    private static async Task SeedAsync(
        string databasePath,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
        await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = false,
            ForeignKeys = true
        }.ToString());
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            PRAGMA user_version = 1;
            CREATE TABLE sources (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                environment TEXT NOT NULL,
                service_index TEXT NOT NULL,
                kind TEXT NOT NULL DEFAULT 'nuget',
                last_indexed_at TEXT NULL
            );
            CREATE TABLE libraries (
                id TEXT PRIMARY KEY,
                source_id TEXT NOT NULL REFERENCES sources(id) ON DELETE CASCADE,
                package_id TEXT NOT NULL,
                normalized_package_id TEXT NOT NULL,
                kind TEXT NOT NULL DEFAULT 'nuget',
                display_name TEXT NULL
            );
            CREATE TABLE library_versions (
                id TEXT PRIMARY KEY,
                library_id TEXT NOT NULL REFERENCES libraries(id) ON DELETE CASCADE,
                version TEXT NOT NULL,
                content_hash TEXT NOT NULL,
                title TEXT NULL,
                description TEXT NULL,
                summary TEXT NULL,
                authors TEXT NULL,
                tags TEXT NULL,
                project_url TEXT NULL,
                repository_url TEXT NULL,
                is_listed INTEGER NOT NULL,
                is_prerelease INTEGER NOT NULL,
                is_deprecated INTEGER NOT NULL,
                published_at TEXT NULL,
                indexed_at TEXT NOT NULL
            );
            CREATE TABLE artifacts (
                id TEXT PRIMARY KEY,
                library_version_id TEXT NOT NULL REFERENCES library_versions(id) ON DELETE CASCADE,
                path TEXT NOT NULL,
                kind TEXT NOT NULL,
                content_hash TEXT NOT NULL,
                size INTEGER NOT NULL,
                content TEXT NULL
            );
            CREATE TABLE document_chunks (
                id TEXT PRIMARY KEY,
                library_version_id TEXT NOT NULL REFERENCES library_versions(id) ON DELETE CASCADE,
                artifact_id TEXT NULL REFERENCES artifacts(id) ON DELETE SET NULL,
                path TEXT NOT NULL,
                kind TEXT NOT NULL,
                member_name TEXT NULL,
                ordinal INTEGER NOT NULL,
                content TEXT NOT NULL,
                content_hash TEXT NOT NULL
            );
            CREATE TABLE symbols (
                id TEXT PRIMARY KEY,
                library_version_id TEXT NOT NULL REFERENCES library_versions(id) ON DELETE CASCADE,
                namespace TEXT NOT NULL,
                fully_qualified_name TEXT NOT NULL,
                kind TEXT NOT NULL,
                signature TEXT NOT NULL,
                containing_type TEXT NULL,
                assembly_path TEXT NOT NULL,
                target_framework TEXT NULL,
                xml_documentation_member TEXT NULL
            );
            INSERT INTO sources (id, name, environment, service_index, kind, last_indexed_at)
            VALUES
                ('nuget-source', 'Demo Feed', 'qa', 'file://demo', 'nuget', '2026-06-19T11:00:00.0000000+00:00');
            INSERT INTO libraries (id, source_id, package_id, normalized_package_id, kind, display_name)
            VALUES
                ('nuget-library', 'nuget-source', 'Demo.Cities', 'demo.cities', 'nuget', NULL);
            INSERT INTO library_versions (
                id, library_id, version, content_hash, is_listed, is_prerelease,
                is_deprecated, indexed_at)
            VALUES
                ('nuget-version', 'nuget-library', '1.1.0', 'hash-110', 1, 0, 0, '2026-06-19T11:00:00.0000000+00:00');
            INSERT INTO artifacts (id, library_version_id, path, kind, content_hash, size, content)
            VALUES
                ('artifact-110', 'nuget-version', 'lib/net8.0/Demo.Cities.xml', 'xml_documentation', 'b', 20, 'content');
            INSERT INTO document_chunks (id, library_version_id, artifact_id, path, kind, member_name, ordinal, content, content_hash)
            VALUES
                ('doc-110', 'nuget-version', 'artifact-110', 'lib/net8.0/Demo.Cities.xml', 'xml_documentation', 'M:Demo.Cities.CityService.Get', 0, 'body', 'e');
            INSERT INTO symbols (
                id, library_version_id, namespace, fully_qualified_name, kind,
                signature, assembly_path, target_framework)
            VALUES
                ('sym-110', 'nuget-version', 'Demo.Cities', 'Demo.Cities.CityService.Get', 'method', 'string Get()', 'Demo.Cities.dll', 'net8.0');
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private sealed class HttpServerProcess : IAsyncDisposable
    {
        private readonly Process _process;
        private readonly ConcurrentQueue<string> _logs;

        private HttpServerProcess(Process process, HttpClient client, ConcurrentQueue<string> logs)
        {
            _process = process;
            Client = client;
            _logs = logs;
        }

        public HttpClient Client { get; }

        public static async Task<HttpServerProcess> StartAsync(
            string databasePath,
            CancellationToken cancellationToken)
        {
            var port = GetAvailablePort();
            var baseAddress = new Uri($"http://127.0.0.1:{port}");
            var logs = new ConcurrentQueue<string>();
            var listening = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var process = CreateHostProcess(port, databasePath);

            process.ErrorDataReceived += (_, eventArgs) =>
            {
                if (eventArgs.Data is null)
                {
                    return;
                }

                logs.Enqueue(eventArgs.Data);
                if (eventArgs.Data.Contains("Now listening on", StringComparison.OrdinalIgnoreCase))
                {
                    listening.TrySetResult();
                }
            };
            process.OutputDataReceived += (_, eventArgs) =>
            {
                if (eventArgs.Data is not null)
                {
                    logs.Enqueue(eventArgs.Data);
                }
            };

            Assert.True(process.Start());
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            var exited = process.WaitForExitAsync(cancellationToken);
            var completed = await Task.WhenAny(listening.Task, exited);
            Assert.False(
                completed == exited,
                $"HTTP host exited before listening.{Environment.NewLine}{string.Join(Environment.NewLine, logs)}");
            await listening.Task.WaitAsync(cancellationToken);

            return new HttpServerProcess(
                process,
                new HttpClient { BaseAddress = baseAddress },
                logs);
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
                await _process.WaitForExitAsync(CancellationToken.None);
            }

            _process.Dispose();
            GC.KeepAlive(_logs);
        }

        private static Process CreateHostProcess(int port, string databasePath)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                WorkingDirectory = RepositoryRoot(),
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add(HostAssemblyPath());
            startInfo.ArgumentList.Add(
                $"--DevContextMcp:McpUrl=http://127.0.0.1:{port}/mcp");
            startInfo.ArgumentList.Add(
                $"--DevContextMcp:DatabasePath={databasePath}");
            return new Process { StartInfo = startInfo };
        }

        private static int GetAvailablePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();

            try
            {
                return ((IPEndPoint)listener.LocalEndpoint).Port;
            }
            finally
            {
                listener.Stop();
            }
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
}
