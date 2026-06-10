namespace DevContextMcp.Server.Core.Retrieval.Services;

public interface IResponseBudget
{
    IReadOnlyList<T> Take<T>(
        IReadOnlyList<T> values,
        int maximumCount,
        int maximumBytes,
        Func<T, string> textSelector,
        out bool truncated);
}
