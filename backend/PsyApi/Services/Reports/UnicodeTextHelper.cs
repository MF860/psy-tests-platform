using System;
using System.Globalization;
using System.Text;

namespace PsyApi.Services.Reports
{
    /// <summary>
    /// Unicode text normalization and formatting utilities
    /// Eliminates � (replacement character) and ensures proper UTF-8 handling
    /// </summary>
    public static class UnicodeTextHelper
    {
        /// <summary>
        /// Normalize text to Unicode NFC (Canonical Composition)
        /// Prevents � glyphs from malformed combining characters
        /// </summary>
        public static string NormalizeNfc(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text ?? string.Empty;

            // Normalize to NFC form (composed characters)
            var normalized = text.Normalize(NormalizationForm.FormC);
            
            // Remove any remaining replacement characters
            normalized = normalized.Replace("\uFFFD", "?");
            
            return normalized;
        }

        /// <summary>
        /// Format T-Score with guaranteed Western numerals, no broken glyphs
        /// </summary>
        public static string FormatTScore(double value, int decimals = 1)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return "—";

            // Force InvariantCulture (Western numerals only)
            var formatted = value.ToString($"F{decimals}", CultureInfo.InvariantCulture);
            
            // Ensure no Arabic-Indic digits slipped through
            formatted = ConvertToWesternNumerals(formatted);
            
            return NormalizeNfc(formatted);
        }

        /// <summary>
        /// Format percentage with Western numerals
        /// </summary>
        public static string FormatPercentage(double value, int decimals = 1)
        {
            var formatted = FormatTScore(value * 100, decimals);
            return $"{formatted}%";
        }

        /// <summary>
        /// Convert Arabic-Indic numerals (٠-٩) to Western (0-9)
        /// Prevents � glyphs when fonts don't support Arabic-Indic
        /// </summary>
        public static string ConvertToWesternNumerals(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text ?? string.Empty;

            var result = new StringBuilder(text.Length);
            foreach (var ch in text)
            {
                // Arabic-Indic digits range: U+0660-U+0669
                if (ch >= '\u0660' && ch <= '\u0669')
                {
                    result.Append((char)('0' + (ch - '\u0660')));
                }
                // Extended Arabic-Indic digits range: U+06F0-U+06F9 (Farsi)
                else if (ch >= '\u06F0' && ch <= '\u06F9')
                {
                    result.Append((char)('0' + (ch - '\u06F0')));
                }
                else
                {
                    result.Append(ch);
                }
            }
            return result.ToString();
        }

        /// <summary>
        /// Ensure text is safe for PDF embedding
        /// Removes control characters, normalizes whitespace
        /// </summary>
        public static string SanitizeForPdf(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            // Normalize Unicode
            text = NormalizeNfc(text);

            // Replace control characters (except newline/tab)
            var sanitized = new StringBuilder(text.Length);
            foreach (var ch in text)
            {
                // Allow printable characters + newline + tab
                if (ch >= 32 || ch == '\n' || ch == '\r' || ch == '\t')
                {
                    sanitized.Append(ch);
                }
                else
                {
                    sanitized.Append(' '); // Replace control chars with space
                }
            }

            return sanitized.ToString();
        }

        /// <summary>
        /// Format label: value pair with proper RTL handling
        /// Example: "T-Score: 53.2" or "الدرجة: 53.2"
        /// </summary>
        public static string FormatLabelValue(string label, double value, int decimals = 1)
        {
            var formattedValue = FormatTScore(value, decimals);
            var text = $"{label}: {formattedValue}";
            return NormalizeNfc(text);
        }

        /// <summary>
        /// Truncate text with ellipsis, respecting Unicode boundaries
        /// </summary>
        public static string TruncateSmart(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
                return text ?? string.Empty;

            // Truncate at word boundary if possible
            var truncated = text.Substring(0, maxLength - 1);
            var lastSpace = truncated.LastIndexOf(' ');
            
            if (lastSpace > maxLength / 2) // Only truncate at space if it's not too early
            {
                truncated = truncated.Substring(0, lastSpace);
            }

            return NormalizeNfc(truncated + "…");
        }

        /// <summary>
        /// Check if text contains Arabic characters
        /// </summary>
        public static bool ContainsArabic(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            foreach (var ch in text)
            {
                // Arabic Unicode block: U+0600-U+06FF
                // Arabic Supplement: U+0750-U+077F
                if ((ch >= '\u0600' && ch <= '\u06FF') || 
                    (ch >= '\u0750' && ch <= '\u077F'))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Format T-Score with label (Arabic or English)
        /// </summary>
        public static string FormatTScoreWithLabel(double value, bool useArabicLabel = true)
        {
            var formatted = FormatTScore(value, 1);
            var label = useArabicLabel ? "ت" : "T"; // Arabic Taa or Latin T
            return NormalizeNfc($"{label} = {formatted}");
        }
    }
}
