using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PsyApi.Services.Reports
{
    /// <summary>
    /// Theme constants and helpers for modern PDF reports
    /// </summary>
    public static class ReportTheme
    {
        /// <summary>
        /// Modern color palette for reports - following exact specifications
        /// </summary>
        public static class Colors
        {
            // Primary brand colors
            public const string Primary = "#1E40AF";     // Blue
            public const string Secondary = "#6366F1";   // Indigo
            
            // FINAL: Performance band colors as per v2 specifications
            public const string Excellent = "#14A44D";   // Green ≥55
            public const string Average = "#FF8C00";     // Orange 40-54.9  
            public const string Weak = "#E53935";        // Red <40
            
            // Track color for donut backgrounds
            public const string Track = "#E5E7EB";       // Gray-200 for unfilled areas
            public const string Unfilled = "#E5E7EB";    // Light gray for empty states (alias)
            
            // Layout colors
            public const string Background = "#FFFFFF";  // White
            public const string Surface = "#FAFAFA";     // Light surface
            public const string Border = "#E2E8F0";      // Gray border
            
            // Text colors
            public const string Text = "#111827";        // Dark text (updated)
            public const string TextSecondary = "#6B7280"; // Muted text (updated)
            public const string TextLight = "#9CA3AF";   // Light gray
        }

        /// <summary>
        /// Tight spacing scale (8/12/16pt system) for consistent, compact layout
        /// </summary>
        public static class Spacing
        {
            // Modern compact spacing scale
            public const float XS = 4f;      // Micro spacing
            public const float SM = 8f;      // Small spacing 
            public const float MD = 12f;     // Medium spacing
            public const float LG = 16f;     // Large spacing
            
            // Legacy aliases for compatibility
            public const float XSmall = XS;   // 4f
            public const float Small = SM;    // 8f
            public const float Medium = MD;   // 12f
            public const float Large = LG;    // 16f
            
            // Page margins (16-18mm as specified)
            public const float PageMargin = 16f; // ~16mm in points
        }

        /// <summary>
        /// FINAL: Get band color based on T-score (exact thresholds as specified)
        /// </summary>
        public static string GetBandColor(double tScore)
        {
            if (double.IsNaN(tScore) || double.IsInfinity(tScore))
            {
                Console.WriteLine($"[ReportTheme] Warning: Invalid T-score {tScore}, defaulting to Weak band");
                return Colors.Weak;
            }
            
            if (tScore >= 55.0) return Colors.Excellent;   // Green ≥55
            if (tScore >= 40.0) return Colors.Average;     // Orange 40-54.9
            return Colors.Weak;                           // Red <40
        }
        
        /// <summary>
        /// Legacy alias for backward compatibility
        /// </summary>
        public static string BandColor(double tScore) => GetBandColor(tScore);

        /// <summary>
        /// Get performance label in Arabic based on exact T-score bands
        /// </summary>
        public static string GetPerformanceLabel(double tScore)
        {
            if (tScore >= 55.0) return "ممتاز";    // Excellent ≥55
            if (tScore >= 40.0) return "متوسط";    // Average 40-54.9  
            return "ضعيف";                        // Weak <40
        }

        /// <summary>
        /// Alias for GetPerformanceLabel for compatibility
        /// </summary>
        public static string PerformanceLabel(double tScore) => GetPerformanceLabel(tScore);

        /// <summary>
        /// Create Arabic text style for proper RTL rendering with Noto Naskh Arabic
        /// </summary>
        public static TextStyle ArabicTextStyle(float size, bool bold = false, string color = Colors.Text)
        {
            var style = TextStyle.Default
                .FontFamily("Noto Naskh Arabic")
                .FontSize(size)
                .FontColor(color)
                .DirectionFromRightToLeft();
            
            if (bold)
                style = style.Bold();
                
            return style;
        }

        /// <summary>
        /// Force Western numerals (0-9) instead of Arabic-Indic digits with proper culture handling
        /// UPDATED: Fixed broken decimal formatting that caused � glyphs
        /// </summary>
        public static string FormatNumberWestern(object value, int decimals = 1)
        {
            if (value == null) return "0";
            
            // Always use InvariantCulture to prevent broken Arabic numerals
            var culture = System.Globalization.CultureInfo.InvariantCulture;
            
            try 
            {
                double numericValue = 0;
                
                // Convert all inputs to double first
                if (value is double d)
                    numericValue = d;
                else if (value is float f)
                    numericValue = (double)f;
                else if (value is int i)
                    numericValue = (double)i;
                else if (value is long l)
                    numericValue = (double)l;
                else if (value is decimal dec)
                    numericValue = (double)dec;
                else if (value is string str)
                {
                    // Convert Arabic-Indic digits to Western if present
                    str = ConvertArabicNumeralsToWestern(str);
                    if (double.TryParse(str, System.Globalization.NumberStyles.Float, culture, out double parsed))
                        numericValue = parsed;
                    else
                        return "0";
                }
                else
                {
                    // Try to parse as string
                    var strVal = value.ToString() ?? "0";
                    strVal = ConvertArabicNumeralsToWestern(strVal);
                    if (double.TryParse(strVal, System.Globalization.NumberStyles.Float, culture, out double parsed))
                        numericValue = parsed;
                    else
                        return "0";
                }
                
                // Format with exact decimal places to prevent � replacement glyphs
                if (decimals == 0)
                    return Math.Round(numericValue).ToString("F0", culture);
                else
                    return numericValue.ToString($"F{decimals}", culture);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ReportTheme] FormatNumberWestern error for value '{value}': {ex.Message}");
                return "0";
            }
        }
        
        /// <summary>
        /// MASTER v3: Single FormatNum helper - forced ASCII numerals only
        /// FormatNum(double x, int decimals = 1, bool westernDigits = true) → "51.7", "74"
        /// Always uses CultureInfo.InvariantCulture to prevent � glyphs
        /// </summary>
        public static string FormatNum(double value, int decimals = 1, bool westernDigits = true)
        {
            // Handle edge cases first to prevent NaN/Infinity rendering
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                Console.WriteLine($"[ReportTheme] Warning: Invalid numeric value {value} replaced with 0");
                return decimals == 0 ? "0" : "0.0";
            }
            
            try
            {
                // Always use InvariantCulture to ensure Western ASCII numerals and prevent � glyphs
                var culture = System.Globalization.CultureInfo.InvariantCulture;
                var formatted = value.ToString($"F{decimals}", culture);
                
                // Force Western digits if requested (default true)
                if (westernDigits)
                {
                    formatted = ConvertArabicNumeralsToWestern(formatted);
                }
                
                return formatted;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ReportTheme] FormatNum error for value {value}: {ex.Message}");
                return decimals == 0 ? "0" : "0.0";
            }
        }
        
        /// <summary>
        /// FINAL: FormatPercent helper as specified
        /// FormatPercent(double p) → FormatNum(p, 0) + "%"
        /// </summary>
        public static string FormatPercent(double value)
        {
            return FormatNum(value, 0) + "%";
        }
        
        /// <summary>
        /// Convert Arabic-Indic numerals (٠١٢٣٤٥٦٧٨٩) to Western numerals (0123456789)
        /// </summary>
        public static string ConvertArabicNumeralsToWestern(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            
            var arabicNumerals = new[] { '٠', '١', '٢', '٣', '٤', '٥', '٦', '٧', '٨', '٩' };
            var westernNumerals = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
            
            var result = input;
            for (int i = 0; i < arabicNumerals.Length; i++)
            {
                result = result.Replace(arabicNumerals[i], westernNumerals[i]);
            }
            
            return result;
        }

        /// <summary>
        /// Format T-Score with Western numerals (1 decimal place as specified)
        /// </summary>
        public static string FormatTScore(double tScore) => FormatNum(tScore, 1);

        /// <summary>
        /// Format percentile with Western numerals (0 decimal places as specified)  
        /// </summary>
        public static string FormatPercentile(double percentile) => FormatNum(percentile, 0);

        /// <summary>
        /// Convert hex color string to SkiaSharp SKColor
        /// </summary>
        public static SkiaSharp.SKColor FromHex(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return SkiaSharp.SKColors.Black;
            
            hex = hex.TrimStart('#');
            if (hex.Length == 6)
            {
                var r = Convert.ToByte(hex.Substring(0, 2), 16);
                var g = Convert.ToByte(hex.Substring(2, 2), 16);
                var b = Convert.ToByte(hex.Substring(4, 2), 16);
                return new SkiaSharp.SKColor(r, g, b);
            }
            return SkiaSharp.SKColors.Black;
        }

        /// <summary>
        /// Standard margins for pages
        /// </summary>
        public static class Margins
        {
            public const float Top = 40f;
            public const float Bottom = 40f;
            public const float Left = 40f;
            public const float Right = 40f;
            public const float All = 40f;
        }

        /// <summary>
        /// Helper method for creating colored chips/badges (for reference, implement inline)
        /// Usage: container.Background(bgColor).Padding(8).Text(text).Style(...)
        /// </summary>
        public static void CreateChip(IContainer container, string text, string bgColor, string textColor = "#FFFFFF")
        {
            // This is a reference implementation - use inline in actual code
            // container.Background(bgColor).Padding(8).Text(text).Style(ArabicTextStyle(10, false, textColor));
        }
    }
}