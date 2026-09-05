namespace FunctionalProgramming.Services;

/// <summary>
/// Calculates aggregates across fixed-size, adjacent message windows.
/// </summary>
public static class MessageWindowAnalytics
{
    /// <summary>
    /// Counts contiguous windows containing <paramref name="windowLength"/> values whose sum equals <paramref name="target"/>.
    /// </summary>
    public static int CountWindowsWithSum(IReadOnlyList<decimal> amounts, int windowLength, decimal target)
    {
        ArgumentNullException.ThrowIfNull(amounts);

        if (windowLength <= 0 || windowLength > amounts.Count)
        {
            return 0;
        }

        decimal sum = 0;
        for (var index = 0; index < windowLength; index++)
        {
            sum += amounts[index];
        }

        var count = sum == target ? 1 : 0;

        for (var index = windowLength; index < amounts.Count; index++)
        {
            sum += amounts[index] - amounts[index - windowLength];
            if (sum == target)
            {
                count++;
            }
        }

        return count;
    }
}
