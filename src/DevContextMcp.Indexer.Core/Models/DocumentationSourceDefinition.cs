namespace DevContextMcp.Indexer.Core.Models;

/// <summary>
/// Defines a documentation source: the root folder to scan and the file extensions to include.
/// </summary>
public sealed record DocumentationSourceDefinition(
    string RootPath,
    IReadOnlySet<string> Extensions);
