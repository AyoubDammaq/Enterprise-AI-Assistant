using System;
using Xunit;
using EnterpriseAIAssistant.Infrastructure.AI.Plugins;

namespace EnterpriseAIAssistant.Infrastructure.Tests
{
    public class DateTimePluginTests
    {
        [Fact]
        public void FormatDateTime_ValidDate_ReturnsFormattedString()
        {
            var dt = new DateTime(2026, 9, 27, 14, 5, 30);
            var s = DateTimePlugin.FormatDateTime(dt);
            Assert.Equal("2026-09-27 14:05:30", s);
        }

        [Theory]
        [InlineData(1900)]
        [InlineData(3000)]
        public void FormatDateTime_BoundaryYears_AreAllowed(int year)
        {
            var dt = new DateTime(year, 1, 1, 0, 0, 0);
            var s = DateTimePlugin.FormatDateTime(dt);
            Assert.NotNull(s);
        }

        [Fact]
        public void FormatDateTime_YearTooLow_Throws()
        {
            var dt = new DateTime(1899, 1, 1);
            Assert.Throws<InvalidOperationException>(() => DateTimePlugin.FormatDateTime(dt));
        }

        [Fact]
        public void FormatDateTime_YearTooHigh_Throws()
        {
            var dt = new DateTime(3001, 1, 1);
            Assert.Throws<InvalidOperationException>(() => DateTimePlugin.FormatDateTime(dt));
        }
    }
}
