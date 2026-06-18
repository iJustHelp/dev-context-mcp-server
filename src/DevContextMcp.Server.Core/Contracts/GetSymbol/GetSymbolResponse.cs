using System.Text.Json.Serialization;
using DevContextMcp.Server.Core.Contracts.Common;

namespace DevContextMcp.Server.Core.Contracts.GetSymbol;

/// <summary>
/// Response for the get_symbol tool.
/// </summary>
public sealed record GetSymbolResponse : ToolResponse<GetSymbolResult>;

/// <summary>
/// Symbol lookup payload.
/// </summary>
public sealed record GetSymbolResult
{
    [JsonPropertyName("symbol")]
    public SymbolDetails? Symbol { get; init; }

    [JsonPropertyName("candidates")]
    public IReadOnlyList<SymbolDetails> Candidates { get; init; } = [];
}

/// <summary>
/// Full details of a resolved symbol, including signature, documentation, and related members.
/// </summary>
public sealed record SymbolDetails
{
    [JsonPropertyName("fullyQualifiedName")]
    public required string FullyQualifiedName { get; init; }

    [JsonPropertyName("kind")]
    public required string Kind { get; init; }

    [JsonPropertyName("signature")]
    public required string Signature { get; init; }

    [JsonPropertyName("documentation")]
    public string? Documentation { get; init; }

    [JsonPropertyName("assembly")]
    public string? Assembly { get; init; }

    [JsonPropertyName("targetFrameworks")]
    public IReadOnlyList<string> TargetFrameworks { get; init; } = [];

    [JsonPropertyName("citationUri")]
    public string? CitationUri { get; init; }

    [JsonPropertyName("relatedMembers")]
    public IReadOnlyList<RelatedSymbol> RelatedMembers { get; init; } = [];
}

/// <summary>
/// A sibling member related to a resolved symbol (for example, another member of the same type).
/// </summary>
public sealed record RelatedSymbol
{
    [JsonPropertyName("fullyQualifiedName")]
    public required string FullyQualifiedName { get; init; }

    [JsonPropertyName("kind")]
    public required string Kind { get; init; }

    [JsonPropertyName("signature")]
    public required string Signature { get; init; }
}
