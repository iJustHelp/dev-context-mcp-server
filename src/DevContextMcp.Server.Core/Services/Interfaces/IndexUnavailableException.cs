namespace DevContextMcp.Server.Core.Services;

/// <summary>
/// Thrown when the documentation index cannot be opened or is missing/incompatible.
/// </summary>
public sealed class IndexUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException);
