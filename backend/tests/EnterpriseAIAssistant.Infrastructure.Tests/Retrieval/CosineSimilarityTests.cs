using EnterpriseAIAssistant.Infrastructure.Retrieval;

namespace EnterpriseAIAssistant.Infrastructure.Tests.Retrieval;

public sealed class CosineSimilarityTests
{
    [Fact]
    public void Calculate_IdenticalVectors_ReturnsOne()
    {
        // Arrange
        var left = new float[] { 1, 2, 3 };
        var right = new float[] { 1, 2, 3 };

        // Act
        var result = CosineSimilarity.Calculate(left, right);

        // Assert
        Assert.Equal(1d, result, precision: 6);
    }

    [Fact]
    public void Calculate_OppositeVectors_ReturnsMinusOne()
    {
        var left = new float[] { 1, 0 };
        var right = new float[] { -1, 0 };

        var result = CosineSimilarity.Calculate(left, right);

        Assert.Equal(-1d, result, precision: 6);
    }

    [Fact]
    public void Calculate_OrthogonalVectors_ReturnsZero()
    {
        var left = new float[] { 1, 0 };
        var right = new float[] { 0, 1 };

        var result = CosineSimilarity.Calculate(left, right);

        Assert.Equal(0d, result, precision: 6);
    }

    [Fact]
    public void Calculate_SameDirectionDifferentMagnitudes_ReturnsOne()
    {
        var left = new float[] { 1, 2 };
        var right = new float[] { 2, 4 };

        var result = CosineSimilarity.Calculate(left, right);

        Assert.Equal(1d, result, precision: 6);
    }

    [Fact]
    public void Calculate_DifferentDimensions_Throws()
    {
        var left = new float[] { 1, 2 };
        var right = new float[] { 1, 2, 3 };

        Assert.Throws<ArgumentException>(
            () => CosineSimilarity.Calculate(left, right));
    }

    [Fact]
    public void Calculate_EmptyLeftVector_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => CosineSimilarity.Calculate(
                Array.Empty<float>(),
                new float[] { 1, 0 }));
    }

    [Fact]
    public void Calculate_EmptyRightVector_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => CosineSimilarity.Calculate(
                new float[] { 1, 0 },
                Array.Empty<float>()));
    }

    [Fact]
    public void Calculate_ZeroVector_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => CosineSimilarity.Calculate(
                new float[] { 0, 0 },
                new float[] { 1, 0 }));
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void Calculate_NonFiniteValue_Throws(float invalidValue)
    {
        var left = new float[] { invalidValue, 1 };
        var right = new float[] { 1, 0 };

        Assert.Throws<ArgumentException>(
            () => CosineSimilarity.Calculate(left, right));
    }

    [Fact]
    public void Calculate_NullLeftVector_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => CosineSimilarity.Calculate(
                null!,
                new float[] { 1, 0 }));
    }

    [Fact]
    public void Calculate_NullRightVector_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => CosineSimilarity.Calculate(
                new float[] { 1, 0 },
                null!));
    }
}
