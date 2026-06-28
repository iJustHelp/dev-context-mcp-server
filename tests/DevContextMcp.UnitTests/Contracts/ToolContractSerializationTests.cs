using System.Text.Json;
using DevContextMcp.Server.Core.Contracts.Common;
using DevContextMcp.Server.Core.Contracts.QueryDocs;
using DevContextMcp.Server.Core.Contracts.ResolveLibrary;

namespace DevContextMcp.UnitTests.Contracts;

public sealed class ToolContractSerializationTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void NotFoundResponseSerializesToExpectedShape()
    {
        var response = new ResolveLibraryResponse
        {
            Status = ToolResultStatus.NotFound,
            Data = new ResolveLibraryResult(),
            Errors =
            [
                new ToolError
                {
                    Code = "library_not_found",
                    Message = "No indexed NuGet package matched 'missing'."
                }
            ]
        };

        var json = JsonSerializer.Serialize(response, SerializerOptions);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal("not_found", root.GetProperty("status").GetString());
        Assert.Empty(root.GetProperty("data").GetProperty("matches").EnumerateArray());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("resolvedContext").ValueKind);
        Assert.False(root.TryGetProperty("evidence", out _));
        Assert.False(root.TryGetProperty("citations", out _));
        Assert.Empty(root.GetProperty("warnings").EnumerateArray());

        var error = Assert.Single(root.GetProperty("errors").EnumerateArray());
        Assert.Equal("library_not_found", error.GetProperty("code").GetString());
        Assert.Equal(
            "No indexed NuGet package matched 'missing'.",
            error.GetProperty("message").GetString());
    }

    [Fact]
    public void RequestAndResponseContractsRoundTrip()
    {
        var request = new QueryDocsRequest(
            LibraryId: "nuget:qa/Company.Customer.Client",
            Question: "How do I register the client?",
            Version: "4.2.0",
            TargetFramework: "net10.0",
            MaxResults: 8);
        var response = new QueryDocsResponse
        {
            Status = ToolResultStatus.Ok,
            Data = new QueryDocsResult(),
            ResolvedContext = new ResolvedContext
            {
                LibraryId = "nuget:qa/Company.Customer.Client",
                Environment = "qa",
                SourceId = "qa-feed",
                Version = "4.2.0",
                VersionSelectionReason = "requested"
            }
        };

        var requestJson = JsonSerializer.Serialize(request, SerializerOptions);
        var responseJson = JsonSerializer.Serialize(response, SerializerOptions);

        var deserializedRequest = JsonSerializer.Deserialize<QueryDocsRequest>(requestJson, SerializerOptions);
        var deserializedResponse = JsonSerializer.Deserialize<QueryDocsResponse>(responseJson, SerializerOptions);

        Assert.Equal(request, deserializedRequest);
        Assert.NotNull(deserializedResponse);
        Assert.Equal(ToolResultStatus.Ok, deserializedResponse.Status);
        Assert.Equal("qa", deserializedResponse.ResolvedContext!.Environment);
        Assert.Equal("4.2.0", deserializedResponse.ResolvedContext!.Version);
    }

    [Fact]
    public void OkQueryDocsResponseOmitsEvidenceAndCitations()
    {
        var response = new QueryDocsResponse
        {
            Status = ToolResultStatus.Ok,
            Data = new QueryDocsResult
            {
                Fragments =
                [
                    new DocumentFragment
                    {
                        Title = "README.md",
                        Text = "Install the package via NuGet.",
                        CitationUri = "nuget://qa/Company.Package/1.0.0/artifact/README.md"
                    }
                ]
            },
            ResolvedContext = new ResolvedContext
            {
                LibraryId = "nuget:qa/Company.Package",
                Environment = "qa",
                SourceId = "qa-feed",
                Version = "1.0.0",
                VersionSelectionReason = "requested"
            }
        };

        var json = JsonSerializer.Serialize(response, SerializerOptions);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal("ok", root.GetProperty("status").GetString());
        Assert.False(root.TryGetProperty("evidence", out _));
        Assert.False(root.TryGetProperty("citations", out _));
        Assert.StartsWith(
            "nuget://",
            root.GetProperty("data").GetProperty("fragments")[0].GetProperty("citationUri").GetString(),
            StringComparison.Ordinal);
    }
}
