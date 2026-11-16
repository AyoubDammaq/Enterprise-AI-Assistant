using Xunit;

namespace EnterpriseAIAssistant.Infrastructure.Tests.Evaluation;

public sealed class RetrievalMetricsCalculatorTests
{
    [Fact]
    public void Calculate_ShouldReturnExpectedMetrics_WhenRelevantChunksAreRetrieved()
    {
        IReadOnlyList<IReadOnlyList<string>> retrieved =
        [
            new[] { "chunk-a", "chunk-b" },
            new[] { "chunk-c", "chunk-d" }
        ];

        IReadOnlyList<IReadOnlySet<string>> relevant =
        [
            new HashSet<string> { "chunk-a" },
            new HashSet<string> { "chunk-d" }
        ];

        var result = RetrievalMetricsCalculator.Calculate(
            retrieved, relevant, k: 2);

        Assert.Equal(0.5, result.PrecisionAtK);
        Assert.Equal(1.0, result.RecallAtK);
        Assert.Equal(1.0, result.HitRateAtK);
        Assert.Equal(0.75, result.MeanReciprocalRank);
    }

    [Fact]
    public void Calculate_ShouldUseFixedKDenominator_WhenFewerChunksAreRetrieved()
    {
        IReadOnlyList<IReadOnlyList<string>> retrieved =
        [
            new[] { "chunk-a" }
        ];

        IReadOnlyList<IReadOnlySet<string>> relevant =
        [
            new HashSet<string> { "chunk-a" }
        ];

        var result = RetrievalMetricsCalculator.Calculate(
            retrieved, relevant, k: 3);

        Assert.Equal(1.0 / 3.0, result.PrecisionAtK, 10);
        Assert.Equal(1.0, result.RecallAtK);
        Assert.Equal(1.0, result.HitRateAtK);
        Assert.Equal(1.0, result.MeanReciprocalRank);
    }

    [Fact]
    public void Calculate_ShouldReturnZeroMetrics_WhenNoRelevantChunkIsRetrieved()
    {
        IReadOnlyList<IReadOnlyList<string>> retrieved =
        [
            new[] { "chunk-x", "chunk-y" }
        ];

        IReadOnlyList<IReadOnlySet<string>> relevant =
        [
            new HashSet<string> { "chunk-a" }
        ];

        var result = RetrievalMetricsCalculator.Calculate(
            retrieved, relevant, k: 2);

        Assert.Equal(0.0, result.PrecisionAtK);
        Assert.Equal(0.0, result.RecallAtK);
        Assert.Equal(0.0, result.HitRateAtK);
        Assert.Equal(0.0, result.MeanReciprocalRank);
    }

    [Fact]
    public void Calculate_ShouldNotCountDuplicateRelevantChunksTwice()
    {
        IReadOnlyList<IReadOnlyList<string>> retrieved =
        [
            new[] { "chunk-a", "chunk-a" }
        ];

        IReadOnlyList<IReadOnlySet<string>> relevant =
        [
            new HashSet<string> { "chunk-a" }
        ];

        var result = RetrievalMetricsCalculator.Calculate(
            retrieved, relevant, k: 2);

        Assert.Equal(0.5, result.PrecisionAtK);
        Assert.Equal(1.0, result.RecallAtK);
        Assert.Equal(1.0, result.HitRateAtK);
        Assert.Equal(1.0, result.MeanReciprocalRank);
    }

    [Fact]
    public void Calculate_ShouldThrow_WhenKIsLessThanOne()
    {
        IReadOnlyList<IReadOnlyList<string>> retrieved =
        [
            new[] { "chunk-a" }
        ];

        IReadOnlyList<IReadOnlySet<string>> relevant =
        [
            new HashSet<string> { "chunk-a" }
        ];

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RetrievalMetricsCalculator.Calculate(
                retrieved, relevant, k: 0));
    }

    [Fact]
    public void Calculate_ShouldThrow_WhenDatasetSizesDoNotMatch()
    {
        IReadOnlyList<IReadOnlyList<string>> retrieved =
        [
            new[] { "chunk-a" }
        ];

        IReadOnlyList<IReadOnlySet<string>> relevant =
        [
            new HashSet<string> { "chunk-a" },
            new HashSet<string> { "chunk-b" }
        ];

        Assert.Throws<ArgumentException>(() =>
            RetrievalMetricsCalculator.Calculate(
                retrieved, relevant, k: 1));
    }

    [Fact]
    public void Calculate_ShouldThrow_WhenDatasetIsEmpty()
    {
        IReadOnlyList<IReadOnlyList<string>> retrieved =
            Array.Empty<IReadOnlyList<string>>();

        IReadOnlyList<IReadOnlySet<string>> relevant =
            Array.Empty<IReadOnlySet<string>>();

        Assert.Throws<ArgumentException>(() =>
            RetrievalMetricsCalculator.Calculate(
                retrieved, relevant, k: 1));
    }

    [Fact]
    public void Calculate_ShouldThrow_WhenAQuestionHasNoRelevantChunks()
    {
        IReadOnlyList<IReadOnlyList<string>> retrieved =
        [
            new[] { "chunk-a" }
        ];

        IReadOnlyList<IReadOnlySet<string>> relevant =
        [
            new HashSet<string>()
        ];

        Assert.Throws<ArgumentException>(() =>
            RetrievalMetricsCalculator.Calculate(
                retrieved, relevant, k: 1));
    }
}
