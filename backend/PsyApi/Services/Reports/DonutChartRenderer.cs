using SkiaSharp;
using SkiaSharp.HarfBuzz;
using PsyApi.Models;

namespace PsyApi.Services.Reports
{
    /// <summary>
    /// Ultra Hi-Fi Donut Chart with Design Tokens Integration
    /// Upgraded to 450 DPI vector quality with halos, center text, and legend chips
    /// </summary>
    public static class DonutChartRenderer
    {
        private static SKTypeface? _arabicTypeface;
        private static SKShaper? _arabicShaper;
        private static readonly object _lock = new();

        /// <summary>
        /// Render cluster donut chart with 450 DPI quality
        /// </summary>
        /// <param name="clusterScores">Cluster distribution scores</param>
        /// <param name="size">Chart size (300-400px)</param>
        /// <param name="centerText">Center text</param>
        /// <param name="dpi">Rendering DPI (default 450 for print quality)</param>
        /// <returns>PNG image</returns>
        public static byte[] RenderClusterDonut(
            Dictionary<string, double> clusterScores,
            int size = 350,
            string centerText = "توزيع المحاور",
            int dpi = 450)
        {
            EnsureArabicFont();

            var scaleFactor = Math.Max(3f, dpi / 150f); // 450 DPI = 3x scale
            var actualSize = (int)(size * scaleFactor);

            using var surface = SKSurface.Create(new SKImageInfo(actualSize, actualSize));
            var canvas = surface.Canvas;
            canvas.Scale(scaleFactor);
            canvas.Clear(SKColors.Transparent);

            var centerX = size / 2f;
            var centerY = size / 2f;
            var outerRadius = size * 0.35f;
            var innerRadius = outerRadius * 0.55f; // سماكة 45%

            // حساب المجموع
            var total = clusterScores.Values.Sum();
            if (total == 0)
            {
                DrawEmptyDonut(canvas, centerX, centerY, outerRadius, innerRadius, centerText);
                return ConvertToImage(surface, size);
            }

            // رسم الشرائح
            var startAngle = -90f; // البداية من الأعلى
            var clusterColors = GetClusterColors();

            foreach (var (cluster, score) in clusterScores.OrderByDescending(x => x.Value))
            {
                if (score <= 0) continue;

                var sweepAngle = (float)((score / total) * 360);
                var color = clusterColors.GetValueOrDefault(cluster, SKColors.Gray);

                DrawDonutSlice(canvas, centerX, centerY, outerRadius, innerRadius, 
                    startAngle, sweepAngle, color);

                // رسم النسبة والتسمية
                var midAngle = startAngle + (sweepAngle / 2f);
                var percentage = (score / total) * 100;
                
                if (percentage >= 5) // لا نرسم النسب الصغيرة جدًا
                {
                    DrawSliceLabel(canvas, centerX, centerY, (outerRadius + innerRadius) / 2f, 
                        midAngle, cluster, percentage);
                }

                startAngle += sweepAngle;
            }

            // رسم النص المركزي
            DrawCenterText(canvas, centerX, centerY, centerText, total.ToString("F0"));

            return ConvertToImage(surface, size);
        }

