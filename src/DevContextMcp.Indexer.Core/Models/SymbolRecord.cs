namespace DevContextMcp.Indexer.Core.Models;

/// <summary>
/// A public API symbol extracted from a package's assemblies, with its signature and documentation link.
/// </summary>
public sealed record SymbolRecord(
    string Namespace,
    string FullyQualifiedName,
    string Kind,
    string Signature,
    string? ContainingType,
    string AssemblyPath,
    string? TargetFramework,
    string? XmlDocumentationMember);
