namespace DevContextMcp.Server.Core.Services;

// Selects a prefix of items that fits within configured count and byte-size budgets.
public interface IResponseBudget
{
    IReadOnlyList<T> Take<T>(
        IReadOnlyList<T> values,
        int maximumCount,
        int maximumBytes,
        Func<T, string> textSelector,
        out bool truncated);
}
