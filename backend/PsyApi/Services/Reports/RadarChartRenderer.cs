using SkiaSharp;
using SkiaSharp.HarfBuzz;
using PsyApi.Models;

namespace PsyApi.Services.Reports
{
    /// <summary>
    /// مخطط رادار Vector لعرض الأبعاد بشكل دائري (Spider/Radar Chart)
    /// 100% Vector مع دعم RTL للنصوص العربية
    /// </summary>
    public static class RadarChartRenderer
    {
        private static SKTypeface? _arabicTypeface;
        private static SKShaper? _arabicShaper;
        private static readonly object _lock = new();

        /// <summary>
        /// رسم مخطط رادار كامل لعدة أبعاد (4-12 بُعد مثالي)
        /// </summary>
        /// <param name="dimensions">قائمة الأبعاد مع T-scores</param>
        /// <param name="size">حجم المخطط (400-600px مثالي)</param>
        /// <param name="showGrid">عرض شبكة الخلفية</param>
        /// <returns>صورة PNG كـ byte array</returns>
        public static byte[] RenderRadarChart(
            IEnumerable<DimensionScore> dimensions,
            int size = 500,
            bool showGrid = true)
        {
            EnsureArabicFont();

            var dimensionsList = dimensions.Take(12).ToList(); // حد أقصى 12 بُعد
            if (dimensionsList.Count < 3)
                throw new ArgumentException("Radar chart needs at least 3 dimensions");

            // High DPI rendering
            var scaleFactor = 2f;
            var actualSize = (int)(size * scaleFactor);

            using var surface = SKSurface.Create(new SKImageInfo(actualSize, actualSize));
            var canvas = surface.Canvas;
            canvas.Scale(scaleFactor);
            canvas.Clear(SKColors.Transparent);

            var centerX = size / 2f;
            var centerY = size / 2f;
            var maxRadius = (size * 0.35f); // 35% من الحجم للرسم

            // رسم الشبكة والمحاور
            if (showGrid)
                DrawRadarGrid(canvas, centerX, centerY, maxRadius, dimensionsList.Count);

            // رسم المحاور والتسميات
            DrawRadarAxes(canvas, centerX, centerY, maxRadius, dimensionsList);

            // رسم البيانات (المضلع)
            DrawRadarPolygon(canvas, centerX, centerY, maxRadius, dimensionsList);

            // تحويل إلى PNG
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
        /// رسم شبكة الخلفية (دوائر متحدة المركز)
        /// </summary>
        private static void DrawRadarGrid(
            SKCanvas canvas,
            float centerX,
            float centerY,
            float maxRadius,
            int axisCount)
        {
            using var gridPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                Color = ReportTheme.FromHex(ReportTheme.Colors.Border),
                StrokeWidth = 1f
            };

            // رسم 5 دوائر (20%, 40%, 60%, 80%, 100%)
            for (int i = 1; i <= 5; i++)
            {
                var radius = maxRadius * (i / 5f);
                canvas.DrawCircle(centerX, centerY, radius, gridPaint);
            }

            // رسم خطوط المحاور
            var angleStep = 360f / axisCount;
            for (int i = 0; i < axisCount; i++)
            {
                var angle = (i * angleStep - 90) * (float)Math.PI / 180f; // Start at top
                var endX = centerX + maxRadius * (float)Math.Cos(angle);
                var endY = centerY + maxRadius * (float)Math.Sin(angle);
                
                canvas.DrawLine(centerX, centerY, endX, endY, gridPaint);
            }
        }

        /// <summary>
        /// رسم المحاور مع التسميات العربية
        /// </summary>
        private static void DrawRadarAxes(
            SKCanvas canvas,
            float centerX,
            float centerY,
            float maxRadius,
            List<DimensionScore> dimensions)
        {
            var angleStep = 360f / dimensions.Count;
            var labelDistance = maxRadius + 40; // مسافة التسميات من المركز

            using var textPaint = new SKPaint
            {
                IsAntialias = true,
                Color = ReportTheme.FromHex(ReportTheme.Colors.Text)
            };

            var fontSize = 11f;
            var font = new SKFont(_arabicTypeface ?? SKTypeface.Default, fontSize);

            for (int i = 0; i < dimensions.Count; i++)
            {
                var angle = (i * angleStep - 90) * (float)Math.PI / 180f;
                var labelX = centerX + labelDistance * (float)Math.Cos(angle);
                var labelY = centerY + labelDistance * (float)Math.Sin(angle);

                // تقصير اسم البُعد
                var dimensionName = ArabicTextRenderer.FormatDimensionName(dimensions[i].Dimension, 15);

                // رسم النص مع دعم العربية
                DrawRotatedLabel(canvas, dimensionName, labelX, labelY, angle, font, textPaint);
            }
        }

