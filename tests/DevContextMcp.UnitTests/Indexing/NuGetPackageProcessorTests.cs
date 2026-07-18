using System.IO.Compression;
using System.Text;
using DevContextMcp.Indexer.Core.Models;
using DevContextMcp.Infrastructure.Indexer.NuGet;

namespace DevContextMcp.UnitTests.Indexing;

public sealed class NuGetPackageProcessorTests
{
    private const string PackageId = "Fixture.Documentation";
    private const string Version = "1.2.3";

    private readonly NuGetPackageProcessor _target = new();

    // Purpose: processes a fixture nupkg into documents, symbols, deps, and frameworks
    [Fact]
    public async Task ProcessAsync_ValidPackage_ReturnsIndexablePackageData()
    {
        // arrange
        var feedDirectory = CreateTempDirectory();
        try
        {
            var packagePath = CreateFixturePackage(feedDirectory);
            await using var package = CreateDownloadedPackage(packagePath);
            var candidate = new PackageVersionCandidate(
                PackageId: PackageId,
                Version: Version,
                IsListed: true,
                IsDeprecated: false,
                PublishedAt: DateTimeOffset.Parse("2024-01-01T00:00:00Z"));

            // act
            var actual = await _target.ProcessAsync(
                candidate,
                package,
                CreateLimits(),
                CancellationToken.None);

            // assert
            Assert.Equal(PackageId, actual.PackageId);
            Assert.Equal(Version, actual.Version);
            Assert.Equal("Fixture Documentation", actual.Title);
            Assert.True(actual.IsListed);
            Assert.False(actual.IsDeprecated);
            Assert.Contains(
                actual.Documents,
                document => document.Kind == "readme"
                    && document.Content.Contains("indexed package behavior", StringComparison.Ordinal));
            Assert.Contains(
                actual.Documents,
                document => document.Kind == "xml_documentation"
                    && document.MemberName == "T:DevContextMcp.Indexer.Core.Models.PackageIndexData");
            Assert.Contains(
                actual.Symbols,
                symbol => symbol.FullyQualifiedName
                    == "DevContextMcp.Indexer.Core.Models.PackageIndexData");
            Assert.Contains(
                actual.Dependencies,
                dependency => dependency.PackageId == "Fixture.Dependency"
                    && dependency.TargetFramework == "net10.0");
            Assert.Contains(actual.TargetFrameworks, framework => framework.Framework == "net10.0");
            Assert.Contains(
                actual.Artifacts,
                artifact => artifact.Kind == "readme" && artifact.Path == "README.md");
            Assert.Contains(
                actual.Artifacts,
                artifact => artifact.Kind == "managed_assembly"
                    && artifact.Path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            TryDeleteDirectory(feedDirectory);
        }
    }

    // Purpose: rejects packages whose archive entries escape the package root
    [Fact]
    public async Task ProcessAsync_PathTraversalPackage_ThrowsInvalidDataException()
    {
        // arrange
        var feedDirectory = CreateTempDirectory();
        try
        {
            var packagePath = CreateUnsafePackage(feedDirectory);
            await using var package = CreateDownloadedPackage(packagePath);
            var candidate = new PackageVersionCandidate(
                PackageId: PackageId,
                Version: Version,
                IsListed: true,
                IsDeprecated: false,
                PublishedAt: null);

            // act
            var actual = await Assert.ThrowsAsync<InvalidDataException>(() =>
                _target.ProcessAsync(
                    candidate,
                    package,
                    CreateLimits(),
                    CancellationToken.None));

            // assert
            Assert.Contains("escape", actual.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            TryDeleteDirectory(feedDirectory);
        }
    }

    private static DownloadedPackage CreateDownloadedPackage(string packagePath) =>
        new(
            filePath: packagePath,
            contentHash: "fixture-content-hash",
            length: new FileInfo(packagePath).Length);

    private static PackageProcessingLimits CreateLimits() => new(
        MaxPackageBytes: 100_000_000,
        MaxDocumentBytes: 10_000_000,
        MaxArchiveEntries: 1_000,
        MaxExtractedBytes: 100_000_000,
        MaxCompressionRatio: 1_000,
        MaxDocumentChars: 4_000,
        MinDocumentChars: 0,
        PackageDownloadTimeout: TimeSpan.FromSeconds(10));

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"nuget-package-processor-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (IOException)
        {
            // Best-effort cleanup for temporary fixture packages.
        }
        catch (UnauthorizedAccessException)
        {
            // Best-effort cleanup for temporary fixture packages.
        }
    }

    private static string CreateFixturePackage(string feedDirectory)
    {
        var packagePath = Path.Combine(feedDirectory, $"{PackageId}.{Version}.nupkg");
        using var file = new FileStream(packagePath, FileMode.Create, FileAccess.Write);
        using var archive = new ZipArchive(file, ZipArchiveMode.Create);
        WriteText(
            archive,
            $"{PackageId}.nuspec",
            $$"""
            <?xml version="1.0" encoding="utf-8"?>
            <package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">
              <metadata>
                <id>{{PackageId}}</id>
                <version>{{Version}}</version>
                <title>Fixture Documentation</title>
                <authors>MCP Tests</authors>
                <description>A deterministic fixture package for documentation indexing.</description>
                <summary>Fixture summary for full text search.</summary>
                <tags>fixture documentation indexing</tags>
                <projectUrl>https://example.invalid/fixture</projectUrl>
                <repository type="git" url="https://example.invalid/repository.git" />
                <readme>README.md</readme>
                <dependencies>
                  <group targetFramework="net10.0">
                    <dependency id="Fixture.Dependency" version="[1.0.0, 2.0.0)" />
                  </group>
                </dependencies>
              </metadata>
            </package>
            """);
        WriteText(
            archive,
            "README.md",
            $"# Fixture Documentation\n\nVersion {Version} explains indexed package behavior.");
        WriteText(
            archive,
            "lib/net10.0/DevContextMcp.Indexer.Core.xml",
            """
            <doc>
              <members>
                <member name="T:DevContextMcp.Indexer.Core.Models.PackageIndexData">
                  <summary>Fixture XML documentation for a public package index record.</summary>
                </member>
              </members>
            </doc>
            """);

        var assemblyPath = typeof(PackageIndexData).Assembly.Location;
        var assemblyEntry = archive.CreateEntry(
            "lib/net10.0/DevContextMcp.Indexer.Core.dll",
            CompressionLevel.NoCompression);
        using var source = File.OpenRead(assemblyPath);
        using var destination = assemblyEntry.Open();
        source.CopyTo(destination);

        return packagePath;
    }

    private static string CreateUnsafePackage(string feedDirectory)
    {
        var packagePath = Path.Combine(feedDirectory, $"{PackageId}.{Version}.nupkg");
        using var file = new FileStream(packagePath, FileMode.Create, FileAccess.Write);
        using var archive = new ZipArchive(file, ZipArchiveMode.Create);
        WriteText(
            archive,
            $"{PackageId}.nuspec",
            $$"""
            <package>
              <metadata>
                <id>{{PackageId}}</id>
                <version>{{Version}}</version>
                <authors>MCP Tests</authors>
                <description>Unsafe replacement fixture.</description>
              </metadata>
            </package>
            """);
        WriteText(archive, "../outside.txt", "This entry must be rejected.");
        return packagePath;
    }

    private static void WriteText(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.NoCompression);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }
}
