using System.Text.RegularExpressions;

namespace PsyApi.Utils
{
    public static class ArabicTextNormalizer
    {
        public static string Normalize(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var normalized = text.Trim();
                
            // Remove tashkeel (diacritics)
            normalized = Regex.Replace(normalized, @"[\u064B-\u065F\u0670]", "");

            // Normalize alef variants to plain alef
            normalized = Regex.Replace(normalized, @"[أإآٱ]", "ا");

            // Normalize taa marbouta to haa
            normalized = normalized.Replace('ة', 'ه');

            // Normalize alef maksura to yaa
            normalized = normalized.Replace('ى', 'ي');

            // Normalize hamza variants
            normalized = Regex.Replace(normalized, @"[ئؤ]", "ء");

            // Remove double spaces
            normalized = Regex.Replace(normalized, @"\s+", " ");

            // Remove kashida (tatweel)
            normalized = normalized.Replace('ـ', ' ');

            return normalized.Trim();
        }

        public static bool AreEqual(string? text1, string? text2)
        {
            if (text1 == null && text2 == null) return true;
            if (text1 == null || text2 == null) return false;

            var normalized1 = Normalize(text1);
            var normalized2 = Normalize(text2);

            return string.Equals(normalized1, normalized2, StringComparison.Ordinal);
        }
    }
}