using SkiaSharp;
using SkiaSharp.HarfBuzz;
using HarfBuzzSharp;
using System.Globalization;

namespace PsyApi.Services.Reports
{
    public static class RadialGaugeRenderer
    {
        private static SKTypeface? _arabicTypeface;
        private static SKShaper? _arabicShaper;
        private static readonly object _lock = new();

        /// <summary>
        /// Draw a modern radial gauge with smooth arcs and anti-aliasing (100-110px size specification)
        /// </summary>
        public static byte[] DrawRadialGauge(
            double tScore,
            int size = 110,
            float thicknessPct = 0.16f,
            string centerLine1 = "",
            string centerLine2 = "")
        {
            EnsureArabicFont();
            
            // Create high-DPI surface for smooth rendering
            var scaleFactor = 2f; // 2x for anti-aliasing
            var actualSize = (int)(size * scaleFactor);
            
            using var surface = SKSurface.Create(new SKImageInfo(actualSize, actualSize));
            var canvas = surface.Canvas;
            canvas.Scale(scaleFactor);
            canvas.Clear(SKColors.Transparent);

            var centerX = size / 2f;
            var centerY = size / 2f;
            var strokeWidth = size * thicknessPct;
            var radius = (size - strokeWidth) / 2f;

            // Improved T-score normalization (0-100 range, better scaling)
            var normalizedScore = Math.Clamp(tScore / 100.0, 0.0, 1.0);
            var sweepAngle = (float)(normalizedScore * 280); // 280 degrees for almost full circle

            var rect = SKRect.Create(centerX - radius, centerY - radius, radius * 2, radius * 2);

            using var paint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = strokeWidth,
                StrokeCap = SKStrokeCap.Round // Smooth rounded ends
            };

