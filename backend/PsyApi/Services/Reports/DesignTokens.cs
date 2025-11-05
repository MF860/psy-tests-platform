using QuestPDF.Infrastructure;
using QuestPDF.Helpers;
using SkiaSharp;
using System.Text;

namespace PsyApi.Services.Reports
{
    /// <summary>
    /// UltraHiFi v6.1 AuroraNeo Design Tokens — Complete Redesign
    /// 100% Modern, NO legacy resemblance. Editorial-grade print quality.
    /// </summary>
    public static class DesignTokens
    {
        /// <summary>
        /// AuroraNeo Light is the ONLY theme for v6.1
        /// </summary>
        public static ReportThemeMode CurrentTheme { get; set; } = ReportThemeMode.AuroraNeo;

        public enum ReportThemeMode
        {
            AuroraNeo
        }

        /// <summary>
        /// AuroraNeo Modern Palette — ZERO visual overlap with legacy themes
        /// Primary: Cyan Sky (#0EA5E9), Secondary: Indigo (#6366F1), Accent: Emerald (#22C55E)
        /// </summary>
        public static class Colors
        {
            // Primary Palette (Cyan Sky)
            public static string Primary => "#0EA5E9";
            public static string PrimaryLight => "#38BDF8";
            public static string PrimaryDark => "#0369A1";

            // Secondary Palette (Indigo)
            public static string Secondary => "#6366F1";
            public static string SecondaryLight => "#A5B4FC";
            public static string SecondaryDark => "#4338CA";

            // Semantic Colors
            public static string Accent => "#22C55E";      // Emerald (Growth/Success)
            public static string Warning => "#F59E0B";     // Amber (Caution)
            public static string Danger => "#EF4444";      // Red (Risk/Critical)
            public static string Success => "#22C55E";     // Same as Accent
            public static string Info => "#0EA5E9";        // Same as Primary

            // Background System
            public static string Background => "#F8FAFC";  // Ultra-light slate
            public static string BackgroundGradient => "linear-gradient(135deg, #E0F2FE 0%, #EEF2FF 50%, #F8FAFC 100%)";
            public static float BackgroundNoiseOpacity => 0.015f; // Subtle texture

            // Surface System (Glass Cards)
            public static string Surface => "#FFFFFF";
            public static string SurfaceHover => "#F1F5F9";
            public static string SurfaceElevated => "#FEFEFE";
            public static string SurfaceGlass => "rgba(255, 255, 255, 0.65)"; // 65% opacity white glass

            // Border & Dividers
            public static string Border => "#E5E7EB";      // Light gray
            public static string BorderSubtle => "#F3F4F6"; // Almost invisible
            public static string Divider => "#E5E7EB";

            // Typography Colors
            public static string Text => "#0F172A";         // Slate 900
            public static string TextSecondary => "#334155"; // Slate 700
            public static string TextMuted => "#64748B";     // Slate 500
            public static string TextInverse => "#FFFFFF";

            // Glass Morphism
            public static string GlassOverlay => "rgba(255, 255, 255, 0.65)";
            public static string GlassBorder => "rgba(255, 255, 255, 0.40)";
            public static string GlassShadow => "rgba(0, 0, 0, 0.08)";

            // Shadow System
            public static string ShadowLight => "rgba(15, 23, 42, 0.04)";
            public static string ShadowMedium => "rgba(15, 23, 42, 0.08)";
            public static string ShadowStrong => "rgba(15, 23, 42, 0.12)";

            // Chart Colors (Performance Bands)
            public static string ChartExcellent => "#22C55E"; // Emerald
            public static string ChartGood => "#0EA5E9";      // Cyan
            public static string ChartAverage => "#F59E0B";   // Amber
            public static string ChartWeak => "#EF4444";      // Red
            public static string ChartNeutral => "#CBD5E1";   // Slate 300

            /// <summary>
            /// Convert hex to SKColor for SkiaSharp rendering
            /// </summary>
            public static SKColor ToSKColor(string hexColor)
            {
                if (string.IsNullOrEmpty(hexColor)) return SKColors.Black;
                
                hexColor = hexColor.TrimStart('#');
                if (hexColor.Length == 6)
                {
                    var r = Convert.ToByte(hexColor.Substring(0, 2), 16);
                    var g = Convert.ToByte(hexColor.Substring(2, 2), 16);
                    var b = Convert.ToByte(hexColor.Substring(4, 2), 16);
                    return new SKColor(r, g, b);
                }
                return SKColors.Black;
            }

