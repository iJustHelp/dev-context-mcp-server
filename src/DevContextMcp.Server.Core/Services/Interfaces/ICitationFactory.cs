namespace DevContextMcp.Server.Core.Services;

/// <summary>
/// Builds stable MCP resource URIs for artifacts, symbols, and documentation.
/// </summary>
public interface ICitationFactory
{
    string ArtifactUri(string source, string packageId, string version, string path);

    string SymbolUri(string source, string packageId, string version, string qualifiedName);

    string DocumentationUri(string path);
}
