namespace EnterpriseAIAssistant.Infrastructure.Tests.Evaluation;

public sealed record RetrievalEvaluationCase(
    string Question,
    IReadOnlySet<string> RelevantChunkIds);