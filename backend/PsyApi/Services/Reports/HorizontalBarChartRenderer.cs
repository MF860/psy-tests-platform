using SkiaSharp;
using SkiaSharp.HarfBuzz;
using PsyApi.Models;
using System.Globalization;

namespace PsyApi.Services.Reports
{
    /// <summary>
    /// مخطط أعمدة أفقي محسّن مع HarfBuzz للعربية وتنسيق احترافي
    /// vNext: 520-600px width, 14-18px bars, RTL labels 11-12pt, T-values at end
    /// </summary>
    public static class HorizontalBarChartRenderer
    {
        private static SKTypeface? _arabicTypeface;
        private static SKShaper? _arabicShaper;
        private static readonly object _lock = new();

        /// <summary>
        /// رسم مخطط أعمدة أفقي مرتب تصاعدياً (الأضعف أولاً)
        /// </summary>
        /// <param name="dimensions">قائمة الأبعاد</param>
        /// <param name="width">عرض المخطط (520-600px مثالي)</param>
        /// <param name="maxDimensions">حد أقصى للأبعاد (default 12)</param>
        /// <returns>صورة PNG</returns>
        public static byte[] RenderHorizontalBars(
            IEnumerable<DimensionScore> dimensions,
            int width = 580,
            int maxDimensions = 12)
        {
            EnsureArabicFont();

            var sortedDimensions = dimensions
                .OrderBy(d => d.T) // تصاعدياً: الأضعف أولاً
                .Take(maxDimensions)
                .ToList();

            if (sortedDimensions.Count == 0)
                throw new ArgumentException("No dimensions provided");

            // حساب الارتفاع ديناميكياً: 14-18px per bar + 10-12px spacing
            var barHeight = 16f;
            var barSpacing = 11f;
            var topMargin = 50f;
            var bottomMargin = 40f;
            var leftMargin = 190f; // Increased from 160f for longer Arabic dimension names
            var rightMargin = 80f;  // للقيم في نهاية الشريط

            var plotHeight = sortedDimensions.Count * (barHeight + barSpacing);
            var totalHeight = (int)(topMargin + plotHeight + bottomMargin);

            // HiFi settings: 3× scale for maximum quality
            var scaleFactor = HiFiSettings.GetScaleFactor();
            var actualWidth = (int)(width * scaleFactor);
            var actualHeight = (int)(totalHeight * scaleFactor);

            using var surface = SKSurface.Create(new SKImageInfo(actualWidth, actualHeight));
            var canvas = surface.Canvas;
            canvas.Scale(scaleFactor);
            canvas.Clear(SKColors.White);

            // رسم العنوان
            DrawTitle(canvas, "ترتيب الأبعاد تصاعدياً حسب T-Score", width / 2f, 20f);

            // رسم الأسطورة (Legend)
            DrawLegend(canvas, width - rightMargin, 35f);

            // حساب مساحة الرسم
            var plotWidth = width - leftMargin - rightMargin;

            // رسم محور X (T-scores: 0-100)
            DrawXAxis(canvas, leftMargin, topMargin, plotWidth);

            // رسم الأعمدة
            DrawBars(canvas, sortedDimensions, leftMargin, topMargin, plotWidth, barHeight, barSpacing);

            // تحويل إلى JPEG بجودة HiFi
            using var image = surface.Snapshot();
            using var resizedBitmap = new SKBitmap(width, totalHeight);
            var pixmap = resizedBitmap.PeekPixels();
            var samplingOptions = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);
            image.ScalePixels(pixmap, samplingOptions);

            using var finalImage = SKImage.FromBitmap(resizedBitmap);
            using var data = finalImage.Encode(SKEncodedImageFormat.Jpeg, HiFiSettings.GetJpegQuality());
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
            using var paint = new SKPaint
            {
                IsAntialias = true,
                Color = SKColor.Parse("#1F2937")
            };

            var font = new SKFont(_arabicTypeface ?? SKTypeface.Default, 16f) { Embolden = true };

            if (_arabicShaper != null && ContainsArabic(title))
            {
                var result = _arabicShaper.Shape(title, font);
                if (result?.Points != null && result.Points.Length > 0)
                {
                    var textWidth = result.Points.LastOrDefault().X;
                    canvas.DrawShapedText(_arabicShaper, title, x - textWidth / 2f, y, font, paint);
                    return;
                }
            }

            canvas.DrawText(title, x, y, SKTextAlign.Center, font, paint);
        }

