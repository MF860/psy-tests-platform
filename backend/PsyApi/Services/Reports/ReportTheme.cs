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
        /// Enhanced institutional color palette for professional reports v2.0
        /// </summary>
        public static class Colors
        {
            // Primary brand colors (institutional, professional)
            public const string Primary = "#0B5ED7";     // Professional Blue
            public const string Secondary = "#4F46E5";   // Indigo
            
            // Semantic colors for statuses and bands
            public const string Success = "#16A34A";     // Green (for Excellent ≥55)
            public const string Warning = "#F59E0B";     // Amber (for Average 40-54.9)
            public const string Danger = "#DC2626";      // Red (for Weak <40)
            public const string Info = "#0EA5E9";        // Sky blue (informational)
            
            // Performance band colors (aliases for semantic colors)
            public const string Excellent = Success;     // Green ≥55
            public const string Average = Warning;       // Amber 40-54.9  
            public const string Weak = Danger;           // Red <40
            
            // Neutral palette for charts and backgrounds
            public const string Track = "#E5E7EB";       // Gray-200 for unfilled areas
            public const string Unfilled = "#E5E7EB";    // Light gray for empty states (alias)
            public const string Neutral = "#64748B";     // Slate-500
            public const string NeutralLight = "#94A3B8"; // Slate-400
            
            // Layout colors
            public const string Background = "#FFFFFF";  // White
            public const string Surface = "#F9FAFB";     // Gray-50 (card backgrounds)
            public const string SurfaceHover = "#F3F4F6"; // Gray-100 (hover states)
            public const string Border = "#E5E7EB";      // Gray-200 (borders)
            public const string Divider = "#D1D5DB";     // Gray-300 (dividers)
            
            // Text colors (improved contrast)
            public const string Text = "#111827";        // Gray-900 (primary text)
            public const string TextSecondary = "#6B7280"; // Gray-500 (secondary text)
            public const string TextMuted = "#9CA3AF";   // Gray-400 (muted text)
            public const string TextLight = "#D1D5DB";   // Gray-300 (light text on dark)
            
            // Accent colors for highlights and interactive elements
            public const string Accent = "#8B5CF6";      // Purple-500 (CTAs, highlights)
            public const string AccentLight = "#A78BFA"; // Purple-400
        }

        /// <summary>
        /// Enhanced spacing scale (4/8/12/16/24pt system) for consistent layout v2.0
        /// </summary>
        public static class Spacing
        {
            // Modern spacing scale
            public const float XS = 4f;      // Micro spacing (tight gaps)
            public const float SM = 8f;      // Small spacing (default gaps)
            public const float MD = 12f;     // Medium spacing (section gaps)
            public const float LG = 16f;     // Large spacing (major sections)
            public const float XL = 24f;     // Extra large (page-level spacing)
            public const float XXL = 32f;    // Double extra large (major breaks)
            
            // Legacy aliases for compatibility
            public const float XSmall = XS;   // 4f
            public const float Small = SM;    // 8f
            public const float Medium = MD;   // 12f
            public const float Large = LG;    // 16f
            
            // Page margins (consistent across all pages)
            public const float PageMargin = 40f; // 40pt (~14mm) - professional standard
        }

        /// <summary>
        /// Typography scale for consistent text hierarchy v2.0
        /// </summary>
        public static class Typography
        {
            // Heading sizes (Arabic-optimized)
            public const float H1 = 22f;     // Page titles, major headings
            public const float H2 = 18f;     // Section titles
            public const float H3 = 16f;     // Subsection titles
            public const float H4 = 14f;     // Minor headings
            
            // Body text sizes
            public const float Body = 12f;   // Standard body text
            public const float BodyLarge = 14f; // Emphasized body text
            public const float BodySmall = 10f; // Secondary/fine print
            
            // Special purpose sizes
            public const float KPI = 28f;    // Large numbers (KPI cards)
            public const float Caption = 9f; // Captions, footnotes
            public const float Label = 11f;  // Form labels, chart labels
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