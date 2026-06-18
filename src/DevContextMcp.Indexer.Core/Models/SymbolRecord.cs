namespace DevContextMcp.Indexer.Core.Models;

// A public API symbol extracted from a package's assemblies, with its signature and documentation link.
public sealed record SymbolRecord(
    string Namespace,
    string FullyQualifiedName,
    string Kind,
    string Signature,
    string? ContainingType,
    string AssemblyPath,
    string? TargetFramework,
    string? XmlDocumentationMember);