            /// <summary>
            /// Parse rgba string to SKColor
            /// </summary>
            public static SKColor ParseRGBA(string rgba)
            {
                try
                {
                    var vals = rgba.Replace("rgba(", "").Replace(")", "").Split(',');
                    if (vals.Length >= 4)
                    {
                        var r = byte.Parse(vals[0].Trim());
                        var g = byte.Parse(vals[1].Trim());
                        var b = byte.Parse(vals[2].Trim());
                        var a = (byte)(float.Parse(vals[3].Trim()) * 255);
                        return new SKColor(r, g, b, a);
                    }
                }
                catch { }
                return SKColors.Transparent;
            }
        }

        /// <summary>
        /// Typography System — Scale: H1(26), H2(20), H3(16), Title(14), Body(12), Small(10)
        /// Primary Font: Noto Naskh Arabic (Regular/Bold)
        /// </summary>
        public static class Typography
        {
            // Font Families
            public const string FontArabic = "Noto Naskh Arabic";
            public const string FontEnglish = "Inter";

            // Size Scale (pt)
            public const float H1 = 26f;            // Page titles
            public const float H2 = 20f;            // Section headers
            public const float H3 = 16f;            // Subsection headers
            public const float H4 = 14f;            // Component headers
            public const float TitleSemibold = 14f; // Card titles
            public const float Body = 12f;          // Body text
            public const float BodyLarge = 14f;     // Lead paragraphs
            public const float Caption = 10f;       // Captions
            public const float KPI = 34f;           // Large KPI numbers
            public const float KPILabel = 12f;      // KPI labels
            public const float Badge = 11f;         // Badge text
            public const float Small = 10f;         // Small text
            public const float Micro = 9f;          // Footnotes

            // Line Heights
            public const float LineHeightTight = 1.25f;
            public const float LineHeightNormal = 1.5f;
            public const float LineHeightRelaxed = 1.75f;
        }

        /// <summary>
        /// Spacing System — 8pt grid (8, 12, 16, 24, 32, 40)
        /// </summary>
        public static class Spacing
        {
            public const float XS = 6f;
            public const float SM = 8f;
            public const float MD = 12f;
            public const float LG = 16f;
            public const float XL = 24f;
            public const float XXL = 32f;
            public const float XXXL = 40f;
            public const float Jumbo = 48f;

            // Semantic Spacing
            public const float SectionGap = 24f;     // Between major sections
            public const float ComponentGap = 16f;   // Between components
            public const float ElementGap = 8f;      // Between elements
            public const float CardPadding = 16f;    // Inside cards

            // Page Margins
            public const float PageMarginTop = 40f;
            public const float PageMarginBottom = 40f;
            public const float PageMarginLeft = 36f;
            public const float PageMarginRight = 36f;
        }

        /// <summary>
        /// Border Radius System
        /// </summary>
        public static class Radius
        {
            public const float None = 0f;
            public const float SM = 8f;
            public const float MD = 12f;
            public const float LG = 18f;   // Glass cards
            public const float XL = 24f;
            public const float Pill = 999f;
        }

        /// <summary>
        /// Shadow System (soft, modern)
        /// </summary>
        public static class Shadows
        {
            public const float None = 0f;
            public const float Subtle = 2f;
            public const float Soft = 4f;
            public const float Medium = 8f;
            public const float Strong = 12f;
        }

        /// <summary>
        /// Opacity Scale
        /// </summary>
        public static class Opacity
        {
            public const float Invisible = 0f;
            public const float Subtle = 0.05f;
            public const float Light = 0.1f;
            public const float Medium = 0.25f;
            public const float Strong = 0.5f;
            public const float Heavy = 0.75f;
            public const float MostlyOpaque = 0.9f;
            public const float Opaque = 1f;
        }

        /// <summary>
        /// Performance Band Thresholds (Psychometric Standard)
        /// </summary>
        public static class PerformanceBands
        {
            public const double ExcellentMin = 65.0;
            public const double GoodMin = 55.0;
            public const double AverageMin = 40.0;

            public static string GetBandColor(double tScore)
            {
                if (double.IsNaN(tScore) || double.IsInfinity(tScore))
                    return Colors.ChartNeutral;
                
                if (tScore >= ExcellentMin) return Colors.ChartExcellent;
                if (tScore >= GoodMin) return Colors.ChartGood;
                if (tScore >= AverageMin) return Colors.ChartAverage;
                return Colors.ChartWeak;
            }
            
