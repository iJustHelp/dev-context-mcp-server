namespace DevContextMcp.Server.Core.Models;

/// <summary>
/// A symbol that matched a symbol search, with its signature, documentation, and match tier.
/// </summary>
public sealed record SymbolHitRecord(
    string FullyQualifiedName,
    string Kind,
    string Signature,
    string? ContainingType,
    string AssemblyPath,
    string? TargetFramework,
    string? XmlDocumentationMember,
    string? Documentation,
    int MatchTier);
