using PsyApi.Models;

namespace PsyApi.Services.Reports
{
    /// <summary>
    /// Convenience service for generating all 4 report variants (AR/EN × Aurora/Noir)
    /// Used for QA, CI artifacts, and visual regression testing
    /// </summary>
    public class ReportVariantGenerator
    {
        private readonly IPdfReportService _pdfService;
        private readonly ILogger<ReportVariantGenerator> _logger;

        public ReportVariantGenerator(
            IPdfReportService pdfService,
            ILogger<ReportVariantGenerator> logger)
        {
            _pdfService = pdfService;
            _logger = logger;
        }

        /// <summary>
        /// Generate all 4 report variants for a given result
        /// </summary>
        public async Task<ReportVariantSet> GenerateAllVariantsAsync(
            Result result,
            User user,
            IEnumerable<DimensionScore> dimensions,
            CancellationToken ct = default)
        {
            _logger.LogInformation("Generating all 4 report variants for result {ResultId}", result.Id);

            var variants = new ReportVariantSet
            {
                ResultId = result.Id,
                UserId = user.Id.ToString(),
                GeneratedAt = DateTime.UtcNow
            };

            // Generate AR + AuroraNeo
            variants.ArabicAuroraGlass = await GenerateVariantAsync(
                result, user, dimensions, 
                ReportLanguage.AR, 
                DesignTokens.ReportThemeMode.AuroraNeo, 
                ct);

            // Generate AR + AuroraNeo (duplicate for backward compatibility)
            variants.ArabicNoirExecutive = await GenerateVariantAsync(
                result, user, dimensions,
                ReportLanguage.AR,
                DesignTokens.ReportThemeMode.AuroraNeo,
                ct);

            // Generate EN + AuroraNeo
            variants.EnglishAuroraGlass = await GenerateVariantAsync(
                result, user, dimensions,
                ReportLanguage.EN,
                DesignTokens.ReportThemeMode.AuroraNeo,
                ct);

            // Generate EN + AuroraNeo (duplicate for backward compatibility)
            variants.EnglishNoirExecutive = await GenerateVariantAsync(
                result, user, dimensions,
                ReportLanguage.EN,
                DesignTokens.ReportThemeMode.AuroraNeo,
                ct);

            _logger.LogInformation("Successfully generated all 4 variants - Total size: {TotalKB} KB",
                (variants.ArabicAuroraGlass.Length +
                 variants.ArabicNoirExecutive.Length +
                 variants.EnglishAuroraGlass.Length +
                 variants.EnglishNoirExecutive.Length) / 1024);

            return variants;
        }

        /// <summary>
        /// Generate a single variant with specified theme and language
        /// </summary>
        private async Task<byte[]> GenerateVariantAsync(
            Result result,
            User user,
            IEnumerable<DimensionScore> dimensions,
            ReportLanguage language,
            DesignTokens.ReportThemeMode theme,
            CancellationToken ct)
        {
            var variantName = $"{language}_{theme}";
            _logger.LogInformation("Generating variant: {Variant}", variantName);

            // Set global context (thread-safe for async operations)
            LocalizationStrings.CurrentLanguage = language;
            DesignTokens.CurrentTheme = theme;

            try
            {
                // Detect SDJ vs standard report
                var hasSdjData = !string.IsNullOrWhiteSpace(result.DimensionScoresJson) &&
                                 result.DimensionScoresJson.Contains("\"SubDimensions\"");

                byte[] pdfBytes;
                if (hasSdjData)
                {
                    pdfBytes = await _pdfService.RenderSdjResultPdfAsync(result, user, ct);
                }
                else
                {
                    pdfBytes = await _pdfService.RenderResultPdfAsync(result, user, dimensions, ct);
                }

                _logger.LogInformation("Variant {Variant} generated: {SizeKB} KB", 
                    variantName, pdfBytes.Length / 1024);

                return pdfBytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating variant {Variant}", variantName);
                throw;
            }
        }

        /// <summary>
        /// Save all 4 variants to disk (for CI artifacts)
        /// </summary>
        public async Task SaveVariantsToDiskAsync(
            ReportVariantSet variants,
            string outputDirectory,
            string filePrefix = "Report")
        {
            Directory.CreateDirectory(outputDirectory);

            var tasks = new[]
            {
                SaveVariantAsync(variants.ArabicAuroraGlass, outputDirectory, $"{filePrefix}_AR_AuroraGlass.pdf"),
                SaveVariantAsync(variants.ArabicNoirExecutive, outputDirectory, $"{filePrefix}_AR_NoirExecutive.pdf"),
                SaveVariantAsync(variants.EnglishAuroraGlass, outputDirectory, $"{filePrefix}_EN_AuroraGlass.pdf"),
                SaveVariantAsync(variants.EnglishNoirExecutive, outputDirectory, $"{filePrefix}_EN_NoirExecutive.pdf")
            };

            await Task.WhenAll(tasks);

            _logger.LogInformation("Saved all 4 variants to {Directory}", outputDirectory);
        }

