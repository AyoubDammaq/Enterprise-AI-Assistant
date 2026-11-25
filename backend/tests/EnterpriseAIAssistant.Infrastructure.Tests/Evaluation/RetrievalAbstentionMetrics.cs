namespace EnterpriseAIAssistant.Infrastructure.Tests.Evaluation;

public sealed record RetrievalAbstentionMetrics(
    int NegativeQueryCount,
    int AbstainedQueryCount,
    int QueriesWithResults,
    double AbstentionRate);