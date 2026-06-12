namespace DevContextMcp.Indexer.Core.Models;

public sealed record DocumentationSourceDefinition(
    string RootPath,
    IReadOnlySet<string> Extensions);