        /// <summary>
        /// رسم المضلع (البيانات الفعلية)
        /// </summary>
        private static void DrawRadarPolygon(
            SKCanvas canvas,
            float centerX,
            float centerY,
            float maxRadius,
            List<DimensionScore> dimensions)
        {
            var angleStep = 360f / dimensions.Count;
            var path = new SKPath();

            // حساب النقاط
            var points = new List<SKPoint>();
            for (int i = 0; i < dimensions.Count; i++)
            {
                var t = dimensions[i].T;
                // Normalize T-score [20-80] to [0-1]
                var normalized = Math.Clamp((t - 20) / 60.0, 0, 1);
                var radius = maxRadius * (float)normalized;

                var angle = (i * angleStep - 90) * (float)Math.PI / 180f;
                var x = centerX + radius * (float)Math.Cos(angle);
                var y = centerY + radius * (float)Math.Sin(angle);

                points.Add(new SKPoint(x, y));

                if (i == 0)
                    path.MoveTo(x, y);
                else
                    path.LineTo(x, y);
            }
            path.Close();

            // تعبئة المضلع (شفافية)
            using var fillPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Color = ReportTheme.FromHex(ReportTheme.Colors.Primary).WithAlpha(60)
            };
            canvas.DrawPath(path, fillPaint);

            // حدود المضلع
            using var strokePaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                Color = ReportTheme.FromHex(ReportTheme.Colors.Primary),
                StrokeWidth = 3f,
                StrokeJoin = SKStrokeJoin.Round
            };
            canvas.DrawPath(path, strokePaint);

            // نقاط البيانات
            using var pointPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Color = ReportTheme.FromHex(ReportTheme.Colors.Primary)
            };

            foreach (var point in points)
            {
                canvas.DrawCircle(point.X, point.Y, 5f, pointPaint);
                
                // حدود بيضاء للنقاط
                using var whiteBorder = new SKPaint
                {
                    IsAntialias = true,
                    Style = SKPaintStyle.Stroke,
                    Color = SKColors.White,
                    StrokeWidth = 2f
                };
                canvas.DrawCircle(point.X, point.Y, 5f, whiteBorder);
            }
        }

        /// <summary>
        /// رسم تسمية مع دوران تلقائي
        /// </summary>
        private static void DrawRotatedLabel(
            SKCanvas canvas,
            string text,
            float x,
            float y,
            float angleRad,
            SKFont font,
            SKPaint paint)
        {
            canvas.Save();
            
            // لا نحتاج دوران للنصوص العربية - نبقيها أفقية دائماً
            // نضبط المحاذاة حسب الموقع فقط
            var angleDeg = angleRad * 180 / (float)Math.PI + 90;
            
            SKTextAlign align = SKTextAlign.Center;
            if (angleDeg > 90 && angleDeg < 270)
                align = SKTextAlign.Right;
            else if (angleDeg >= 270 || angleDeg <= 90)
                align = SKTextAlign.Left;

            // رسم النص بدون دوران
            if (_arabicShaper != null && ArabicTextRenderer.ContainsArabic(text))
            {
                var result = _arabicShaper.Shape(text, font);
                if (result?.Points != null && result.Points.Length > 0)
                {
                    var textWidth = result.Points.LastOrDefault().X;
                    var adjustedX = align switch
                    {
                        SKTextAlign.Left => x,
                        SKTextAlign.Right => x - textWidth,
                        _ => x - textWidth / 2
                    };
                    canvas.DrawShapedText(_arabicShaper, text, adjustedX, y, font, paint);
                }
                else
                {
                    canvas.DrawText(text, x, y, align, font, paint);
                }
            }
            else
            {
                canvas.DrawText(text, x, y, align, font, paint);
            }

            canvas.Restore();
        }

        /// <summary>
        /// رسم رادار مبسط (Mini Radar) للملخصات
        /// </summary>
        public static byte[] RenderMiniRadar(
            IEnumerable<DimensionScore> dimensions,
            int size = 200)
        {
            return RenderRadarChart(dimensions, size, showGrid: false);
        }

        /// <summary>
        /// رسم رادار لمحور معين (Cluster Radar)
        /// </summary>
        public static byte[] RenderClusterRadar(
            string clusterCode,
            IEnumerable<DimensionScore> allDimensions,
            int size = 400)
        {
            var clusterDimensions = allDimensions
                .Where(d => ReportAnalytics.ClusterMap.GetValueOrDefault(d.Dimension, "OTHER") == clusterCode)
                .ToList();

            if (clusterDimensions.Count < 3)
                throw new ArgumentException($"Cluster {clusterCode} has insufficient dimensions for radar chart");

            return RenderRadarChart(clusterDimensions, size, showGrid: true);
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
                        Console.WriteLine("[RadarChartRenderer] ✓ Arabic font loaded successfully");
                    }
                    else
                    {
                        Console.WriteLine($"[RadarChartRenderer] ⚠ Font not found: {regularPath}");
                        _arabicTypeface = SKTypeface.Default;
                        _arabicShaper = new SKShaper(_arabicTypeface);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[RadarChartRenderer] ❌ Font loading error: {ex.Message}");
                    _arabicTypeface = SKTypeface.Default;
                    _arabicShaper = new SKShaper(_arabicTypeface);
                }
            }
        }
    }
}