        private static void DrawLegend(SKCanvas canvas, float x, float y)
        {
            var legends = new[]
            {
                (Color: "#E53935", Label: "ضعيف <40"),
                (Color: "#FF8C00", Label: "متوسط 40-54"),
                (Color: "#14A44D", Label: "جيد ≥55")
            };

            var boxSize = 10f;
            var spacing = 8f;
            var font = new SKFont(_arabicTypeface ?? SKTypeface.Default, 10f);

            using var boxPaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill };
            using var textPaint = new SKPaint { IsAntialias = true, Color = SKColor.Parse("#6B7280") };

            foreach (var (Color, Label) in legends.Reverse())
            {
                // رسم المربع
                boxPaint.Color = SKColor.Parse(Color);
                canvas.DrawRect(x, y, boxSize, boxSize, boxPaint);

                // رسم النص
                if (_arabicShaper != null && ContainsArabic(Label))
                {
                    var result = _arabicShaper.Shape(Label, font);
                    if (result?.Points != null && result.Points.Length > 0)
                    {
                        var textWidth = result.Points.LastOrDefault().X;
                        canvas.DrawShapedText(_arabicShaper, Label, x - textWidth - spacing, y + boxSize - 2, font, textPaint);
                        x -= textWidth + boxSize + spacing * 3;
                        continue;
                    }
                }

                canvas.DrawText(Label, x + boxSize + spacing, y + boxSize - 2, SKTextAlign.Left, font, textPaint);
                x += 80f;
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
            var font = new SKFont(_arabicTypeface ?? SKTypeface.Default, 12f); // 11-12pt labels
            var valueFont = new SKFont(SKTypeface.Default, 11f) { Embolden = true }; // 10-11pt values

            using var labelPaint = new SKPaint { IsAntialias = true, Color = SKColor.Parse("#1F2937") };
            using var valuePaint = new SKPaint { IsAntialias = true, Color = SKColor.Parse("#FFFFFF") };
            using var barPaint = new SKPaint { IsAntialias = true };

            for (int i = 0; i < dimensions.Count; i++)
            {
                var dim = dimensions[i];
                var y = topMargin + (i * (barHeight + barSpacing)) + barSpacing;

                // رسم تسمية البُعد (RTL على اليمين)
                DrawDimensionLabel(canvas, dim.Dimension, leftMargin - 10, y + barHeight / 2f + 4, font, labelPaint);

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

                // رسم القيمة T في نهاية الشريط
                var tValue = $"T={dim.T.ToString("F1", CultureInfo.InvariantCulture)}";
                var textX = leftMargin + barWidth + 5;
                var textY = y + barHeight / 2f + 4;

                // خلفية بيضاء خفيفة للقيمة
                using var bgPaint = new SKPaint
                {
                    IsAntialias = true,
                    Color = SKColors.White.WithAlpha(230)
                };
                
                var textBounds = valueFont.MeasureText(tValue);
                var bgRect = SKRect.Create(textX - 3, textY - 12, textBounds + 6, 14);
                canvas.DrawRect(bgRect, bgPaint);

                // النص
                using var tValuePaint = new SKPaint { IsAntialias = true, Color = SKColor.Parse("#1F2937") };
                canvas.DrawText(tValue, textX, textY, SKTextAlign.Left, valueFont, tValuePaint);
            }
        }

        private static void DrawDimensionLabel(
            SKCanvas canvas,
            string dimensionName,
            float x,
            float y,
            SKFont font,
            SKPaint paint)
        {
            // Increased from 20 to 26 chars to prevent truncating long Arabic dimension names
            var formatted = FormatDimensionName(dimensionName, 26);

            if (_arabicShaper != null && ContainsArabic(formatted))
            {
                var result = _arabicShaper.Shape(formatted, font);
                if (result?.Points != null && result.Points.Length > 0)
                {
                    var textWidth = result.Points.LastOrDefault().X;
                    canvas.DrawShapedText(_arabicShaper, formatted, x - textWidth, y, font, paint);
                    return;
                }
            }

            canvas.DrawText(formatted, x, y, SKTextAlign.Right, font, paint);
        }

        private static SKColor GetBandColor(double tScore)
        {
            if (tScore < 40) return SKColor.Parse("#E53935"); // Red
            if (tScore < 55) return SKColor.Parse("#FF8C00"); // Orange
            return SKColor.Parse("#14A44D"); // Green
        }

        private static string FormatDimensionName(string name, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(name)) return "غير محدد";
            // Increased max length from 20 to 26 to accommodate longer Arabic sub-dimension names
            if (name.Length <= maxLength) return name;
            return name[..(maxLength - 1)] + "…";
        }

        private static bool ContainsArabic(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            return text.Any(c => c >= 0x0600 && c <= 0x06FF);
        }
    }
}
