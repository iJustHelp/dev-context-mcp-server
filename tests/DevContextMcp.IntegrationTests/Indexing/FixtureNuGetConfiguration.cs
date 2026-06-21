using System.Text.Json;

namespace DevContextMcp.IntegrationTests.Indexing;

internal static class FixtureNuGetConfiguration
{
    public static string CreatePackageFolder(
        string root,
        params PackagePolicy[] packages)
    {
        var path = Path.Combine(root, "nugets");
        Directory.CreateDirectory(path);
        foreach (var file in Directory.EnumerateFiles(
                     path,
                     "*.json",
                     SearchOption.AllDirectories))
        {
            File.Delete(file);
        }

        foreach (var package in packages)
        {
            var environmentPath = Path.Combine(path, package.Environment);
            Directory.CreateDirectory(environmentPath);
            var fileName = $"{package.PackageId}.json";
            File.WriteAllText(
                Path.Combine(environmentPath, fileName),
                JsonSerializer.Serialize(new
                {
                    package.Delete,
                    package.Environment,
                    package.PackageId,
                    package.MaxVersionsPerPackage,
                    package.Versions
                }));
        }

        return path;
    }

    internal sealed record PackagePolicy(
        string Environment,
        string PackageId,
        int MaxVersionsPerPackage = 10,
        string? Versions = null,
        bool Delete = false);
}
