namespace EnterpriseAIAssistant.Infrastructure.Retrieval;

/// <summary>Mathematical cosine similarity helper; it does not call an AI model.</summary>
public static class CosineSimilarity
{
    public static double Calculate(
        IReadOnlyList<float> left,
        IReadOnlyList<float> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        if (left.Count == 0 || right.Count == 0)
        {
            throw new ArgumentException("Vectors must not be empty.");
        }

        if (left.Count != right.Count)
        {
            throw new ArgumentException(
                $"Vector dimensions must match. Left: {left.Count}, right: {right.Count}.");
        }

        double dot = 0;
        double leftNormSquared = 0;
        double rightNormSquared = 0;

        for (var i = 0; i < left.Count; i++)
        {
            var a = left[i];
            var b = right[i];

            if (!float.IsFinite(a) || !float.IsFinite(b))
            {
                throw new ArgumentException("Vectors must contain only finite values.");
            }

            dot += (double)a * b;
            leftNormSquared += (double)a * a;
            rightNormSquared += (double)b * b;
        }

        if (leftNormSquared == 0 || rightNormSquared == 0)
        {
            throw new ArgumentException("Cosine similarity is undefined for a zero vector.");
        }

        // Clamp tiny floating-point overshoots to the mathematical range [-1, 1].
        return Math.Clamp(dot / (Math.Sqrt(leftNormSquared) * Math.Sqrt(rightNormSquared)), -1d, 1d);
    }
}
