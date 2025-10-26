using SkiaSharp;
using SkiaSharp.HarfBuzz;
using System.Text.RegularExpressions;

namespace PsyApi.Services.Reports
{
    /// <summary>
    /// Dedicated text renderer for proper Arabic typography with HarfBuzz shaping
    /// Ensures no broken glyphs (�) and proper RTL text handling
    /// </summary>
    public static class ArabicTextRenderer
    {
        private static readonly Regex ArabicRegex = new(@"[\u0600-\u06FF\u0750-\u077F\u08A0-\u08FF\uFB50-\uFDFF\uFE70-\uFEFF]", RegexOptions.Compiled);
        
        /// <summary>
        /// Detect if text contains Arabic characters requiring HarfBuzz shaping
        /// </summary>
        public static bool ContainsArabic(string text)
        {
            return !string.IsNullOrEmpty(text) && ArabicRegex.IsMatch(text);
        }
        
        /// <summary>
        /// Render text with proper Arabic shaping using HarfBuzz
        /// Returns rendered text bytes suitable for QuestPDF image embedding
        /// </summary>
        public static byte[] RenderArabicText(
            string text, 
            float fontSize = 12f, 
            string color = "#111111",
            bool isBold = false,
            int maxWidth = 300,
            int maxHeight = 100)
        {
            if (string.IsNullOrEmpty(text)) return new byte[0];
            
            // Ensure Western numerals
            text = ReportTheme.ConvertArabicNumeralsToWestern(text);
            
            var colorSK = ReportTheme.FromHex(color);
            
            // Create bitmap for text rendering
            using var bitmap = new SKBitmap(maxWidth, maxHeight);
            using var canvas = new SKCanvas(bitmap);
            
            canvas.Clear(SKColors.Transparent);
            
            try
            {
                if (ContainsArabic(text))
                {
                    // Use HarfBuzz for Arabic text
                    RenderWithHarfBuzz(canvas, text, fontSize, colorSK, isBold, maxWidth, maxHeight);
                }
                else
                {
                    // Use standard rendering for Latin/numeric text
                    RenderWithStandardFont(canvas, text, fontSize, colorSK, isBold, maxWidth, maxHeight);
                }
                
                // Encode as PNG bytes
                using var image = SKImage.FromBitmap(bitmap);
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                return data.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ArabicTextRenderer] Error rendering text '{text}': {ex.Message}");
                // Fallback to standard rendering
                return RenderFallbackText(text, fontSize, colorSK, maxWidth, maxHeight);
            }
        }
        
        /// <summary>
        /// Render text using HarfBuzz shaping for proper Arabic typography
        /// </summary>
        private static void RenderWithHarfBuzz(
            SKCanvas canvas, 
            string text, 
            float fontSize, 
            SKColor color,
            bool isBold,
            int maxWidth,
            int maxHeight)
        {
            using var typeface = SKTypeface.FromFamilyName(
                "Noto Naskh Arabic", 
                isBold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
                SKFontStyleWidth.Normal,
                SKFontStyleSlant.Upright);
                
            using var font = new SKFont(typeface, fontSize);
            using var shaper = new SKShaper(typeface);
            using var paint = new SKPaint 
            { 
                Color = color,
                IsAntialias = true
            };
            
            // Calculate text positioning (RTL-aware)
            var textBounds = new SKRect();
            font.MeasureText(text, out textBounds);
            
            float x = ContainsArabic(text) ? maxWidth - textBounds.Width - 10 : 10;
            float y = (maxHeight + textBounds.Height) / 2;
            
            // Shape and draw text with modern API
            var shapedText = shaper.Shape(text, font);
            if (shapedText?.Points != null && shapedText.Points.Length > 0)
            {
                canvas.DrawShapedText(shaper, text, x, y, font, paint);
            }
            else
            {
                // Fallback to direct text drawing
                canvas.DrawText(text, x, y, SKTextAlign.Left, font, paint);
            }
        }
        
        /// <summary>
        /// Standard font rendering for Latin/numeric content
        /// </summary>
        private static void RenderWithStandardFont(
            SKCanvas canvas,
            string text,
            float fontSize,
            SKColor color,
            bool isBold,
            int maxWidth,
            int maxHeight)
        {
            using var typeface = SKTypeface.FromFamilyName(
                "Arial",
                isBold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
                SKFontStyleWidth.Normal,
                SKFontStyleSlant.Upright);
                
            using var font = new SKFont(typeface, fontSize);
            if (isBold) font.Embolden = true;
            
            using var paint = new SKPaint
            {
                Color = color,
                IsAntialias = true
            };
            
            var textBounds = new SKRect();
            font.MeasureText(text, out textBounds);
            
            float x = (maxWidth - textBounds.Width) / 2;
            float y = (maxHeight + textBounds.Height) / 2;
            
            canvas.DrawText(text, x, y, SKTextAlign.Center, font, paint);
        }
        
        /// <summary>
        /// Fallback text rendering in case of errors
        /// </summary>
        private static byte[] RenderFallbackText(
            string text,
            float fontSize,
            SKColor color,
            int maxWidth,
            int maxHeight)
        {
            try
            {
                using var bitmap = new SKBitmap(maxWidth, maxHeight);
                using var canvas = new SKCanvas(bitmap);
                canvas.Clear(SKColors.Transparent);
                
                using var typeface = SKTypeface.FromFamilyName("Arial");
                using var font = new SKFont(typeface, fontSize);
                using var paint = new SKPaint
                {
                    Color = color,
                    IsAntialias = true
                };
                
                canvas.DrawText(text, 10, maxHeight / 2, SKTextAlign.Left, font, paint);
                
                using var image = SKImage.FromBitmap(bitmap);
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                return data.ToArray();
            }
            catch
            {
                return new byte[0]; // Empty fallback
            }
        }
        
        /// <summary>
        /// Validate text for broken characters and provide sanitized version
        /// </summary>
        public static (bool isValid, string sanitized) ValidateAndSanitizeText(string text)
        {
            if (string.IsNullOrEmpty(text)) return (true, "");
            
            // Check for broken Unicode characters
            bool hasInvalidChars = text.Contains('\uFFFD'); // Replacement character �
            
            // Sanitize text
            var sanitized = text.Replace('\uFFFD', '?'); // Replace broken chars
            sanitized = ReportTheme.ConvertArabicNumeralsToWestern(sanitized);
            
            return (!hasInvalidChars, sanitized);
        }
        
        /// <summary>
        /// Format Arabic dimension names with proper truncation
        /// </summary>
        public static string FormatDimensionName(string dimensionName, int maxLength = 32)
        {
            if (string.IsNullOrEmpty(dimensionName)) return "";
            
            var (isValid, sanitized) = ValidateAndSanitizeText(dimensionName);
            
            if (sanitized.Length > maxLength)
            {
                return sanitized.Substring(0, maxLength - 3) + "...";
            }
            
            return sanitized;
        }
        
        /// <summary>
        /// Format course descriptions with proper truncation
        /// </summary>
        public static string FormatDescription(string description, int maxLength = 120)
        {
            if (string.IsNullOrEmpty(description)) return "";
            
            var (isValid, sanitized) = ValidateAndSanitizeText(description);
            
            if (sanitized.Length > maxLength)
            {
                return sanitized.Substring(0, maxLength - 3) + "...";
            }
            
            return sanitized;
        }
    }
}