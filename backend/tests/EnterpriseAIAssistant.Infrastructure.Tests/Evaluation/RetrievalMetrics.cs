namespace EnterpriseAIAssistant.Infrastructure.Tests.Evaluation;

public sealed record RetrievalMetrics(
    double PrecisionAtK,
    double RecallAtK,
    double HitRateAtK,
    double MeanReciprocalRank);