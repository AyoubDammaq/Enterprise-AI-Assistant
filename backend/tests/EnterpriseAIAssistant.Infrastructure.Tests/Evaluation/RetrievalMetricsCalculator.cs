namespace EnterpriseAIAssistant.Infrastructure.Tests.Evaluation;

public static class RetrievalMetricsCalculator
{
    public static RetrievalMetrics Calculate(
        IReadOnlyList<IReadOnlyList<string>> retrievedChunkIds,
        IReadOnlyList<IReadOnlySet<string>> relevantChunkIds,
        int k)
    {
        ArgumentNullException.ThrowIfNull(retrievedChunkIds);
        ArgumentNullException.ThrowIfNull(relevantChunkIds);

        if (k < 1)
            throw new ArgumentOutOfRangeException(nameof(k));

        if (retrievedChunkIds.Count != relevantChunkIds.Count)
            throw new ArgumentException("Dataset sizes must match.");

        if (retrievedChunkIds.Count == 0)
            throw new ArgumentException("Dataset cannot be empty.");

        double precisionSum = 0;
        double recallSum = 0;
        double hitSum = 0;
        double reciprocalRankSum = 0;

        for (int i = 0; i < retrievedChunkIds.Count; i++)
        {
            var relevant = relevantChunkIds[i];

            if (relevant.Count == 0)
                throw new ArgumentException(
                    "Only positive questions are supported.");

            var retrieved = retrievedChunkIds[i]
                .Take(k)
                .ToArray();

            var relevantRetrieved = retrieved
                .Distinct()
                .Count(relevant.Contains);

            precisionSum += (double)relevantRetrieved / k;

            recallSum += (double)relevantRetrieved / relevant.Count;

            if (relevantRetrieved > 0)
                hitSum++;

            var firstRelevantRank = retrieved
                .Select((id, index) => new { id, index })
                .FirstOrDefault(x => relevant.Contains(x.id));

            if (firstRelevantRank is not null)
            {
                reciprocalRankSum +=
                    1.0 / (firstRelevantRank.index + 1);
            }
        }

        int count = retrievedChunkIds.Count;

        return new RetrievalMetrics(
            PrecisionAtK: precisionSum / count,
            RecallAtK: recallSum / count,
            HitRateAtK: hitSum / count,
            MeanReciprocalRank: reciprocalRankSum / count
        );
    }
}