            // Draw background (unfilled) ring with subtle shadow effect
            using var shadowPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = strokeWidth + 1,
                Color = SKColors.Black.WithAlpha(20),
                MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 1f)
            };
            canvas.DrawArc(rect, 140, 260, false, shadowPaint);

            // Draw background ring
            paint.Color = ReportTheme.FromHex(ReportTheme.Colors.Unfilled);
            canvas.DrawArc(rect, 140, 260, false, paint); // Start at 140° for better visual balance

            // Draw filled arc with gradient effect for band color
            var bandColor = ReportTheme.BandColor(tScore);
            using var gradientPaint = CreateGradientPaint(bandColor, strokeWidth, rect);
            if (sweepAngle > 0)
            {
                canvas.DrawArc(rect, 140, sweepAngle, false, gradientPaint);
            }

            // Draw center text with proper Arabic shaping
            DrawCenterTextWithShaping(canvas, centerX, centerY, size, centerLine1, centerLine2, bandColor);

            // Convert to PNG with downscaling for smooth result
            using var image = surface.Snapshot();
            using var resizedBitmap = new SKBitmap(size, size);
            
            // Use modern scaling approach
            var pixmap = resizedBitmap.PeekPixels();
            var samplingOptions = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);
            image.ScalePixels(pixmap, samplingOptions);
            
            using var finalImage = SKImage.FromBitmap(resizedBitmap);
            using var data = finalImage.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }
        
        /// <summary>
        /// Create gradient paint for smooth band color rendering
        /// </summary>
        private static SKPaint CreateGradientPaint(string baseColor, float strokeWidth, SKRect rect)
        {
            var baseColorSK = ReportTheme.FromHex(baseColor);
            var lighterColor = baseColorSK.WithAlpha((byte)(baseColorSK.Alpha * 0.8f));
            
            var colors = new[] { baseColorSK, lighterColor };
            var positions = new float[] { 0f, 1f };
            
            // Calculate center point manually
            var centerX = rect.Left + rect.Width / 2f;
            var centerY = rect.Top + rect.Height / 2f;
            var centerPoint = new SKPoint(centerX, centerY);
            
            var gradient = SKShader.CreateRadialGradient(
                centerPoint,
                rect.Width / 2f,
                colors,
                positions,
                SKShaderTileMode.Clamp);
                
            return new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = strokeWidth,
                StrokeCap = SKStrokeCap.Round,
                Shader = gradient
            };
        }

        /// <summary>
        /// Draw center text with proper Arabic shaping using HarfBuzz
        /// </summary>
        private static void DrawCenterTextWithShaping(
            SKCanvas canvas,
            float centerX,
            float centerY,
            int size,
            string line1,
            string line2,
            string textColor)
        {
            var textPaint = new SKPaint
            {
                IsAntialias = true,
                Color = ReportTheme.FromHex(textColor)
            };

            // Draw first line (dimension name - larger)
            if (!string.IsNullOrEmpty(line1))
            {
                var fontSize1 = size * 0.08f;
                var font1 = new SKFont(_arabicTypeface ?? SKTypeface.Default, fontSize1);
                var line1Y = centerY - (size * 0.02f); // Slightly above center
                
                // Use HarfBuzz shaping for proper Arabic rendering
                if (_arabicShaper != null && ContainsArabic(line1))
                {
                    var result1 = _arabicShaper.Shape(line1, font1);
                    // Calculate text width for centering
                    var textWidth = result1.Points.LastOrDefault().X;
                    var adjustedX1 = centerX - textWidth / 2f;
                    canvas.DrawShapedText(_arabicShaper, line1, adjustedX1, line1Y, font1, textPaint);
                }
                else
                {
                    // Fallback for non-Arabic text
                    canvas.DrawText(line1, centerX, line1Y, SKTextAlign.Center, font1, textPaint);
                }
            }

            // Draw second line (T-score and percentile - smaller)
            if (!string.IsNullOrEmpty(line2))
            {
                var fontSize2 = size * 0.06f;
                var font2 = new SKFont(_arabicTypeface ?? SKTypeface.Default, fontSize2);
                textPaint.Color = ReportTheme.FromHex(ReportTheme.Colors.TextSecondary);
                var line2Y = centerY + (size * 0.04f); // Below center
                
                // Numbers are always LTR, so use standard rendering
                canvas.DrawText(line2, centerX, line2Y, SKTextAlign.Center, font2, textPaint);
            }
        }

        /// <summary>
        /// Check if text contains Arabic characters
        /// </summary>
        private static bool ContainsArabic(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            
            foreach (char c in text)
            {
                // Arabic Unicode range: U+0600 to U+06FF
                if (c >= 0x0600 && c <= 0x06FF)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Create a modern gauge for a dimension (100-110px specification)
        /// UPDATED: Support empty center text as per new requirements (no text inside donut)
        /// </summary>
        public static byte[] CreateDimensionGauge(double tScore, double percentile, string dimensionName, int size = 110)
        {
            // If dimensionName is empty, create donut with no center text (as specified)
            if (string.IsNullOrEmpty(dimensionName))
            {
                return DrawRadialGauge(tScore, size, 0.16f, "", "");
            }
            
            // Use ArabicTextRenderer for proper text handling (backward compatibility)
            var sanitizedName = ArabicTextRenderer.FormatDimensionName(dimensionName, 32);
            var centerLine1 = TruncateArabicText(sanitizedName, 14); // Fit in gauge center
            var centerLine2 = $"T={ReportTheme.FormatTScore(tScore)} | %{ReportTheme.FormatPercentile(percentile)}";

            return DrawRadialGauge(tScore, size, 0.16f, centerLine1, centerLine2);
        }

        /// <summary>
        /// Truncate Arabic text to fit in gauge center
        /// </summary>
        private static string TruncateArabicText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
                return text;

            return text.Substring(0, maxLength - 1) + "…";
        }

        /// <summary>
        /// Initialize Arabic font and shaper for proper text rendering
        /// </summary>
        private static void EnsureArabicFont()
        {
            if (_arabicTypeface != null && _arabicShaper != null) return;

            lock (_lock)
            {
                if (_arabicTypeface != null && _arabicShaper != null) return;

                try
                {
                    var fontsDir = Path.Combine(AppContext.BaseDirectory, "Resources", "Fonts");
                    var regularPath = Path.Combine(fontsDir, "NotoNaskhArabic-Regular.ttf");
                    
                    if (File.Exists(regularPath))
                    {
                        using var fontStream = File.OpenRead(regularPath);
                        _arabicTypeface = SKTypeface.FromStream(fontStream);
                        
                        // Initialize HarfBuzz shaper for proper Arabic text shaping
                        _arabicShaper = new SKShaper(_arabicTypeface);
                        
                        Console.WriteLine("[RadialGaugeRenderer] ✓ Arabic font loaded: Noto Naskh Arabic Regular");
                        Console.WriteLine("[RadialGaugeRenderer] ✓ HarfBuzz shaper initialized for Arabic text");
                    }
                    else
                    {
                        Console.WriteLine($"[RadialGaugeRenderer] ⚠ Arabic font not found at: {regularPath}");
                        _arabicTypeface = SKTypeface.Default;
                        _arabicShaper = new SKShaper(_arabicTypeface);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[RadialGaugeRenderer] ❌ Font loading failed: {ex.Message}");
                    // Fallback to default font if Arabic font loading fails
                    _arabicTypeface = SKTypeface.Default;
                    _arabicShaper = new SKShaper(_arabicTypeface);
                }
            }
        }
    }
}