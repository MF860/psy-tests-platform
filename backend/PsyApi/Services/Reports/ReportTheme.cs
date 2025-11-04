using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PsyApi.Services.Reports
{
    /// <summary>
    /// ULTRA HI-FI v5 Design System - Aurora Glass & Noir Executive Themes
    /// Dramatically different visual identity with glassmorphism and premium gradients
    /// </summary>
    public static class ReportTheme
    {
        /// <summary>
        /// Active theme mode - controls all visual styling
        /// </summary>
        public enum ThemeMode
        {
            AuroraGlass,    // Light theme with glassmorphism and gradients
            NoirExecutive   // Dark theme with neumorphic shadows
        }

        // Default to Aurora Glass for immediate visual impact
        public static ThemeMode CurrentTheme { get; set; } = ThemeMode.AuroraGlass;

        /// <summary>
        /// ULTRA HI-FI v5 Color System - Dramatically Enhanced
        /// Aurora Glass: Vibrant gradients with glassmorphism
        /// Noir Executive: Premium dark with high contrast
        /// </summary>
        public static class Colors
        {
            // === AURORA GLASS COLORS (Light Theme) ===
            private static class Aurora
            {
                // Background gradients (applied to full pages)
                public const string GradientStart = "#D4F1F4";    // Soft cyan
                public const string GradientEnd = "#E8F5E9";      // Soft mint
                
                // Glass surfaces with blur effect
                public const string GlassLight = "#FFFFFF";       // Pure white glass
                public const string GlassMedium = "#F8FEFF";      // Cyan-tinted glass
                public const string GlassDark = "#ECF5FF";        // Blue-tinted glass
                
                // Primary brand - vibrant electric blue
                public const string Primary = "#0066FF";          // Electric Blue
                public const string PrimaryLight = "#3399FF";     // Sky Blue
                public const string PrimaryDark = "#0047B3";      // Deep Blue
                
                // Status colors - vivid and clear
                public const string Success = "#00C853";          // Vivid Green
                public const string Warning = "#FFB300";          // Bright Amber
                public const string Danger = "#FF1744";           // Vivid Red
                public const string Info = "#00B8D4";             // Cyan
                
                // Accent colors - purple and gold
                public const string Accent = "#AA00FF";           // Vivid Purple
                public const string Gold = "#FFD700";             // True Gold
                
                // Text on glass surfaces
                public const string Text = "#1A1A1A";             // Almost black
                public const string TextSecondary = "#424242";    // Dark gray
                public const string TextTertiary = "#757575";     // Medium gray
                
                // Borders and dividers - subtle but visible
                public const string Border = "#B0BEC5";           // Blue-gray
                public const string Divider = "#CFD8DC";          // Light blue-gray
            }

            // === NOIR EXECUTIVE COLORS (Dark Theme) ===
            private static class Noir
            {
                // Background gradients (dark with vignette)
                public const string GradientStart = "#0A0E27";    // Deep navy
                public const string GradientEnd = "#1A1A2E";      // Charcoal
                
                // Neumorphic surfaces
                public const string SurfaceElevated = "#16213E";  // Elevated panel
                public const string SurfaceBase = "#0F1419";      // Base panel
                public const string SurfaceDepressed = "#080B10"; // Depressed area
                
                // Primary brand - bright cyan for contrast
                public const string Primary = "#00D9FF";          // Bright Cyan
                public const string PrimaryLight = "#33E0FF";     // Lighter Cyan
                public const string PrimaryDark = "#00A8CC";      // Deep Cyan
                
                // Status colors - neon-bright for dark theme
                public const string Success = "#00FF85";          // Neon Green
                public const string Warning = "#FFD93D";          // Bright Yellow
                public const string Danger = "#FF3D71";           // Neon Pink-Red
                public const string Info = "#6BCF7F";             // Mint Green
                
                // Accent colors - gold and magenta
                public const string Accent = "#FF0080";           // Hot Pink
                public const string Gold = "#FFAA00";             // Bright Gold
                
                // Text for dark backgrounds
                public const string Text = "#FFFFFF";             // Pure white
                public const string TextSecondary = "#B0BEC5";    // Light gray
                public const string TextTertiary = "#78909C";     // Medium gray
                
                // Borders - glowing effect
                public const string Border = "#2C3E50";           // Subtle glow
                public const string Divider = "#1C2A38";          // Darker divider
            }

            // === ACTIVE THEME PROPERTIES (Dynamic) ===
            public static string Background => CurrentTheme == ThemeMode.AuroraGlass 
                ? Aurora.GradientStart : Noir.GradientStart;
            
            public static string BackgroundEnd => CurrentTheme == ThemeMode.AuroraGlass 
                ? Aurora.GradientEnd : Noir.GradientEnd;
            
            public static string Surface => CurrentTheme == ThemeMode.AuroraGlass 
                ? Aurora.GlassLight : Noir.SurfaceElevated;
            
            public static string SurfaceHover => CurrentTheme == ThemeMode.AuroraGlass 
                ? Aurora.GlassMedium : Noir.SurfaceBase;
            
            public static string Primary => CurrentTheme == ThemeMode.AuroraGlass 
                ? Aurora.Primary : Noir.Primary;
            
            public static string PrimaryLight => CurrentTheme == ThemeMode.AuroraGlass 
                ? Aurora.PrimaryLight : Noir.PrimaryLight;
            
            public static string Success => CurrentTheme == ThemeMode.AuroraGlass 
                ? Aurora.Success : Noir.Success;
            
            public static string Warning => CurrentTheme == ThemeMode.AuroraGlass 
                ? Aurora.Warning : Noir.Warning;
            
            public static string Danger => CurrentTheme == ThemeMode.AuroraGlass 
                ? Aurora.Danger : Noir.Danger;
            
            public static string Info => CurrentTheme == ThemeMode.AuroraGlass 
                ? Aurora.Info : Noir.Info;
            
            public static string Accent => CurrentTheme == ThemeMode.AuroraGlass 
                ? Aurora.Accent : Noir.Accent;
            
            public static string Gold => CurrentTheme == ThemeMode.AuroraGlass 
                ? Aurora.Gold : Noir.Gold;
            
            public static string Text => CurrentTheme == ThemeMode.AuroraGlass 
                ? Aurora.Text : Noir.Text;
            
            public static string TextSecondary => CurrentTheme == ThemeMode.AuroraGlass 
                ? Aurora.TextSecondary : Noir.TextSecondary;
            
            public static string TextMuted => CurrentTheme == ThemeMode.AuroraGlass 
                ? Aurora.TextTertiary : Noir.TextTertiary;
            
            public static string Border => CurrentTheme == ThemeMode.AuroraGlass 
                ? Aurora.Border : Noir.Border;
            
            public static string Divider => CurrentTheme == ThemeMode.AuroraGlass 
                ? Aurora.Divider : Noir.Divider;

            // Aliases for backward compatibility
            public static string Excellent => Success;
            public static string Average => Warning;
            public static string Weak => Danger;
            public static string Secondary => CurrentTheme == ThemeMode.AuroraGlass ? "#6366F1" : "#9333EA";
            public static string Track => Border;
            public static string Unfilled => Divider;
            public static string Neutral => TextSecondary;
            public static string NeutralLight => TextMuted;
            public static string TextLight => TextMuted;
            public static string AccentLight => Accent;
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
        /// Typography scale for consistent text hierarchy v3.0 HiFi
        /// All sizes optimized for Noto Naskh Arabic at 300 DPI
        /// </summary>
        public static class Typography
        {
            // Heading sizes (Arabic-optimized for HiFi)
            public const float H1 = 24f;     // Page titles, major headings (increased for impact)
            public const float H2 = 18f;     // Section titles
            public const float H3 = 14f;     // Subsection titles (was 16, adjusted for balance)
            public const float H4 = 12f;     // Minor headings
            
            // Body text sizes
            public const float Body = 12f;   // Standard body text (perfect readability)
            public const float BodyLarge = 14f; // Emphasized body text
            public const float BodySmall = 10f; // Secondary/fine print
            
            // Special purpose sizes (HiFi enhanced)
            public const float KPI = 32f;    // Large numbers (KPI cards) - increased for prominence
            public const float KPILabel = 11f; // KPI labels
            public const float Caption = 9f; // Captions, footnotes
            public const float Label = 11f;  // Form labels, chart labels
            public const float Badge = 10f;  // Badge text
            public const float Icon = 16f;   // Unicode icons size
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
        public static TextStyle ArabicTextStyle(float size, bool bold = false, string? color = null)
        {
            var textColor = color ?? Colors.Text;  // Resolve at runtime
            var style = TextStyle.Default
                .FontFamily("Noto Naskh Arabic")
                .FontSize(size)
                .FontColor(textColor)
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