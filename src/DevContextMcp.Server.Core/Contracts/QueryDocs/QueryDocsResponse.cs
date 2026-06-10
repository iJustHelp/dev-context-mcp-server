using System.Text.Json.Serialization;
using DevContextMcp.Server.Core.Contracts.Common;

namespace DevContextMcp.Server.Core.Contracts.QueryDocs;

/// <summary>
/// Response for the query_docs tool.
/// </summary>
public sealed record QueryDocsResponse : ToolResponse<QueryDocsResult>;

/// <summary>
/// Documentation query payload.
/// </summary>
public sealed record QueryDocsResult
{
    [JsonPropertyName("fragments")]
    public IReadOnlyList<DocumentFragment> Fragments { get; init; } = [];

    [JsonPropertyName("symbols")]
    public IReadOnlyList<SymbolReference> Symbols { get; init; } = [];

    [JsonPropertyName("examples")]
    public IReadOnlyList<UsageExample> Examples { get; init; } = [];
}

public sealed record DocumentFragment
{
    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("text")]
    public required string Text { get; init; }

    [JsonPropertyName("citationUri")]
    public required string CitationUri { get; init; }
}

public sealed record SymbolReference
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("signature")]
    public string? Signature { get; init; }

    [JsonPropertyName("citationUri")]
    public string? CitationUri { get; init; }
}

public sealed record UsageExample
{
    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("code")]
    public required string Code { get; init; }

    [JsonPropertyName("citationUri")]
    public required string CitationUri { get; init; }
}
