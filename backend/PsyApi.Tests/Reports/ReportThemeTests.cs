using PsyApi.Services.Reports;
using Xunit;

namespace PsyApi.Tests.Reports
{
    /// <summary>
    /// Unit tests for ReportTheme utility methods
    /// Validates number formatting, band classification, and text processing
    /// </summary>
    public class ReportThemeTests
    {
        [Theory]
        [InlineData(52.376, 1, "52.4")]
        [InlineData(48.0, 0, "48")]
        [InlineData(67.12, 2, "67.12")]
        [InlineData(100.999, 1, "101.0")]
        [InlineData(0.0, 1, "0.0")]
        public void FormatNum_ReturnsCorrectFormat(double value, int decimals, string expected)
        {
            // Act
            var result = ReportTheme.FormatNum(value, decimals);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(70.0, "#10b981")] // Excellent (>= 55) -> Green
        [InlineData(55.0, "#10b981")] // Boundary: Excellent
        [InlineData(54.9, "#f59e0b")] // Average (45-54.9) -> Orange
        [InlineData(50.0, "#f59e0b")] // Average
        [InlineData(45.0, "#f59e0b")] // Boundary: Average
        [InlineData(44.9, "#ef4444")] // Weak (< 45) -> Red
        [InlineData(30.0, "#ef4444")] // Weak
        public void GetBandColor_ReturnsCorrectColor(double tScore, string expectedColor)
        {
            // Act
            var result = ReportTheme.GetBandColor(tScore);

            // Assert
            Assert.Equal(expectedColor, result);
        }

        [Theory]
        [InlineData(70.0, "ممتاز")]
        [InlineData(55.0, "ممتاز")]
        [InlineData(54.9, "متوسط")]
        [InlineData(50.0, "متوسط")]
        [InlineData(45.0, "متوسط")]
        [InlineData(44.9, "ضعيف")]
        [InlineData(30.0, "ضعيف")]
        public void GetPerformanceLabel_ReturnsCorrectLabel(double tScore, string expected)
        {
            // Act
            var result = ReportTheme.GetPerformanceLabel(tScore);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(0.876, "87.6%")]
        [InlineData(0.5, "50.0%")]
        [InlineData(0.0, "0.0%")]
        [InlineData(1.0, "100.0%")]
        [InlineData(0.999, "99.9%")]
        public void FormatPercent_ReturnsCorrectFormat(double value, string expected)
        {
            // Act
            var result = ReportTheme.FormatPercent(value);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("١٢٣٤٥٦٧٨٩٠", "1234567890")] // Arabic-Indic digits
        [InlineData("T-Score: ٥٥.٧", "T-Score: 55.7")]
        [InlineData("Already Western 123", "Already Western 123")]
        [InlineData("Mixed ١٢٣ and 456", "Mixed 123 and 456")]
        [InlineData("", "")] // Empty string
        public void ConvertArabicNumeralsToWestern_ReturnsCorrectString(string input, string expected)
        {
            // Act
            var result = ReportTheme.ConvertArabicNumeralsToWestern(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Colors_PrimaryUpdated()
        {
            // Act & Assert
            Assert.Equal("#0B5ED7", ReportTheme.Colors.Primary);
        }

        [Fact]
        public void Typography_HasCorrectSizes()
        {
            // Act & Assert
            Assert.Equal(22, ReportTheme.Typography.H1);
            Assert.Equal(18, ReportTheme.Typography.H2);
            Assert.Equal(16, ReportTheme.Typography.H3);
            Assert.Equal(14, ReportTheme.Typography.H4);
            Assert.Equal(12, ReportTheme.Typography.Body);
            Assert.Equal(14, ReportTheme.Typography.BodyLarge);
            Assert.Equal(10, ReportTheme.Typography.BodySmall);
            Assert.Equal(28, ReportTheme.Typography.KPI);
            Assert.Equal(9, ReportTheme.Typography.Caption);
            Assert.Equal(11, ReportTheme.Typography.Label);
        }

        [Fact]
        public void Spacing_HasCorrectValues()
        {
            // Act & Assert
            Assert.Equal(4, ReportTheme.Spacing.XS);
            Assert.Equal(8, ReportTheme.Spacing.SM);
            Assert.Equal(12, ReportTheme.Spacing.MD);
            Assert.Equal(16, ReportTheme.Spacing.LG);
            Assert.Equal(24, ReportTheme.Spacing.XL);
            Assert.Equal(32, ReportTheme.Spacing.XXL);
        }

        [Theory]
        [InlineData(65.0, "ممتاز", "#10b981")] // Excellent
        [InlineData(50.0, "متوسط", "#f59e0b")] // Average
        [InlineData(40.0, "ضعيف", "#ef4444")] // Weak
        public void GetBandColor_And_GetPerformanceLabel_AreConsistent(double tScore, string expectedLabel, string expectedColor)
        {
            // Act
            var label = ReportTheme.GetPerformanceLabel(tScore);
            var color = ReportTheme.GetBandColor(tScore);

            // Assert
            Assert.Equal(expectedLabel, label);
            Assert.Equal(expectedColor, color);
        }
    }
}
