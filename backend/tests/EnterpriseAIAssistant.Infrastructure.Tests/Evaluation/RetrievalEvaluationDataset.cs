namespace EnterpriseAIAssistant.Infrastructure.Tests.Evaluation;

public static class RetrievalEvaluationDataset
{
    // Baseline annotations for the current corpus and chunking strategy.
    // These annotations are validated against the actual generated chunks.
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

    // Negative questions: the expected information is absent from the corpus.
    public static IReadOnlyList<string> NegativeQuestions { get; } =
    [
        "What is the monthly subscription price of Atlas Inventory?",
        "Which database engine does Atlas Inventory use in production?"
    ];

    // Single source of truth for all retrieval evaluation questions.
    // Positive cases reference the annotated relevant source.
    // Negative cases have no expected source.
    public static IReadOnlyList<(
        string Id,
        string Question,
        string? ExpectedSource)> AllCases
    { get; } =
    [
        ("Q1", PositiveCases[0].Question, "support-faq.md"),
        ("Q2", PositiveCases[1].Question, "support-faq.md"),
        ("Q3", PositiveCases[2].Question, "product-guide.md"),
        ("Q4", PositiveCases[3].Question, "product-guide.md"),
        ("Q5", PositiveCases[4].Question, "troubleshooting.md"),
        ("Q6", NegativeQuestions[0], null),
        ("Q7", NegativeQuestions[1], null)
    ];

    public static int TotalQuestions => AllCases.Count;
}
