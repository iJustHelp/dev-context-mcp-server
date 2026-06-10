namespace DevContextMcp.Server.Core.Retrieval.Services;

public sealed class IndexUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException);
