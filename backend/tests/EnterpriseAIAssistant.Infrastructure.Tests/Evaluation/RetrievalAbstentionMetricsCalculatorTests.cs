using Xunit;

namespace EnterpriseAIAssistant.Infrastructure.Tests.Evaluation;

public sealed class RetrievalAbstentionMetricsCalculatorTests
{
    [Fact]
    public void Calculate_ShouldReturn100Percent_WhenAllNegativeQueriesAreAbstained()
    {
        int[] retrievedCounts = [0, 0];

        var result =
            RetrievalAbstentionMetricsCalculator.Calculate(retrievedCounts);

        Assert.Equal(2, result.NegativeQueryCount);
        Assert.Equal(2, result.AbstainedQueryCount);
        Assert.Equal(0, result.QueriesWithResults);
        Assert.Equal(1.0, result.AbstentionRate);
    }

    [Fact]
    public void Calculate_ShouldReturn50Percent_WhenHalfOfNegativeQueriesAreAbstained()
    {
        int[] retrievedCounts = [0, 3];

        var result =
            RetrievalAbstentionMetricsCalculator.Calculate(retrievedCounts);

        Assert.Equal(2, result.NegativeQueryCount);
        Assert.Equal(1, result.AbstainedQueryCount);
        Assert.Equal(1, result.QueriesWithResults);
        Assert.Equal(0.5, result.AbstentionRate);
    }

    [Fact]
    public void Calculate_ShouldReturnZeroPercent_WhenNoNegativeQueryIsAbstained()
    {
        int[] retrievedCounts = [2, 4];

        var result =
            RetrievalAbstentionMetricsCalculator.Calculate(retrievedCounts);

        Assert.Equal(2, result.NegativeQueryCount);
        Assert.Equal(0, result.AbstainedQueryCount);
        Assert.Equal(2, result.QueriesWithResults);
        Assert.Equal(0.0, result.AbstentionRate);
    }

    [Fact]
    public void Calculate_ShouldThrow_WhenDatasetIsEmpty()
    {
        int[] retrievedCounts = [];

        Assert.Throws<ArgumentException>(() =>
            RetrievalAbstentionMetricsCalculator.Calculate(retrievedCounts));
    }

    [Fact]
    public void Calculate_ShouldThrow_WhenRetrievedCountIsNegative()
    {
        int[] retrievedCounts = [0, -1];

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RetrievalAbstentionMetricsCalculator.Calculate(retrievedCounts));
    }

    [Fact]
    public void Calculate_ShouldThrow_WhenDatasetIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            RetrievalAbstentionMetricsCalculator.Calculate(null!));
    }
}