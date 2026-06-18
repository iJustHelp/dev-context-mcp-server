namespace DevContextMcp.Indexer.Core.Models;

/// <summary>
/// A library that has been indexed, grouped by the environments it appears in.
/// </summary>
public sealed record IndexedLibrary(
    string PackageId,
    IReadOnlyList<IndexedLibraryEnvironment> Environments);

/// <summary>
/// The set of indexed versions of a library within a single environment.
/// </summary>
public sealed record IndexedLibraryEnvironment(
    string Environment,
    IReadOnlyList<string> Versions);
