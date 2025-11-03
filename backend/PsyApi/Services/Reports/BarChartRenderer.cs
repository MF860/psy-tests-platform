using SkiaSharp;
using SkiaSharp.HarfBuzz;
using PsyApi.Models;

namespace PsyApi.Services.Reports
{
    /// <summary>
    /// مخطط الأعمدة Vector مع أشرطة مرجعية عند 40/55/65
    /// رسم احترافي مع تدرجات لونية وتسميات عربية RTL
    /// </summary>
    public static class BarChartRenderer
    {
        private static SKTypeface? _arabicTypeface;
        private static SKShaper? _arabicShaper;
        private static readonly object _lock = new();

        /// <summary>
        /// رسم مخطط أعمدة أفقي لعدة أبعاد (مرتب تصاعديًا حسب T-score)
        /// </summary>
        /// <param name="dimensions">قائمة الأبعاد</param>
        /// <param name="width">عرض المخطط (600-800px مثالي)</param>
        /// <param name="height">ارتفاع المخطط (يعتمد على عدد الأبعاد: 40-50px لكل بُعد)</param>
        /// <param name="showBandLines">عرض خطوط مرجعية عند 40/55/65</param>
        /// <returns>صورة PNG</returns>
        public static byte[] RenderHorizontalBarChart(
            IEnumerable<DimensionScore> dimensions,
            int width = 700,
            int height = 0,
            bool showBandLines = true)
        {
            EnsureArabicFont();

            var sortedDimensions = dimensions.OrderBy(d => d.T).ToList();
            
            // حساب الارتفاع التلقائي
            if (height == 0)
                height = Math.Max(400, sortedDimensions.Count * 45 + 100);

            var scaleFactor = 2f;
            var actualWidth = (int)(width * scaleFactor);
            var actualHeight = (int)(height * scaleFactor);

            using var surface = SKSurface.Create(new SKImageInfo(actualWidth, actualHeight));
            var canvas = surface.Canvas;
            canvas.Scale(scaleFactor);
            canvas.Clear(SKColors.White);

            // مساحات
            var leftMargin = 180f; // مساحة للتسميات العربية (RTL)
            var rightMargin = 50f;
            var topMargin = 40f;
            var bottomMargin = 40f;
            var chartWidth = width - leftMargin - rightMargin;
            var chartHeight = height - topMargin - bottomMargin;

            // رسم العنوان
            DrawTitle(canvas, "توزيع الأبعاد حسب T-Score", width / 2f, 20f);

            // رسم الأشرطة المرجعية
            if (showBandLines)
                DrawReferenceLinesHorizontal(canvas, leftMargin, rightMargin, topMargin, chartWidth, chartHeight);

            // رسم محور X (T-scores: 20-80)
            DrawXAxis(canvas, leftMargin, topMargin + chartHeight, chartWidth);

            // رسم الأعمدة
            DrawBars(canvas, sortedDimensions, leftMargin, topMargin, chartWidth, chartHeight);

            // تحويل إلى PNG
            using var image = surface.Snapshot();
            using var resizedBitmap = new SKBitmap(width, height);
            var pixmap = resizedBitmap.PeekPixels();
            var samplingOptions = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);
            image.ScalePixels(pixmap, samplingOptions);

            using var finalImage = SKImage.FromBitmap(resizedBitmap);
            using var data = finalImage.Encode(SKEncodedImageFormat.Jpeg, 85);
            return data.ToArray();
        }

