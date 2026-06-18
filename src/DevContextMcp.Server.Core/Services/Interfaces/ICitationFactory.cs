namespace DevContextMcp.Server.Core.Services;

// Builds stable MCP resource URIs for artifacts, symbols, and documentation.
public interface ICitationFactory
{
    string ArtifactUri(string source, string packageId, string version, string path);

    string SymbolUri(string source, string packageId, string version, string qualifiedName);

    string DocumentationUri(string path);
}
