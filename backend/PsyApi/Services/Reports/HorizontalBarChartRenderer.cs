using SkiaSharp;
using SkiaSharp.HarfBuzz;
using PsyApi.Models;
using System.Globalization;
using System.Linq;

namespace PsyApi.Services.Reports
{
    /// <summary>
    /// Ultra Hi-Fi Horizontal Bar Chart with Design Tokens Integration
    /// Upgraded for 600 DPI+ output, RTL label wrapping, and outside value annotations.
    /// </summary>
    public static class HorizontalBarChartRenderer
    {
        private static SKTypeface? _arabicTypeface;
        private static SKShaper? _arabicShaper;
        private static readonly object _lock = new();

        /// <summary>
        /// Render horizontal bar chart with 600 DPI quality and text halos
        /// </summary>
        /// <param name="dimensions">Dimension scores</param>
        /// <param name="width">Chart width (520-600px ideal)</param>
        /// <param name="maxDimensions">Maximum dimensions to show (default 12)</param>
        /// <param name="dpi">Rendering DPI (minimum 600 for editorial quality)</param>
        /// <returns>PNG image</returns>
        public static byte[] RenderHorizontalBars(
            IEnumerable<DimensionScore> dimensions,
            int width = 600,
            int maxDimensions = 12,
            int dpi = 600)
        {
            EnsureArabicFont();

            var sortedDimensions = dimensions
                .OrderByDescending(d => d.T) // تنازلياً: الأقوى أولاً
                .Take(maxDimensions)
                .ToList();

            if (sortedDimensions.Count == 0)
                throw new ArgumentException("No dimensions provided");

            // حساب الارتفاع ديناميكياً: زيادة المسافة لاستيعاب اللف RTL
            var barHeight = 18f;
            var barSpacing = 12f;
            var topMargin = 70f;
            var bottomMargin = 48f;
            var leftMargin = 220f; // مساحة إضافية لتغليف أسماء الأبعاد الطويلة
            var rightMargin = 110f;  // مساحة للقيم خارج الأعمدة

            var plotHeight = sortedDimensions.Count * (barHeight + barSpacing);
            var totalHeight = (int)(topMargin + plotHeight + bottomMargin);

            var effectiveDpi = Math.Max(600, dpi);
            var scaleFactor = effectiveDpi / 96f; // 96 DPI baseline for pixel space
            var actualWidth = (int)Math.Ceiling(width * scaleFactor);
            var actualHeight = (int)Math.Ceiling(totalHeight * scaleFactor);

            using var surface = SKSurface.Create(new SKImageInfo(actualWidth, actualHeight, SKColorType.Rgba8888, SKAlphaType.Premul));
            var canvas = surface.Canvas;
            canvas.Scale(scaleFactor);
            canvas.Clear(SKColors.White);

            // رسم العنوان
            DrawTitle(canvas, "ترتيب الأبعاد تنازلياً حسب T-Score", width / 2f, 28f);

            // رسم الأسطورة (Legend)
            DrawLegend(canvas, leftMargin, topMargin - 32f);

            // حساب مساحة الرسم
            var plotWidth = width - leftMargin - rightMargin;

            // رسم محور X (T-scores: 0-100)
            DrawXAxis(canvas, leftMargin, topMargin, plotWidth);

            // رسم الأعمدة
            DrawBars(canvas, sortedDimensions, leftMargin, topMargin, plotWidth, barHeight, barSpacing);

            // تصدير PNG بدون إعادة تحجيم للحفاظ على دقة 600 DPI
            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }

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
                        _arabicTypeface = SKTypeface.FromFile(regularPath);
                        _arabicShaper = new SKShaper(_arabicTypeface);
                        Console.WriteLine("[HorizontalBarChart] ✓ Noto Naskh Arabic + HarfBuzz ready");
                    }
                    else
                    {
                        Console.WriteLine($"[HorizontalBarChart] ⚠ Arabic font not found at: {regularPath}");
                        _arabicTypeface = SKTypeface.Default;
                        _arabicShaper = new SKShaper(_arabicTypeface);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[HorizontalBarChart] ⚠ Font loading error: {ex.Message}");
                    _arabicTypeface = SKTypeface.Default;
                    _arabicShaper = new SKShaper(_arabicTypeface);
                }
            }
        }

        private static void DrawTitle(SKCanvas canvas, string title, float x, float y)
        {
            // Halo effect for title text
            using var haloPaint = new SKPaint
            {
                IsAntialias = true,
                Color = SKColors.White,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 4f
            };

            using var paint = new SKPaint
            {
                IsAntialias = true,
                Color = SKColor.Parse(DesignTokens.Colors.Text)
            };

            var font = new SKFont(_arabicTypeface ?? SKTypeface.Default, 16f) { Embolden = true };

            if (_arabicShaper != null && ContainsArabic(title))
            {
                var result = _arabicShaper.Shape(title, font);
                if (result?.Points != null && result.Points.Length > 0)
                {
                    var textWidth = result.Points.LastOrDefault().X;
                    var textX = x - textWidth / 2f;
                    
                    // Draw halo
                    canvas.DrawShapedText(_arabicShaper, title, textX, y, font, haloPaint);
                    // Draw text
                    canvas.DrawShapedText(_arabicShaper, title, textX, y, font, paint);
                    return;
                }
            }

            canvas.DrawText(title, x, y, SKTextAlign.Center, font, paint);
        }

        private static void DrawLegend(SKCanvas canvas, float anchorX, float startY)
        {
            var legends = new[]
            {
                (Color: DesignTokens.Colors.ChartExcellent, Label: "ممتاز ≥65"),
                (Color: DesignTokens.Colors.ChartGood, Label: "جيد 55-64"),
                (Color: DesignTokens.Colors.ChartAverage, Label: "متوسط 40-54"),
                (Color: DesignTokens.Colors.ChartWeak, Label: "ضعيف <40")
            };

            var boxSize = 10f;
            var rowGap = 6f;
            var font = new SKFont(_arabicTypeface ?? SKTypeface.Default, 10f);

            using var boxPaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill };
            using var textPaint = new SKPaint { IsAntialias = true, Color = SKColor.Parse(DesignTokens.Colors.TextSecondary) };

            float currentY = startY;

            foreach (var (Color, Label) in legends)
            {
                var boxRect = new SKRect(anchorX - boxSize, currentY, anchorX, currentY + boxSize);
                boxPaint.Color = SKColor.Parse(Color);
                canvas.DrawRoundRect(boxRect, 2f, 2f, boxPaint);

                var labelBaseline = currentY + boxSize - 2f;
                var labelRight = boxRect.Left - 8f;

                if (_arabicShaper != null && ContainsArabic(Label))
                {
                    var shaped = _arabicShaper.Shape(Label, font);
                    if (shaped?.Points != null && shaped.Points.Length > 0)
                    {
                        var textWidth = shaped.Points.LastOrDefault().X;
                        canvas.DrawShapedText(_arabicShaper, Label, labelRight - textWidth, labelBaseline, font, textPaint);
                        currentY += boxSize + rowGap;
                        continue;
                    }
                }

                canvas.DrawText(Label, labelRight, labelBaseline, SKTextAlign.Right, font, textPaint);
                currentY += boxSize + rowGap;
            }
        }

        private static void DrawXAxis(SKCanvas canvas, float leftMargin, float y, float plotWidth)
        {
            using var axisPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                Color = SKColor.Parse("#CBD5E1"), // أدكن قليلاً حسب المواصفات
                StrokeWidth = 1.5f
            };

            canvas.DrawLine(leftMargin, y, leftMargin + plotWidth, y, axisPaint);

            using var textPaint = new SKPaint { IsAntialias = true, Color = SKColor.Parse("#6B7280") };
            var font = new SKFont(SKTypeface.Default, 10f);

            // تسميات المحور (0, 20, 40, 60, 80, 100)
            for (int t = 0; t <= 100; t += 20)
            {
                var x = leftMargin + (plotWidth * t / 100f);
                
                // علامة صغيرة
                canvas.DrawLine(x, y - 3, x, y + 3, axisPaint);
                
                // النص (Western digits)
                var label = t.ToString(CultureInfo.InvariantCulture);
                canvas.DrawText(label, x, y - 8, SKTextAlign.Center, font, textPaint);
            }
        }

        private static void DrawBars(
            SKCanvas canvas,
            List<DimensionScore> dimensions,
            float leftMargin,
            float topMargin,
            float plotWidth,
            float barHeight,
            float barSpacing)
        {
            var font = new SKFont(_arabicTypeface ?? SKTypeface.Default, 12f);
            var valueFont = new SKFont(SKTypeface.Default, 11f) { Embolden = true };

            using var labelPaint = new SKPaint { IsAntialias = true, Color = SKColor.Parse(DesignTokens.Colors.Text) };
            using var barPaint = new SKPaint { IsAntialias = true };

            for (int i = 0; i < dimensions.Count; i++)
            {
                var dim = dimensions[i];
                var y = topMargin + (i * (barHeight + barSpacing)) + barSpacing;

                // رسم تسمية البُعد (RTL على اليمين) مع تغليف أسطر
                DrawDimensionLabel(canvas, dim.Dimension, leftMargin - 16, y + barHeight / 2f, font, labelPaint, barHeight);

                // حساب عرض الشريط
                var barWidth = (float)Math.Max(1, (plotWidth * Math.Min(100, Math.Max(0, dim.T))) / 100.0);
                
                // تحديد اللون حسب Band
                var barColor = GetBandColor(dim.T);
                barPaint.Color = barColor;

                // رسم الشريط مع antialiasing
                var barRect = SKRect.Create(leftMargin, y, barWidth, barHeight);
                canvas.DrawRect(barRect, barPaint);

                // رسم حد خفيف
                using var strokePaint = new SKPaint
                {
                    IsAntialias = true,
                    Style = SKPaintStyle.Stroke,
                    Color = barColor.WithAlpha(180),
                    StrokeWidth = 1.0f
                };
                canvas.DrawRect(barRect, strokePaint);

                // رسم القيمة T خارج الشريط مع خلفية خفيفة
                var tValue = dim.T.ToString("F1", CultureInfo.InvariantCulture);
                var textX = leftMargin + barWidth + 18f;
                var textWidth = MeasureTextWidth(tValue, valueFont);
                var badgeWidth = Math.Max(52f, textWidth + 26f);
                var valueBackground = SKRect.Create(textX - 14f, y - 3f, badgeWidth, barHeight + 8f);
                var textY = y + barHeight / 2f + (valueFont.Size * 0.35f);

                using (var badgePaint = new SKPaint
                {
                    IsAntialias = true,
                    Color = SKColor.Parse(DesignTokens.Colors.SurfaceHover)
                })
                {
                    canvas.DrawRoundRect(valueBackground, 6f, 6f, badgePaint);
                }

                using (var badgeStroke = new SKPaint
                {
                    IsAntialias = true,
                    Style = SKPaintStyle.Stroke,
                    Color = SKColor.Parse(DesignTokens.Colors.Border),
                    StrokeWidth = 1f
                })
                {
                    canvas.DrawRoundRect(valueBackground, 6f, 6f, badgeStroke);
                }

                using var tValuePaint = new SKPaint { IsAntialias = true, Color = SKColor.Parse(DesignTokens.Colors.Text) };
                canvas.DrawText(tValue, valueBackground.MidX, textY, SKTextAlign.Center, valueFont, tValuePaint);
            }
        }

        private static void DrawDimensionLabel(
            SKCanvas canvas,
            string dimensionName,
            float x,
            float y,
            SKFont font,
            SKPaint paint,
            float barHeight)
        {
            var maxWidth = Math.Max(40f, x - 40f);
            var lines = WrapText(dimensionName, font, maxWidth);
            if (lines.Count == 0)
                lines.Add("غير محدد");

            var lineHeight = font.Size * 1.25f;
            var blockHeight = lineHeight * lines.Count;
            var baselineStart = y - (blockHeight / 2f) + lineHeight - (barHeight * 0.15f);

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                var baseline = baselineStart + i * lineHeight;

                if (_arabicShaper != null && ContainsArabic(line))
                {
                    var shaped = _arabicShaper.Shape(line, font);
                    if (shaped?.Points != null && shaped.Points.Length > 0)
                    {
                        var textWidth = shaped.Points.LastOrDefault().X;
                        canvas.DrawShapedText(_arabicShaper, line, x - textWidth, baseline, font, paint);
                        continue;
                    }
                }

                canvas.DrawText(line, x, baseline, SKTextAlign.Right, font, paint);
            }
        }

        private static SKColor GetBandColor(double tScore)
        {
            // Use DesignTokens performance band colors
            if (tScore >= 65) return SKColor.Parse(DesignTokens.Colors.ChartExcellent);  // ≥65: Excellent
            if (tScore >= 55) return SKColor.Parse(DesignTokens.Colors.ChartGood);       // 55-64: Good
            if (tScore >= 40) return SKColor.Parse(DesignTokens.Colors.ChartAverage);    // 40-54: Average
            return SKColor.Parse(DesignTokens.Colors.ChartWeak);                          // <40: Weak
        }

        private static bool ContainsArabic(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            return text.Any(c => c >= 0x0600 && c <= 0x06FF);
        }

        private static List<string> WrapText(string text, SKFont font, float maxWidth)
        {
            var lines = new List<string>();
            if (string.IsNullOrWhiteSpace(text))
                return lines;

            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0)
                return lines;

            var currentLine = words[0];

            for (int i = 1; i < words.Length; i++)
            {
                var candidate = currentLine + " " + words[i];
                if (MeasureTextWidth(candidate, font) <= maxWidth)
                {
                    currentLine = candidate;
                }
                else
                {
                    lines.Add(currentLine);
                    currentLine = words[i];
                }
            }

            lines.Add(currentLine);

            var wrapped = new List<string>();
            foreach (var line in lines)
            {
                wrapped.AddRange(BreakLineToFit(line, font, maxWidth));
            }

            return wrapped;
        }

        private static IEnumerable<string> BreakLineToFit(string line, SKFont font, float maxWidth)
        {
            if (MeasureTextWidth(line, font) <= maxWidth || line.Length <= 1)
                return new[] { line };

            var segments = new List<string>();
            var buffer = new List<char>();

            foreach (var character in line)
            {
                buffer.Add(character);
                var segment = new string(buffer.ToArray());

                if (MeasureTextWidth(segment, font) > maxWidth)
                {
                    if (buffer.Count > 1)
                    {
                        buffer.RemoveAt(buffer.Count - 1);
                        if (buffer.Count > 0)
                            segments.Add(new string(buffer.ToArray()).Trim());
                        buffer.Clear();
                        buffer.Add(character);
                    }
                    else
                    {
                        segments.Add(segment.Trim());
                        buffer.Clear();
                    }
                }
            }

            if (buffer.Count > 0)
                segments.Add(new string(buffer.ToArray()).Trim());

            return segments.Where(s => !string.IsNullOrWhiteSpace(s));
        }

        private static float MeasureTextWidth(string text, SKFont font)
        {
            if (string.IsNullOrEmpty(text))
                return 0f;

            if (_arabicShaper != null && ContainsArabic(text))
            {
                var shaped = _arabicShaper.Shape(text, font);
                if (shaped?.Points != null && shaped.Points.Length > 0)
                    return shaped.Points.LastOrDefault().X;
            }

            font.MeasureText(text, out var bounds);
            return bounds.Width;
        }
    }
}
