namespace DevContextMcp.Server.Core.Retrieval.Services;

public interface ICitationFactory
{
    string ArtifactUri(string source, string packageId, string version, string path);

    string SymbolUri(string source, string packageId, string version, string qualifiedName);
}