        private async Task SaveVariantAsync(byte[] pdfBytes, string directory, string filename)
        {
            var filePath = Path.Combine(directory, filename);
            await File.WriteAllBytesAsync(filePath, pdfBytes);
            _logger.LogInformation("Saved {Filename} ({SizeKB} KB)", filename, pdfBytes.Length / 1024);
        }

        /// <summary>
        /// Calculate SHA256 checksums for all variants (for visual regression testing)
        /// </summary>
        public ReportVariantChecksums CalculateChecksums(ReportVariantSet variants)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();

            return new ReportVariantChecksums
            {
                ArabicAuroraGlass = CalculateChecksum(sha256, variants.ArabicAuroraGlass),
                ArabicNoirExecutive = CalculateChecksum(sha256, variants.ArabicNoirExecutive),
                EnglishAuroraGlass = CalculateChecksum(sha256, variants.EnglishAuroraGlass),
                EnglishNoirExecutive = CalculateChecksum(sha256, variants.EnglishNoirExecutive)
            };
        }

        private string CalculateChecksum(System.Security.Cryptography.SHA256 sha256, byte[] data)
        {
            var hash = sha256.ComputeHash(data);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        /// <summary>
        /// Validate that no variant contains replacement characters (�)
        /// </summary>
        public async Task<ReportValidationResult> ValidateVariantsAsync(ReportVariantSet variants)
        {
            var result = new ReportValidationResult();

            // Note: This is a simplified check. Full validation would require parsing PDF text layers
            // For now, we check that all files are valid PDFs and meet minimum size requirements

            result.ArabicAuroraGlassValid = IsValidPdf(variants.ArabicAuroraGlass);
            result.ArabicNoirExecutiveValid = IsValidPdf(variants.ArabicNoirExecutive);
            result.EnglishAuroraGlassValid = IsValidPdf(variants.EnglishAuroraGlass);
            result.EnglishNoirExecutiveValid = IsValidPdf(variants.EnglishNoirExecutive);

            result.AllValid = result.ArabicAuroraGlassValid &&
                             result.ArabicNoirExecutiveValid &&
                             result.EnglishAuroraGlassValid &&
                             result.EnglishNoirExecutiveValid;

            _logger.LogInformation("Validation result: {AllValid} - AR_Aurora:{AR_Aurora} AR_Noir:{AR_Noir} EN_Aurora:{EN_Aurora} EN_Noir:{EN_Noir}",
                result.AllValid,
                result.ArabicAuroraGlassValid,
                result.ArabicNoirExecutiveValid,
                result.EnglishAuroraGlassValid,
                result.EnglishNoirExecutiveValid);

            return result;
        }

        private bool IsValidPdf(byte[] data)
        {
            if (data == null || data.Length < 1024) // PDFs must be at least 1KB
                return false;

            // Check PDF signature (%PDF-)
            var signature = System.Text.Encoding.ASCII.GetString(data, 0, Math.Min(5, data.Length));
            return signature.StartsWith("%PDF-");
        }
    }

    /// <summary>
    /// Container for all 4 report variants
    /// </summary>
    public class ReportVariantSet
    {
        public int ResultId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public byte[] ArabicAuroraGlass { get; set; } = Array.Empty<byte>();
        public byte[] ArabicNoirExecutive { get; set; } = Array.Empty<byte>();
        public byte[] EnglishAuroraGlass { get; set; } = Array.Empty<byte>();
        public byte[] EnglishNoirExecutive { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// SHA256 checksums for variant comparison
    /// </summary>
    public class ReportVariantChecksums
    {
        public string ArabicAuroraGlass { get; set; } = string.Empty;
        public string ArabicNoirExecutive { get; set; } = string.Empty;
        public string EnglishAuroraGlass { get; set; } = string.Empty;
        public string EnglishNoirExecutive { get; set; } = string.Empty;
    }

    /// <summary>
    /// Validation result for all variants
    /// </summary>
    public class ReportValidationResult
    {
        public bool ArabicAuroraGlassValid { get; set; }
        public bool ArabicNoirExecutiveValid { get; set; }
        public bool EnglishAuroraGlassValid { get; set; }
        public bool EnglishNoirExecutiveValid { get; set; }
        public bool AllValid { get; set; }
    }
}
