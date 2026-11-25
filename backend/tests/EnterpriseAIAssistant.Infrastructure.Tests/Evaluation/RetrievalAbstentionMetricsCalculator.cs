namespace EnterpriseAIAssistant.Infrastructure.Tests.Evaluation;

public static class RetrievalAbstentionMetricsCalculator
{
    public static RetrievalAbstentionMetrics Calculate(
        IReadOnlyList<int> retrievedResultCounts)
    {
        ArgumentNullException.ThrowIfNull(retrievedResultCounts);

        if (retrievedResultCounts.Count == 0)
        {
            throw new ArgumentException(
                "Dataset cannot be empty.",
                nameof(retrievedResultCounts));
        }

        if (retrievedResultCounts.Any(count => count < 0))
        {
            throw new ArgumentOutOfRangeException(
                nameof(retrievedResultCounts),
                "Retrieved result counts cannot be negative.");
        }

        int abstainedCount = retrievedResultCounts.Count(count => count == 0);

        int queriesWithResults =
            retrievedResultCounts.Count(count => count > 0);

        double abstentionRate =
            (double)abstainedCount / retrievedResultCounts.Count;

        return new RetrievalAbstentionMetrics(
            NegativeQueryCount: retrievedResultCounts.Count,
            AbstainedQueryCount: abstainedCount,
            QueriesWithResults: queriesWithResults,
            AbstentionRate: abstentionRate);
    }
}