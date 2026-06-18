namespace DevContextMcp.Indexer.Core.Models;

// A library that has been indexed, grouped by the environments it appears in.
public sealed record IndexedLibrary(
    string PackageId,
    IReadOnlyList<IndexedLibraryEnvironment> Environments);

// The set of indexed versions of a library within a single environment.
public sealed record IndexedLibraryEnvironment(
    string Environment,
    IReadOnlyList<string> Versions);
