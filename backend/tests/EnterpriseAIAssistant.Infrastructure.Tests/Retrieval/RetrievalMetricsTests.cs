namespace EnterpriseAIAssistant.Infrastructure.Tests.Retrieval;

public sealed class RetrievalMetricsTests
{
    private static readonly RetrievalEvaluationSample[] Samples =
    [
        new(
            "company-info.md",
            ["company-info.md", "products.txt"]),

        new(
            "products.txt",
            ["company-info.md", "products.txt"]),

        new(
            "password-reset.md",
            ["company-info.md", "products.txt"])
    ];

    [Fact]
    public void CalculateHitRateAtK_K1_ReturnsExpectedValue()
    {
        var result = RetrievalMetrics.CalculateHitRateAtK(
            Samples,
            k: 1);

        Assert.Equal(1d / 3d, result, precision: 6);
    }

    [Fact]
    public void CalculateHitRateAtK_K2_ReturnsExpectedValue()
    {
        var result = RetrievalMetrics.CalculateHitRateAtK(
            Samples,
            k: 2);

        Assert.Equal(2d / 3d, result, precision: 6);
    }

    [Fact]
    public void CalculateMeanReciprocalRank_ReturnsExpectedValue()
    {
        var result = RetrievalMetrics.CalculateMeanReciprocalRank(
            Samples);

        Assert.Equal(0.5d, result, precision: 6);
    }

    [Fact]
    public void CalculateHitRateAtK_EmptySamples_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            RetrievalMetrics.CalculateHitRateAtK(
                Array.Empty<RetrievalEvaluationSample>(),
                k: 1));
    }

    [Fact]
    public void CalculateHitRateAtK_InvalidK_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RetrievalMetrics.CalculateHitRateAtK(
                Samples,
                k: 0));
    }
}