            public static string GetBandLabelAr(double tScore)
            {
                if (tScore >= ExcellentMin) return "ممتاز";
                if (tScore >= GoodMin) return "جيد جداً";
                if (tScore >= AverageMin) return "متوسط";
                return "بحاجة للتطوير";
            }
            
            public static string GetBandLabelEn(double tScore)
            {
                if (tScore >= ExcellentMin) return "Excellent";
                if (tScore >= GoodMin) return "Very Good";
                if (tScore >= AverageMin) return "Average";
                return "Needs Development";
            }
        }

        /// <summary>
        /// Number Formatting (Western digits ONLY, UTF-8 safe)
        /// </summary>
        public static class Formatting
        {
            public static string FormatNumber(double value, int decimals = 1)
            {
                if (double.IsNaN(value) || double.IsInfinity(value))
                    return decimals == 0 ? "0" : "0.0";
                
                var culture = System.Globalization.CultureInfo.InvariantCulture;
                return value.ToString($"F{decimals}", culture);
            }
            
            public static string FormatPercent(double value)
            {
                return FormatNumber(value, 0) + "%";
            }
            
            public static string FormatTScore(double tScore)
            {
                return FormatNumber(tScore, 1);
            }
            
            public static string FormatPercentile(double percentile)
            {
                return FormatNumber(percentile, 0);
            }
        }

        /// <summary>
        /// Arabic Text Normalization — UTF-8 NFC canonical form
        /// ZERO garbled characters (�)
        /// </summary>
        public static class ArText
        {
            /// <summary>
            /// Sanitize Arabic text: NFC normalization + remove replacement chars + trim
            /// This is the PRIMARY method to use for all Arabic text rendering
            /// </summary>
            public static string Sanitize(string? s)
            {
                if (string.IsNullOrWhiteSpace(s)) return string.Empty;
                
                var normalized = s.Normalize(NormalizationForm.FormC)
                                 .Replace('\uFFFD', ' ')   // Remove Unicode replacement character
                                 .Replace("�", " ")        // Remove visible replacement glyph
                                 .Replace("\u200B", "")    // Remove zero-width space
                                 .Replace("\u200C", "")    // Remove zero-width non-joiner (if problematic)
                                 .Replace("\u200D", "")    // Remove zero-width joiner (if problematic)
                                 .Trim();
                
                return normalized;
            }

            /// <summary>
            /// Check if text contains garbled/replacement characters
            /// Use after Sanitize() to verify cleanliness
            /// </summary>
            public static bool HasGarbledCharacters(string text)
            {
                if (string.IsNullOrEmpty(text)) return false;
                return text.Contains('�') || text.Contains('\uFFFD');
            }

            /// <summary>
            /// Check if text contains placeholder keys (e.g., "insights��tle")
            /// Common placeholder patterns from broken localization
            /// </summary>
            public static bool HasPlaceholderKeys(string text)
            {
                if (string.IsNullOrEmpty(text)) return false;
                
                // Common broken patterns from localization keys
                var placeholderPatterns = new[]
                {
                    "insights��tle",
                    "plan��ps",
                    "courses�dura�on",
                    "summary�kpis",
                    "clusters�analysis",
                    "��", // Generic double replacement char
                };
                
                return placeholderPatterns.Any(pattern => text.Contains(pattern, StringComparison.OrdinalIgnoreCase));
            }

            /// <summary>
            /// Verify text is clean (no garbled chars, no placeholders)
            /// </summary>
            public static bool VerifyCleanText(string text)
            {
                return !HasGarbledCharacters(text) && !HasPlaceholderKeys(text);
            }
        }

        /// <summary>
        /// Legacy alias for backwards compatibility - prefer ArText.Sanitize
        /// </summary>
        public static class ArabicNormalization
        {
            /// <summary>
            /// Normalize Arabic text to NFC form
            /// </summary>
            [Obsolete("Use ArText.Sanitize() instead")]
            public static string NormalizeArabic(string text)
            {
                return ArText.Sanitize(text);
            }

            /// <summary>
            /// Check if text contains garbled characters
            /// </summary>
            [Obsolete("Use ArText.HasGarbledCharacters() instead")]
            public static bool HasGarbledCharacters(string text)
            {
                return ArText.HasGarbledCharacters(text);
            }

            /// <summary>
            /// Verify text is clean (no garbled chars, properly normalized)
            /// </summary>
            [Obsolete("Use ArText.VerifyCleanText() instead")]
            public static bool VerifyCleanText(string text)
            {
                return ArText.VerifyCleanText(text);
            }
        }
    }
}
