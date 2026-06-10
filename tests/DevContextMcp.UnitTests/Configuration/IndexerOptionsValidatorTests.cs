using DevContextMcp.Indexer.Cli.Configuration;
using Microsoft.Extensions.Options;

namespace DevContextMcp.UnitTests.Configuration;

public sealed class IndexerOptionsValidatorTests
{
    private readonly IndexerOptionsValidator _validator = new();

    [Fact]
    public void DefaultOptionsAreValid()
    {
        var result = _validator.Validate(null, new IndexerOptions());

        Assert.Equal(ValidateOptionsResult.Success, result);
    }

    [Fact]
    public void DuplicateSourceNamesFail()
    {
        var result = _validator.Validate(
            null,
            new IndexerOptions
            {
                NuGetSources =
                [
                    new NuGetSourceOptions
                    {
                        Name = "internal",
                        Environment = "qa",
                        ServiceIndex = "https://packages.example/v3/index.json",
                        PackageIds = ["Company.Package"]
                    }
                ],
                RepositorySources =
                [
                    new RepositorySourceOptions
                    {
                        Name = "Internal",
                        RootPath = "repositories/internal"
                    }
                ]
            });

        AssertFailure(result, "configured more than once");
    }

    [Fact]
    public void InvalidSourceAndLimitValuesFail()
    {
        var result = _validator.Validate(
            null,
            new IndexerOptions
            {
                NuGetSources =
                [
                    new NuGetSourceOptions
                    {
                        Name = "internal",
                        Environment = "bad environment",
                        ServiceIndex = "ftp://packages.example/index.json",
                        PackagePrefixes = ["Company.", ""],
                        MaxVersionsPerPackage = 0
                    }
                ],
                Indexing = new IndexingOptions
                {
                    MaxPackageBytes = 0,
                    MaxDocumentBytes = 1
                }
            });

        AssertFailure(result, "ServiceIndex URI");
        AssertFailure(result, "Environment");
        AssertFailure(result, "empty package prefix");
        AssertFailure(result, "MaxPackageBytes");
        AssertFailure(result, "MaxVersionsPerPackage");
    }

    [Fact]
    public void NuGetSourceRequiresASelectionRule()
    {
        var result = _validator.Validate(
            null,
            new IndexerOptions
            {
                NuGetSources =
                [
                    new NuGetSourceOptions
                    {
                        Name = "internal",
                        Environment = "qa",
                        ServiceIndex = "https://packages.example/v3/index.json"
                    }
                ]
            });

        AssertFailure(result, "package prefix or package ID");
    }

    [Fact]
    public void NuGetSourceRequiresEnvironment()
    {
        var result = _validator.Validate(
            null,
            new IndexerOptions
            {
                NuGetSources =
                [
                    new NuGetSourceOptions
                    {
                        Name = "internal",
                        ServiceIndex = "https://packages.example/v3/index.json",
                        PackageIds = ["Company.Package"]
                    }
                ]
            });

        AssertFailure(result, "Environment");
    }

    [Fact]
    public void MultipleSourcesMayShareEnvironment()
    {
        var result = _validator.Validate(
            null,
            new IndexerOptions
            {
                NuGetSources =
                [
                    new NuGetSourceOptions
                    {
                        Name = "qa-primary",
                        Environment = "qa",
                        ServiceIndex = "https://packages.example/primary/v3/index.json",
                        PackageIds = ["Company.Package"]
                    },
                    new NuGetSourceOptions
                    {
                        Name = "qa-secondary",
                        Environment = "QA",
                        ServiceIndex = "https://packages.example/secondary/v3/index.json",
                        PackageIds = ["Company.Package"]
                    }
                ]
            });

        Assert.Equal(ValidateOptionsResult.Success, result);
    }

    private static void AssertFailure(
        ValidateOptionsResult result,
        string expectedText)
    {
        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure =>
            failure.Contains(expectedText, StringComparison.Ordinal));
    }
}