        /// <summary>
        /// رسم العنوان
        /// </summary>
        private static void DrawTitle(SKCanvas canvas, string title, float x, float y)
        {
            using var paint = new SKPaint
            {
                IsAntialias = true,
                Color = ReportTheme.FromHex(ReportTheme.Colors.Text)
            };

            var font = new SKFont(_arabicTypeface ?? SKTypeface.Default, 16f)
            {
                Embolden = true
            };

            if (_arabicShaper != null && ArabicTextRenderer.ContainsArabic(title))
            {
                var result = _arabicShaper.Shape(title, font);
                if (result?.Points != null && result.Points.Length > 0)
                {
                    var textWidth = result.Points.LastOrDefault().X;
                    canvas.DrawShapedText(_arabicShaper, title, x - textWidth / 2f, y, font, paint);
                }
                else
                {
                    canvas.DrawText(title, x, y, SKTextAlign.Center, font, paint);
                }
            }
            else
            {
                canvas.DrawText(title, x, y, SKTextAlign.Center, font, paint);
            }
        }

        /// <summary>
        /// رسم الأشرطة المرجعية (40, 55, 65)
        /// </summary>
        private static void DrawReferenceLinesHorizontal(
            SKCanvas canvas,
            float leftMargin,
            float rightMargin,
            float topMargin,
            float chartWidth,
            float chartHeight)
        {
            var bandValues = new[] { 40.0, 55.0, 65.0 };
            var bandLabels = new[] { "40", "55", "65" };
            var bandColors = new[]
            {
                ReportTheme.FromHex(ReportTheme.Colors.Weak),
                ReportTheme.FromHex(ReportTheme.Colors.Average),
                ReportTheme.FromHex(ReportTheme.Colors.Excellent)
            };

            using var linePaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1.5f,
                PathEffect = SKPathEffect.CreateDash(new[] { 8f, 4f }, 0)
            };

            using var textPaint = new SKPaint
            {
                IsAntialias = true,
                Color = ReportTheme.FromHex(ReportTheme.Colors.TextSecondary)
            };

            var font = new SKFont(SKTypeface.Default, 9f);

