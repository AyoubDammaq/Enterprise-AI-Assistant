namespace EnterpriseAIAssistant.Infrastructure.Tests.Retrieval;

/// <summary>
/// Represents one retrieval evaluation sample.
/// </summary>
public sealed record RetrievalEvaluationSample(
    string ExpectedSource,
    IReadOnlyList<string> RetrievedSources);

/// <summary>
/// Calculates retrieval evaluation metrics independently
/// from the embedding model and the LLM.
/// </summary>
public static class RetrievalMetrics
{
    public static double CalculateHitRateAtK(
        IReadOnlyList<RetrievalEvaluationSample> samples,
        int k)
    {
        ArgumentNullException.ThrowIfNull(samples);

        if (samples.Count == 0)
            throw new ArgumentException(
                "At least one evaluation sample is required.",
                nameof(samples));

        if (k <= 0)
            throw new ArgumentOutOfRangeException(nameof(k));

        var hits = samples.Count(sample =>
            sample.RetrievedSources
                .Take(k)
                .Contains(
                    sample.ExpectedSource,
                    StringComparer.OrdinalIgnoreCase));

        return (double)hits / samples.Count;
    }

    public static double CalculateMeanReciprocalRank(
        IReadOnlyList<RetrievalEvaluationSample> samples)
    {
        ArgumentNullException.ThrowIfNull(samples);

        if (samples.Count == 0)
            throw new ArgumentException(
                "At least one evaluation sample is required.",
                nameof(samples));

        var reciprocalRankSum = 0d;

        foreach (var sample in samples)
        {
            var rank = sample.RetrievedSources
                .ToList()
                .FindIndex(source =>
                    string.Equals(
                        source,
                        sample.ExpectedSource,
                        StringComparison.OrdinalIgnoreCase));

            if (rank >= 0)
                reciprocalRankSum += 1d / (rank + 1);
        }

        return reciprocalRankSum / samples.Count;
    }
}
