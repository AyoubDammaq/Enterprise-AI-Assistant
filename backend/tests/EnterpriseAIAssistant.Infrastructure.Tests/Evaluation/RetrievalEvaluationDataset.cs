namespace EnterpriseAIAssistant.Infrastructure.Tests.Evaluation;

public static class RetrievalEvaluationDataset
{
    // Baseline annotations for the current corpus and chunking strategy.
    // Q6 and Q7 are negative questions and are intentionally excluded.
    public static IReadOnlyList<RetrievalEvaluationCase> PositiveCases { get; } =
    [
        new(
            "How can a user reset their Demo Portal password?",
            new HashSet<string>(StringComparer.Ordinal)
            {
                "support-faq.md::chunk-0000"
            }),

        new(
            "How long is a Demo Portal account locked after failed login attempts?",
            new HashSet<string>(StringComparer.Ordinal)
            {
                "support-faq.md::chunk-0000"
            }),

        new(
            "What is the default low-stock threshold in Atlas Inventory?",
            new HashSet<string>(StringComparer.Ordinal)
            {
                "product-guide.md::chunk-0000"
            }),

        new(
            "Which user role can modify inventory settings and manage users?",
            new HashSet<string>(StringComparer.Ordinal)
            {
                "product-guide.md::chunk-0001"
            }),

        new(
            "How should a user resolve error INV-001?",
            new HashSet<string>(StringComparer.Ordinal)
            {
                "troubleshooting.md::chunk-0000"
            })
    ];
}
