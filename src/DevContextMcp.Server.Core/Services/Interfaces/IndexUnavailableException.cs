namespace DevContextMcp.Server.Core.Services;

// Thrown when the documentation index cannot be opened or is missing/incompatible.
public sealed class IndexUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException);
