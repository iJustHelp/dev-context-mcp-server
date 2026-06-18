namespace DevContextMcp.Indexer.Core.Models;

// Defines a documentation source: the root folder to scan and the file extensions to include.
public sealed record DocumentationSourceDefinition(
    string RootPath,
    IReadOnlySet<string> Extensions);