            for (int i = 0; i < bandValues.Length; i++)
            {
                // حساب الموقع (T-score من 20-80)
                var normalized = (bandValues[i] - 20) / 60.0;
                var x = leftMargin + (float)(chartWidth * normalized);

                linePaint.Color = bandColors[i].WithAlpha(100);
                canvas.DrawLine(x, topMargin, x, topMargin + chartHeight, linePaint);

                // رسم التسمية
                canvas.DrawText(bandLabels[i], x, topMargin - 8, SKTextAlign.Center, font, textPaint);
            }
        }

        /// <summary>
        /// رسم محور X (T-scores)
        /// </summary>
        private static void DrawXAxis(
            SKCanvas canvas,
            float leftMargin,
            float y,
            float chartWidth)
        {
            using var axisPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                Color = ReportTheme.FromHex(ReportTheme.Colors.Border),
                StrokeWidth = 2f
            };

            canvas.DrawLine(leftMargin, y, leftMargin + chartWidth, y, axisPaint);

            using var textPaint = new SKPaint
            {
                IsAntialias = true,
                Color = ReportTheme.FromHex(ReportTheme.Colors.TextSecondary)
            };

            var font = new SKFont(SKTypeface.Default, 10f);

            // تسميات المحور (20, 30, 40, ... 80)
            for (int t = 20; t <= 80; t += 10)
            {
                var normalized = (t - 20) / 60.0;
                var x = leftMargin + (float)(chartWidth * normalized);

                // علامة صغيرة
                canvas.DrawLine(x, y, x, y + 5, axisPaint);

                // النص
                canvas.DrawText(t.ToString(), x, y + 20, SKTextAlign.Center, font, textPaint);
            }
        }

        /// <summary>
        /// رسم الأعمدة (Bars)
        /// </summary>
        private static void DrawBars(
            SKCanvas canvas,
            List<DimensionScore> dimensions,
            float leftMargin,
            float topMargin,
            float chartWidth,
            float chartHeight)
        {
            var barHeight = chartHeight / dimensions.Count;
            var barPadding = barHeight * 0.25f; // 25% padding
            var actualBarHeight = barHeight - barPadding;

            for (int i = 0; i < dimensions.Count; i++)
            {
                var dim = dimensions[i];
                var y = topMargin + (i * barHeight) + (barPadding / 2f);

                // رسم اسم البُعد (على اليمين - RTL)
                DrawDimensionLabel(canvas, dim.Dimension, leftMargin - 10, y + actualBarHeight / 2f + 4);

                // رسم العمود
                var normalized = Math.Clamp((dim.T - 20) / 60.0, 0, 1);
                var barWidth = (float)(chartWidth * normalized);
                var barColor = ReportTheme.FromHex(ReportTheme.GetBandColor(dim.T));

                DrawGradientBar(canvas, leftMargin, y, barWidth, actualBarHeight, barColor);

                // رسم القيمة داخل/بجانب العمود
                DrawBarValue(canvas, dim.T, leftMargin + barWidth, y + actualBarHeight / 2f + 4, barWidth > 60);
            }
        }

        /// <summary>
        /// رسم تسمية البُعد (RTL)
        /// </summary>
        private static void DrawDimensionLabel(SKCanvas canvas, string dimensionName, float x, float y)
        {
            using var paint = new SKPaint
            {
                IsAntialias = true,
                Color = ReportTheme.FromHex(ReportTheme.Colors.Text)
            };

            var font = new SKFont(_arabicTypeface ?? SKTypeface.Default, 11f);
            var formatted = ArabicTextRenderer.FormatDimensionName(dimensionName, 22);

            if (_arabicShaper != null && ArabicTextRenderer.ContainsArabic(formatted))
            {
                var result = _arabicShaper.Shape(formatted, font);
                if (result?.Points != null && result.Points.Length > 0)
                {
                    var textWidth = result.Points.LastOrDefault().X;
                    canvas.DrawShapedText(_arabicShaper, formatted, x - textWidth, y, font, paint);
                }
                else
                {
                    canvas.DrawText(formatted, x, y, SKTextAlign.Right, font, paint);
                }
            }
            else
            {
                canvas.DrawText(formatted, x, y, SKTextAlign.Right, font, paint);
            }
        }

        /// <summary>
        /// رسم عمود مع تدرج لوني
        /// </summary>
        private static void DrawGradientBar(
            SKCanvas canvas,
            float x,
            float y,
            float width,
            float height,
            SKColor baseColor)
        {
            var rect = SKRect.Create(x, y, width, height);
            
            // تدرج لوني من اللون الأساسي إلى لون أفتح
            var lighterColor = baseColor.WithAlpha((byte)(baseColor.Alpha * 0.7f));
            var colors = new[] { baseColor, lighterColor };
            var positions = new float[] { 0f, 1f };

            using var shader = SKShader.CreateLinearGradient(
                new SKPoint(x, y),
                new SKPoint(x + width, y),
                colors,
                positions,
                SKShaderTileMode.Clamp);

            using var paint = new SKPaint
            {
                IsAntialias = true,
                Shader = shader
            };

            // رسم العمود مع حواف مستديرة
            var roundRect = new SKRoundRect(rect, 6f, 6f);
            canvas.DrawRoundRect(roundRect, paint);

            // حدود خفيفة
            using var borderPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                Color = baseColor.WithAlpha(150),
                StrokeWidth = 1.5f
            };
            canvas.DrawRoundRect(roundRect, borderPaint);
        }

        /// <summary>
        /// رسم قيمة T-score
        /// </summary>
        private static void DrawBarValue(
            SKCanvas canvas,
            double tScore,
            float x,
            float y,
            bool inside)
        {
            var valueText = ReportTheme.FormatNum(tScore, 1);
            
            using var paint = new SKPaint
            {
                IsAntialias = true,
                Color = inside 
                    ? SKColors.White 
                    : ReportTheme.FromHex(ReportTheme.Colors.Text)
            };

            var font = new SKFont(SKTypeface.Default, 10f)
            {
                Embolden = inside
            };

            var adjustedX = inside ? x - 8 : x + 8;
            var align = inside ? SKTextAlign.Right : SKTextAlign.Left;

            canvas.DrawText(valueText, adjustedX, y, align, font, paint);
        }

        /// <summary>
        /// رسم مخطط أعمدة رأسي (Vertical)
        /// </summary>
        public static byte[] RenderVerticalBarChart(
            IEnumerable<DimensionScore> dimensions,
            int width = 800,
            int height = 500,
            bool showBandLines = true)
        {
            EnsureArabicFont();

            var sortedDimensions = dimensions.OrderBy(d => d.T).Take(10).ToList(); // حد أقصى 10

            var scaleFactor = 2f;
            var actualWidth = (int)(width * scaleFactor);
            var actualHeight = (int)(height * scaleFactor);

            using var surface = SKSurface.Create(new SKImageInfo(actualWidth, actualHeight));
            var canvas = surface.Canvas;
            canvas.Scale(scaleFactor);
            canvas.Clear(SKColors.White);

            var leftMargin = 60f;
            var rightMargin = 40f;
            var topMargin = 50f;
            var bottomMargin = 100f;
            var chartWidth = width - leftMargin - rightMargin;
            var chartHeight = height - topMargin - bottomMargin;

            DrawTitle(canvas, "مقارنة الأبعاد", width / 2f, 25f);

            // رسم محور Y
            DrawYAxis(canvas, leftMargin, topMargin, chartHeight);

            // رسم الأشرطة المرجعية الأفقية
            if (showBandLines)
                DrawReferenceLinesVertical(canvas, leftMargin, topMargin, chartWidth, chartHeight);

            // رسم الأعمدة الرأسية
            DrawVerticalBars(canvas, sortedDimensions, leftMargin, topMargin, chartWidth, chartHeight, bottomMargin);

            using var image = surface.Snapshot();
            using var resizedBitmap = new SKBitmap(width, height);
            var pixmap = resizedBitmap.PeekPixels();
            var samplingOptions = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);
            image.ScalePixels(pixmap, samplingOptions);

            using var finalImage = SKImage.FromBitmap(resizedBitmap);
            using var data = finalImage.Encode(SKEncodedImageFormat.Jpeg, 85);
            return data.ToArray();
        }

        /// <summary>
        /// رسم محور Y (T-scores)
        /// </summary>
        private static void DrawYAxis(SKCanvas canvas, float x, float topMargin, float chartHeight)
        {
            using var axisPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                Color = ReportTheme.FromHex(ReportTheme.Colors.Border),
                StrokeWidth = 2f
            };

            canvas.DrawLine(x, topMargin, x, topMargin + chartHeight, axisPaint);

            using var textPaint = new SKPaint
            {
                IsAntialias = true,
                Color = ReportTheme.FromHex(ReportTheme.Colors.TextSecondary)
            };

            var font = new SKFont(SKTypeface.Default, 9f);

            for (int t = 20; t <= 80; t += 10)
            {
                var normalized = (t - 20) / 60.0;
                var y = topMargin + chartHeight - (float)(chartHeight * normalized);

                canvas.DrawLine(x - 5, y, x, y, axisPaint);
                canvas.DrawText(t.ToString(), x - 10, y + 3, SKTextAlign.Right, font, textPaint);
            }
        }

        /// <summary>
        /// رسم الأشرطة المرجعية الأفقية
        /// </summary>
        private static void DrawReferenceLinesVertical(
            SKCanvas canvas,
            float leftMargin,
            float topMargin,
            float chartWidth,
            float chartHeight)
        {
            var bandValues = new[] { 40.0, 55.0, 65.0 };
            var bandColors = new[]
            {
                ReportTheme.FromHex(ReportTheme.Colors.Weak),
                ReportTheme.FromHex(ReportTheme.Colors.Average),
                ReportTheme.FromHex(ReportTheme.Colors.Excellent)
            };

            using var linePaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1.5f,
                PathEffect = SKPathEffect.CreateDash(new[] { 8f, 4f }, 0)
            };

            foreach (var (value, color) in bandValues.Zip(bandColors))
            {
                var normalized = (value - 20) / 60.0;
                var y = topMargin + chartHeight - (float)(chartHeight * normalized);

                linePaint.Color = color.WithAlpha(100);
                canvas.DrawLine(leftMargin, y, leftMargin + chartWidth, y, linePaint);
            }
        }

        /// <summary>
        /// رسم الأعمدة الرأسية
        /// </summary>
        private static void DrawVerticalBars(
            SKCanvas canvas,
            List<DimensionScore> dimensions,
            float leftMargin,
            float topMargin,
            float chartWidth,
            float chartHeight,
            float bottomMargin)
        {
            var barWidth = chartWidth / dimensions.Count;
            var barPadding = barWidth * 0.2f;
            var actualBarWidth = barWidth - barPadding;

            for (int i = 0; i < dimensions.Count; i++)
            {
                var dim = dimensions[i];
                var x = leftMargin + (i * barWidth) + (barPadding / 2f);

                var normalized = Math.Clamp((dim.T - 20) / 60.0, 0, 1);
                var barHeight = (float)(chartHeight * normalized);
                var y = topMargin + chartHeight - barHeight;

                var barColor = ReportTheme.FromHex(ReportTheme.GetBandColor(dim.T));
                DrawGradientBar(canvas, x, y, actualBarWidth, barHeight, barColor);

                // قيمة T فوق العمود
                DrawBarValueVertical(canvas, dim.T, x + actualBarWidth / 2f, y - 5);

                // اسم البُعد أسفل المحور (مائل)
                DrawDimensionLabelVertical(canvas, dim.Dimension, x + actualBarWidth / 2f, 
                    topMargin + chartHeight + 15);
            }
        }

        /// <summary>
        /// رسم قيمة فوق العمود الرأسي
        /// </summary>
        private static void DrawBarValueVertical(SKCanvas canvas, double tScore, float x, float y)
        {
            var valueText = ReportTheme.FormatNum(tScore, 1);
            
            using var paint = new SKPaint
            {
                IsAntialias = true,
                Color = ReportTheme.FromHex(ReportTheme.Colors.Text)
            };

            var font = new SKFont(SKTypeface.Default, 9f)
            {
                Embolden = true
            };

            canvas.DrawText(valueText, x, y, SKTextAlign.Center, font, paint);
        }

        /// <summary>
        /// رسم تسمية مائلة أسفل العمود
        /// </summary>
        private static void DrawDimensionLabelVertical(SKCanvas canvas, string dimensionName, float x, float y)
        {
            using var paint = new SKPaint
            {
                IsAntialias = true,
                Color = ReportTheme.FromHex(ReportTheme.Colors.TextSecondary)
            };

            var font = new SKFont(_arabicTypeface ?? SKTypeface.Default, 9f);
            var formatted = ArabicTextRenderer.FormatDimensionName(dimensionName, 12);

            canvas.Save();
            canvas.Translate(x, y);
            canvas.RotateDegrees(-45);

            if (_arabicShaper != null && ArabicTextRenderer.ContainsArabic(formatted))
            {
                canvas.DrawShapedText(_arabicShaper, formatted, 0, 0, font, paint);
            }
            else
            {
                canvas.DrawText(formatted, 0, 0, SKTextAlign.Left, font, paint);
            }

            canvas.Restore();
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
                        Console.WriteLine("[BarChartRenderer] ✓ Arabic font loaded");
                    }
                    else
                    {
                        _arabicTypeface = SKTypeface.Default;
                        _arabicShaper = new SKShaper(_arabicTypeface);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[BarChartRenderer] ❌ Font error: {ex.Message}");
                    _arabicTypeface = SKTypeface.Default;
                    _arabicShaper = new SKShaper(_arabicTypeface);
                }
            }
        }
    }
}
