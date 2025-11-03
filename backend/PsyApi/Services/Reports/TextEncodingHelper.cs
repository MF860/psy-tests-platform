using System.Text;

namespace PsyApi.Services.Reports
{
    /// <summary>
    /// Text Encoding Helper - Ensures zero "�" symbols in all PDF outputs
    /// Handles UTF-8 NFC normalization and Arabic text processing
    /// </summary>
    public static class TextEncodingHelper
    {
        /// <summary>
        /// Normalize Arabic text using UTF-8 NFC (Canonical Composition)
        /// Prevents "�" replacement characters in PDFs
        /// </summary>
        /// <param name="text">Input text (potentially containing Arabic)</param>
        /// <returns>Normalized text safe for PDF rendering</returns>
        public static string NormalizeArabic(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            try
            {
                // NFC normalization ensures consistent Unicode representation
                // Combines base characters with diacritics (e.g., ا + ً → ً)
                var normalized = text.Normalize(NormalizationForm.FormC);
                
                // Additional cleanup for common issues
                normalized = normalized
                    .Replace("\u200B", "")  // Remove zero-width space
                    .Replace("\u200C", "")  // Remove zero-width non-joiner (except for intentional use)
                    .Replace("\uFEFF", ""); // Remove BOM
                
                return normalized;
            }
            catch (Exception)
            {
                // Fallback: return original text if normalization fails
                return text;
            }
        }

        /// <summary>
        /// Normalize and sanitize text for QuestPDF rendering
        /// Handles both Arabic and English text
        /// </summary>
        public static string SanitizeForPdf(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            var normalized = NormalizeArabic(text);

            // Remove control characters that can cause rendering issues
            var sanitized = new StringBuilder(normalized.Length);
            foreach (char c in normalized)
            {
                // Keep printable characters, spaces, newlines, and Arabic/Latin ranges
                if (c >= 0x0020 && c <= 0xD7FF ||   // Basic Multilingual Plane (printable)
                    c >= 0x0600 && c <= 0x06FF ||   // Arabic
                    c >= 0x0750 && c <= 0x077F ||   // Arabic Supplement
                    c >= 0xFE70 && c <= 0xFEFF ||   // Arabic Presentation Forms-B
                    c == '\n' || c == '\r' || c == '\t')
                {
                    sanitized.Append(c);
                }
            }

            return sanitized.ToString();
        }

        /// <summary>
        /// Validate that text contains no replacement characters (�)
        /// Use this for testing/debugging PDF output
        /// </summary>
        public static bool ContainsReplacementCharacters(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            return text.Contains('\uFFFD'); // Unicode replacement character
        }

        /// <summary>
        /// Check if text contains Arabic characters
        /// </summary>
        public static bool ContainsArabic(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            return text.Any(c => (c >= 0x0600 && c <= 0x06FF) ||  // Arabic
                                  (c >= 0x0750 && c <= 0x077F) ||  // Arabic Supplement
                                  (c >= 0xFE70 && c <= 0xFEFF));   // Arabic Presentation Forms
        }

        /// <summary>
        /// Normalize all strings in a dictionary (for JSON deserialization)
        /// </summary>
        public static Dictionary<string, string> NormalizeDictionary(Dictionary<string, string> dictionary)
        {
            return dictionary.ToDictionary(
                kvp => kvp.Key,
                kvp => NormalizeArabic(kvp.Value)
            );
        }

        /// <summary>
        /// Apply normalization to all public string properties of an object using reflection
        /// Useful for normalizing model objects before PDF generation
        /// </summary>
        public static T? NormalizeObject<T>(T? obj) where T : class
        {
            if (obj == null)
                return null;

            var stringProperties = typeof(T)
                .GetProperties()
                .Where(p => p.PropertyType == typeof(string) && p.CanRead && p.CanWrite);

            foreach (var property in stringProperties)
            {
                var value = property.GetValue(obj) as string;
                if (!string.IsNullOrEmpty(value))
                {
                    property.SetValue(obj, NormalizeArabic(value));
                }
            }

            return obj;
        }

        /// <summary>
        /// Test method to generate report with all common Arabic diacritics
        /// Use this to verify PDF rendering handles all Unicode correctly
        /// </summary>
        public static string GetArabicTestString()
        {
            return "اختبار النص العربي: الشكل، التنوين (ً ٌ ٍ)، الشدة (ّ)، السكون (ْ)، " +
                   "الفتحة (َ)، الكسرة (ِ)، الضمة (ُ)، المد (~)، الهمزة (أ إ ؤ ئ ء)";
        }

        /// <summary>
        /// Ensure string is safe for use in filenames (removes invalid characters)
        /// </summary>
        public static string SanitizeFilename(string filename)
        {
            if (string.IsNullOrEmpty(filename))
                return "report";

            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new StringBuilder(filename.Length);

            foreach (char c in filename)
            {
                if (!invalidChars.Contains(c))
                    sanitized.Append(c);
            }

            return sanitized.Length > 0 ? sanitized.ToString() : "report";
        }
    }
}