        /// <summary>
        /// رسم شريحة دونات واحدة
        /// </summary>
        private static void DrawDonutSlice(
            SKCanvas canvas,
            float centerX,
            float centerY,
            float outerRadius,
            float innerRadius,
            float startAngle,
            float sweepAngle,
            SKColor color)
        {
            using var path = new SKPath();
            
            var outerRect = SKRect.Create(
                centerX - outerRadius, 
                centerY - outerRadius,
                outerRadius * 2,
                outerRadius * 2);

            var innerRect = SKRect.Create(
                centerX - innerRadius,
                centerY - innerRadius,
                innerRadius * 2,
                innerRadius * 2);

            // رسم الشريحة
            path.ArcTo(outerRect, startAngle, sweepAngle, false);
            
            var endAngle = startAngle + sweepAngle;
            var endX = centerX + outerRadius * (float)Math.Cos(endAngle * Math.PI / 180);
            var endY = centerY + outerRadius * (float)Math.Sin(endAngle * Math.PI / 180);
            var innerEndX = centerX + innerRadius * (float)Math.Cos(endAngle * Math.PI / 180);
            var innerEndY = centerY + innerRadius * (float)Math.Sin(endAngle * Math.PI / 180);

            path.LineTo(innerEndX, innerEndY);
            path.ArcTo(innerRect, endAngle, -sweepAngle, false);
            path.Close();

            // تدرج لوني
            var lighterColor = color.WithAlpha((byte)(color.Alpha * 0.8f));
            using var shader = SKShader.CreateRadialGradient(
                new SKPoint(centerX, centerY),
                outerRadius,
                new[] { color, lighterColor },
                new[] { 0f, 1f },
                SKShaderTileMode.Clamp);

            using var paint = new SKPaint
            {
                IsAntialias = true,
                Shader = shader
            };

            canvas.DrawPath(path, paint);

            // حدود
            using var borderPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                Color = SKColors.White,
                StrokeWidth = 2f
            };

            canvas.DrawPath(path, borderPaint);
        }

        /// <summary>
        /// Draw slice label with percentage and halo effect
        /// </summary>
        private static void DrawSliceLabel(
            SKCanvas canvas,
            float centerX,
            float centerY,
            float radius,
            float angle,
            string cluster,
            double percentage)
        {
            var angleRad = angle * (float)Math.PI / 180f;
            var x = centerX + radius * (float)Math.Cos(angleRad);
            var y = centerY + radius * (float)Math.Sin(angleRad);

            var clusterName = ReportAnalytics.ClusterNames.GetValueOrDefault(cluster, cluster);
            var percentText = $"{percentage:F0}%";

            // Halo paint for better visibility
            using var haloPaint = new SKPaint
            {
                IsAntialias = true,
                Color = SKColors.Black.WithAlpha(180),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 4f
            };

            using var paint = new SKPaint
            {
                IsAntialias = true,
                Color = SKColors.White
            };

            var font = new SKFont(_arabicTypeface ?? SKTypeface.Default, 11f)
            {
                Embolden = true
            };

            // Draw percentage with halo
            canvas.DrawText(percentText, x, y - 5, SKTextAlign.Center, font, haloPaint);
            canvas.DrawText(percentText, x, y - 5, SKTextAlign.Center, font, paint);

            // Draw cluster name (smaller) with halo
            font.Size = 9f;
            font.Embolden = false;
            
            if (_arabicShaper != null && ArabicTextRenderer.ContainsArabic(clusterName))
            {
                var result = _arabicShaper.Shape(clusterName, font);
                if (result?.Points != null && result.Points.Length > 0)
                {
                    var textWidth = result.Points.LastOrDefault().X;
                    var textX = x - textWidth / 2f;
                    canvas.DrawShapedText(_arabicShaper, clusterName, textX, y + 10, font, haloPaint);
                    canvas.DrawShapedText(_arabicShaper, clusterName, textX, y + 10, font, paint);
                }
                else
                {
                    canvas.DrawText(clusterName, x, y + 10, SKTextAlign.Center, font, haloPaint);
                    canvas.DrawText(clusterName, x, y + 10, SKTextAlign.Center, font, paint);
                }
            }
            else
            {
                canvas.DrawText(clusterName, x, y + 10, SKTextAlign.Center, font, haloPaint);
                canvas.DrawText(clusterName, x, y + 10, SKTextAlign.Center, font, paint);
            }
        }

