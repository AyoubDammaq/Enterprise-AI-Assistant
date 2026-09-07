using EnterpriseAIAssistant.Infrastructure.AI.Plugins;

namespace EnterpriseAIAssistant.Infrastructure.Tests.Plugins
{
    public class CalculatorPluginTests
    {
        [Theory]
        [InlineData(1d, 1d, "add", 2d)]
        [InlineData(10d, 3d, "subtract", 7d)]
        [InlineData(6d, 7d, "multiply", 42d)]
        [InlineData(10d, 4d, "divide", 2.5d)]
        public void Calculate_ValidOperation_ReturnsExpectedResult(
            double left,
            double right,
            string operation,
            double expected)
        {
            // Act
            var result = CalculatorPlugin.Calculate(left, right, operation);

            // Assert
            Assert.Equal(expected, result, precision: 6);
        }

        [Fact]
        public void Calculate_OperationIsCaseInsensitive_ReturnsExpectedResult()
        {
            // Act
            var result = CalculatorPlugin.Calculate(2d, 3d, "AdD");

            // Assert
            Assert.Equal(5d, result, precision: 6);
        }

        [Fact]
        public void Calculate_DivideByZero_ThrowsDivideByZeroException()
        {
            // Act & Assert
            var ex = Assert.Throws<DivideByZeroException>(
                () => CalculatorPlugin.Calculate(10d, 0d, "divide"));

            Assert.Equal("Cannot divide by zero.", ex.Message);
        }

        [Fact]
        public void Calculate_UnsupportedOperation_ThrowsArgumentException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(
                () => CalculatorPlugin.Calculate(10d, 5d, "modulo"));

            Assert.Contains("Unsupported operation", ex.Message);
            Assert.Equal("operation", ex.ParamName);
        }
    }
}