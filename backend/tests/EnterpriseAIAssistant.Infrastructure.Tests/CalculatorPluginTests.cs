using System;
using Xunit;
using EnterpriseAIAssistant.Infrastructure.AI.Plugins;

namespace EnterpriseAIAssistant.Infrastructure.Tests
{
    public class CalculatorPluginTests
    {
        [Fact]
        public void Calculate_Add_ReturnsSum()
        {
            Assert.Equal(5.0, CalculatorPlugin.Calculate(2, 3, "add"));
        }

        [Fact]
        public void Calculate_SymbolDivide_ReturnsQuotient()
        {
            Assert.Equal(2.5, CalculatorPlugin.Calculate(10, 4, "/"));
        }

        [Fact]
        public void Calculate_DivideByZero_Throws()
        {
            Assert.Throws<DivideByZeroException>(() => CalculatorPlugin.Calculate(5, 0, "divide"));
        }

        [Fact]
        public void Calculate_InvalidOperation_Throws()
        {
            Assert.Throws<ArgumentException>(() => CalculatorPlugin.Calculate(1, 1, ""));
        }

        [Fact]
        public void Calculate_NaNOperand_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CalculatorPlugin.Calculate(double.NaN, 1, "add"));
        }

        [Fact]
        public void Calculate_LargeMultiply_ProducesInfinity()
        {
            var res = CalculatorPlugin.Calculate(1e308, 1e308, "multiply");
            Assert.True(double.IsInfinity(res));
        }
    }
}