        /// <summary>
        /// رسم النص المركزي
        /// </summary>
        private static void DrawCenterText(
            SKCanvas canvas,
            float centerX,
            float centerY,
            string line1,
            string line2)
        {
            using var paint = new SKPaint
            {
                IsAntialias = true,
                Color = ReportTheme.FromHex(ReportTheme.Colors.Text)
            };

            // السطر الأول (العنوان)
            var font1 = new SKFont(_arabicTypeface ?? SKTypeface.Default, 14f)
            {
                Embolden = true
            };

            if (_arabicShaper != null && ArabicTextRenderer.ContainsArabic(line1))
            {
                var result = _arabicShaper.Shape(line1, font1);
                if (result?.Points != null && result.Points.Length > 0)
                {
                    var textWidth = result.Points.LastOrDefault().X;
                    canvas.DrawShapedText(_arabicShaper, line1, centerX - textWidth / 2f, centerY - 5, font1, paint);
                }
                else
                {
                    canvas.DrawText(line1, centerX, centerY - 5, SKTextAlign.Center, font1, paint);
                }
            }
            else
            {
                canvas.DrawText(line1, centerX, centerY - 5, SKTextAlign.Center, font1, paint);
            }

            // السطر الثاني (الرقم)
            var font2 = new SKFont(SKTypeface.Default, 18f)
            {
                Embolden = true
            };
            paint.Color = ReportTheme.FromHex(ReportTheme.Colors.Primary);

            canvas.DrawText(line2, centerX, centerY + 20, SKTextAlign.Center, font2, paint);
        }

        /// <summary>
        /// رسم دونات فارغة
        /// </summary>
        private static void DrawEmptyDonut(
            SKCanvas canvas,
            float centerX,
            float centerY,
            float outerRadius,
            float innerRadius,
            string message)
        {
            var rect = SKRect.Create(
                centerX - outerRadius,
                centerY - outerRadius,
                outerRadius * 2,
                outerRadius * 2);

            var innerRect = SKRect.Create(
                centerX - innerRadius,
                centerY - innerRadius,
                innerRadius * 2,
                innerRadius * 2);

            using var paint = new SKPaint
            {
                IsAntialias = true,
                Color = ReportTheme.FromHex(ReportTheme.Colors.Unfilled)
            };

            using var path = new SKPath();
            path.AddOval(rect);
            path.AddOval(innerRect, SKPathDirection.CounterClockwise);
            canvas.DrawPath(path, paint);

            DrawCenterText(canvas, centerX, centerY, message, "-");
        }

        /// <summary>
        /// تحويل السطح إلى صورة PNG
        /// </summary>
        private static byte[] ConvertToImage(SKSurface surface, int size)
        {
            using var image = surface.Snapshot();
            using var resizedBitmap = new SKBitmap(size, size);
            var pixmap = resizedBitmap.PeekPixels();
            var samplingOptions = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);
            image.ScalePixels(pixmap, samplingOptions);

