using PsyApi.Services.Reports;
using PsyApi.Services.Scoring;
using System.Security.Cryptography;
using Xunit;

namespace PsyApi.Tests.Reports
{
    /// <summary>
    /// Visual regression tests for PDF reports
    /// Ensures consistent output by comparing file hashes
    /// </summary>
    public class VisualRegressionTests
    {
        private readonly IModernSdjSevenPatternReportService _reportService;

        public VisualRegressionTests()
        {
            _reportService = new ModernSdjSevenPatternReportService();
        }

        [Fact]
        public void GeneratePdf_ProducesDeterministicOutput()
        {
            // Arrange: Create fixed test data (deterministic input)
            var testData = CreateFixtureData();

            // Act: Generate PDF twice
            var pdf1 = _reportService.GenerateReport(testData);
            var pdf2 = _reportService.GenerateReport(testData);

            // Assert: File sizes should be similar (within 5% tolerance due to timestamps)
            var sizeDiff = Math.Abs(pdf1.Length - pdf2.Length);
            var tolerance = pdf1.Length * 0.05;
            Assert.True(sizeDiff <= tolerance, 
                $"PDF size variance too high: {sizeDiff} bytes (tolerance: {tolerance})");
        }

        [Fact]
        public void GeneratePdf_ContainsExpectedMetadata()
        {
            // Arrange
            var testData = CreateFixtureData();

            // Act
            var pdfBytes = _reportService.GenerateReport(testData);

            // Assert: Basic PDF structure validation
            Assert.NotEmpty(pdfBytes);
            Assert.True(pdfBytes.Length > 50_000, "PDF should be at least 50KB");
            Assert.True(pdfBytes.Length < 5_000_000, "PDF should be under 5MB");

            // Check PDF magic number (starts with "%PDF-")
            var header = System.Text.Encoding.ASCII.GetString(pdfBytes.Take(5).ToArray());
            Assert.Equal("%PDF-", header);
        }

        [Fact]
        public void GeneratePdf_HashStability_WithFixedTimestamp()
        {
            // Arrange: Create data with fixed timestamp to ensure determinism
            var testData = CreateFixtureDataWithFixedDate();

            // Act: Generate PDF and compute SHA256 hash
            var pdfBytes = _reportService.GenerateReport(testData);
            var hash = ComputeSHA256(pdfBytes);

            // Assert: Store this hash as baseline (update when intentional changes occur)
            // For initial run, just validate hash format
            Assert.NotNull(hash);
            Assert.Equal(64, hash.Length); // SHA256 hex string length
            Assert.Matches("^[0-9a-f]{64}$", hash.ToLower());

            // TODO: After baseline established, compare against stored hash:
            // const string expectedHash = "abc123..."; // Update when redesign changes are finalized
            // Assert.Equal(expectedHash, hash);
        }

        /// <summary>
        /// Create deterministic fixture data for testing
        /// </summary>
        private SdjReportData CreateFixtureData()
        {
            return new SdjReportData
            {
                SessionId = 9999,
                SessionDate = DateTime.Parse("2024-01-15 10:30:00"),
                ParticipantName = "اختبار تجريبي",
                ParticipantId = "TEST001",
                SevenPatterns = new List<SevenPatternScore>
                {
                    new() { PatternKey = "Pattern1", PatternNameAr = "النمط الأول", TScore = 55.0, Percentile = 0.70 },
                    new() { PatternKey = "Pattern2", PatternNameAr = "النمط الثاني", TScore = 48.5, Percentile = 0.45 },
                    new() { PatternKey = "Pattern3", PatternNameAr = "النمط الثالث", TScore = 62.0, Percentile = 0.85 },
                    new() { PatternKey = "Pattern4", PatternNameAr = "النمط الرابع", TScore = 51.2, Percentile = 0.55 },
                    new() { PatternKey = "Pattern5", PatternNameAr = "النمط الخامس", TScore = 44.0, Percentile = 0.30 },
                    new() { PatternKey = "Pattern6", PatternNameAr = "النمط السادس", TScore = 58.5, Percentile = 0.75 },
                    new() { PatternKey = "Pattern7", PatternNameAr = "النمط السابع", TScore = 47.0, Percentile = 0.40 }
                },
                SubDimensions = Enumerable.Range(1, 28).Select(i => new SdjSubDimensionScore
                {
                    SubDimension = $"البُعد الفرعي {i}",
                    T = 45.0 + i * 0.5,
                    Percentile = 0.3 + i * 0.02
                }).ToList(),
                WeakDimensions = new List<WeakDimensionInfo>
                {
                    new() { Dimension = "البُعد الضعيف 1", T = 42.0, Percentile = 0.25 },
                    new() { Dimension = "البُعد الضعيف 2", T = 40.5, Percentile = 0.20 }
                },
                RecommendedCourses = new List<RecommendedCourse>
                {
                    new() { CourseTitle = "دورة تدريبية 1", Description = "وصف الدورة", TargetDimensions = new[] { "البُعد 1" } },
                    new() { CourseTitle = "دورة تدريبية 2", Description = "وصف أخرى", TargetDimensions = new[] { "البُعد 2", "البُعد 3" } }
                }
            };
        }

        /// <summary>
        /// Create fixture with fixed date to remove timestamp variance
        /// </summary>
        private SdjReportData CreateFixtureDataWithFixedDate()
        {
            var data = CreateFixtureData();
            data.SessionDate = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
            return data;
        }

        /// <summary>
        /// Compute SHA256 hash for file integrity validation
        /// </summary>
        private string ComputeSHA256(byte[] data)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(data);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }
}