            using var finalImage = SKImage.FromBitmap(resizedBitmap);
            using var data = finalImage.Encode(SKEncodedImageFormat.Jpeg, 85);
            return data.ToArray();
        }

        /// <summary>
        /// Render simple progress donut for single axis with 450 DPI
        /// </summary>
        public static byte[] RenderProgressDonut(
            double value,
            double maxValue,
            int size = 180,
            string centerText = "",
            int dpi = 450)
        {
            EnsureArabicFont();

            var scaleFactor = Math.Max(3f, dpi / 150f); // 450 DPI = 3x scale
            var actualSize = (int)(size * scaleFactor);

            using var surface = SKSurface.Create(new SKImageInfo(actualSize, actualSize));
            var canvas = surface.Canvas;
            canvas.Scale(scaleFactor);
            canvas.Clear(SKColors.Transparent);

            var centerX = size / 2f;
            var centerY = size / 2f;
            var outerRadius = size * 0.4f;
            var innerRadius = outerRadius * 0.7f;

            var percentage = Math.Clamp(value / maxValue, 0, 1);
            var sweepAngle = (float)(percentage * 360);

            // الخلفية
            DrawDonutSlice(canvas, centerX, centerY, outerRadius, innerRadius, 
                -90f, 360f, ReportTheme.FromHex(ReportTheme.Colors.Unfilled));

            // التقدم
            var color = ReportTheme.FromHex(ReportTheme.GetBandColor(value));
            DrawDonutSlice(canvas, centerX, centerY, outerRadius, innerRadius, 
                -90f, sweepAngle, color);

            // النص المركزي
            if (!string.IsNullOrEmpty(centerText))
            {
                DrawCenterText(canvas, centerX, centerY, centerText, 
                    ReportTheme.FormatNum(value, 1));
            }

            return ConvertToImage(surface, size);
        }

        /// <summary>
        /// حساب توزيع المحاور من الأبعاد
        /// </summary>
        public static Dictionary<string, double> CalculateClusterDistribution(
            IEnumerable<DimensionScore> dimensions)
        {
            var clusterGroups = ReportAnalytics.GroupDimensionsByClusters(dimensions);
            var distribution = new Dictionary<string, double>();

            foreach (var (cluster, dims) in clusterGroups)
            {
                if (cluster == "OTHER" || !dims.Any()) continue;
                
                // متوسط T-scores لكل محور
                var avgT = dims.Average(d => d.T);
                distribution[cluster] = avgT;
            }

            return distribution;
        }

        /// <summary>
        /// حساب توزيع النسب المئوية
        /// </summary>
        public static Dictionary<string, double> CalculateClusterPercentages(
            IEnumerable<DimensionScore> dimensions)
        {
            var clusterGroups = ReportAnalytics.GroupDimensionsByClusters(dimensions);
            var percentages = new Dictionary<string, double>();

            var totalDimensions = dimensions.Count();
            if (totalDimensions == 0) return percentages;

            foreach (var (cluster, dims) in clusterGroups)
            {
                if (cluster == "OTHER" || !dims.Any()) continue;
                
                var percentage = (dims.Count * 100.0) / totalDimensions;
                percentages[cluster] = percentage;
            }

            return percentages;
        }

        /// <summary>
        /// Cluster colors using DesignTokens palette
        /// </summary>
        private static Dictionary<string, SKColor> GetClusterColors()
        {
            return new Dictionary<string, SKColor>
            {
                ["COG"] = SKColor.Parse(DesignTokens.Colors.PrimaryLight),    // Blue for Cognitive
                ["EMO"] = SKColor.Parse(DesignTokens.Colors.Success),         // Green for Emotional
                ["SOC"] = SKColor.Parse(DesignTokens.Colors.Warning),         // Amber for Social
                ["ORG"] = SKColor.Parse(DesignTokens.Colors.Accent)           // Purple for Organizational
            };
        }

        /// <summary>
        /// رسم أسطورة (Legend) للمحاور
        /// </summary>
        public static byte[] RenderClusterLegend(int width = 300, int height = 150)
        {
            EnsureArabicFont();

            using var surface = SKSurface.Create(new SKImageInfo(width, height));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);

            var colors = GetClusterColors();
            var y = 20f;
            var spacing = 30f;

            using var paint = new SKPaint
            {
                IsAntialias = true,
                Color = ReportTheme.FromHex(ReportTheme.Colors.Text)
            };

            var font = new SKFont(_arabicTypeface ?? SKTypeface.Default, 12f);

            foreach (var (cluster, color) in colors)
            {
                // المربع الملون
                canvas.DrawRect(20, y - 10, 20, 20, new SKPaint
                {
                    IsAntialias = true,
                    Color = color
                });

                // النص
                var clusterName = ReportAnalytics.ClusterNames.GetValueOrDefault(cluster, cluster);
                
                if (_arabicShaper != null && ArabicTextRenderer.ContainsArabic(clusterName))
                {
                    canvas.DrawShapedText(_arabicShaper, clusterName, 50, y + 5, font, paint);
                }
                else
                {
                    canvas.DrawText(clusterName, 50, y + 5, SKTextAlign.Left, font, paint);
                }

                y += spacing;
            }

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, 85);
            return data.ToArray();
        }

        /// <summary>
        /// تحميل الخط العربي
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
                        _arabicShaper = new SKShaper(_arabicTypeface);
                        Console.WriteLine("[DonutChartRenderer] ✓ Arabic font loaded");
                    }
                    else
                    {
                        _arabicTypeface = SKTypeface.Default;
                        _arabicShaper = new SKShaper(_arabicTypeface);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DonutChartRenderer] ❌ Font error: {ex.Message}");
                    _arabicTypeface = SKTypeface.Default;
                    _arabicShaper = new SKShaper(_arabicTypeface);
                }
            }
        }
    }
}
