using SkiaSharp;
using SkiaSharp.HarfBuzz;
using PsyApi.Services.Scoring;

namespace PsyApi.Services.Reports
{
    /// <summary>
    /// Renders a heptagon (7-sided) radar/polar chart for the Seven Patterns
    /// Pure vector rendering with Skia - Arabic labels with HarfBuzz shaping
    /// </summary>
    public static class HeptagonRadarChartRenderer
    {
        private static SKTypeface? _arabicTypeface;
        private static SKShaper? _arabicShaper;
        private static readonly object _lock = new();

        /// <summary>
        /// Renders a heptagon radar chart showing 7 pattern scores with proper Arabic labels
        /// HiFi version: 3× scale, Quality=100, full antialiasing
        /// </summary>
        public static byte[] RenderHeptagonChart(
            List<SevenPatternScore> patternScores,
            int size = 500,
            string title = "الخريطة النفسية السباعية")
        {
            EnsureArabicFont();

            // HiFi settings
            var scaleFactor = HiFiSettings.GetScaleFactor();
            var actualSize = (int)(size * scaleFactor);

            // Ensure we have exactly 7 patterns
            var orderedPatterns = patternScores.OrderBy(p => p.PatternKey).Take(7).ToList();
            while (orderedPatterns.Count < 7)
            {
                orderedPatterns.Add(new SevenPatternScore
                {
                    PatternNameAr = "غير محدد",
                    TScore = 50.0,
                    Band = "Average"
                });
            }

            using var surface = SKSurface.Create(new SKImageInfo(actualSize, actualSize));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);
            canvas.Scale(scaleFactor);

            // Apply HiFi antialiasing settings
            canvas.Save();

            // Chart parameters - Reduced radius to leave more room for Arabic labels
            var centerX = size / 2f;
            var centerY = size / 2f;
            var maxRadius = size * 0.31f; // Reduced from 0.35f for better label visibility
            var titleHeight = 50f;
            var adjustedCenterY = centerY + (titleHeight / 4); // Shift chart down for title

            // Draw title with Arabic font (HarfBuzz shaping)
            using var titlePaint = HiFiSettings.GetHiFiPaint();
            titlePaint.Color = SKColor.Parse("#1a365d");
            
            var titleTypeface = _arabicTypeface ?? SKTypeface.Default;
            using var titleFont = new SKFont(titleTypeface, 22 * scaleFactor);
            titleFont.Hinting = SKFontHinting.Full;
            
            // Use HarfBuzz shaper for Arabic text in title
            if (_arabicShaper != null && title.Any(c => c >= 0x0600 && c <= 0x06FF))
            {
                var shapedTitle = _arabicShaper.Shape(title, titleFont);
                if (shapedTitle?.Points != null && shapedTitle.Points.Length > 0)
                {
                    var titleWidth = shapedTitle.Points.LastOrDefault().X;
                    canvas.DrawShapedText(_arabicShaper, title, centerX - titleWidth / 2, titleHeight - 10, titleFont, titlePaint);
                }
                else
                {
                    canvas.DrawText(title, centerX, titleHeight - 10, SKTextAlign.Center, titleFont, titlePaint);
                }
            }
            else
            {
                canvas.DrawText(title, centerX, titleHeight - 10, SKTextAlign.Center, titleFont, titlePaint);
            }

            // Draw concentric rings (grid) at T-scores: 40, 55, 70
            var ringTScores = new[] { 40.0, 55.0, 70.0 };
            var ringColors = new[] { "#ef4444", "#f97316", "#22c55e" }; // Red, Orange, Green

            using var gridPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1,
                IsAntialias = true,
                Color = SKColor.Parse("#e5e7eb")
            };

            using var labelPaint = new SKPaint
            {
                Color = SKColor.Parse("#6b7280"),
                IsAntialias = true
            };
            using var labelFont = new SKFont(SKTypeface.Default, 10);

            foreach (var ringT in ringTScores)
            {
                var ringRadius = MapTScoreToRadius(ringT, maxRadius);
                canvas.DrawCircle(centerX, adjustedCenterY, ringRadius, gridPaint);
                
                // Label the ring with T-score value
                canvas.DrawText($"T={ringT:F0}", centerX, adjustedCenterY - ringRadius - 5, SKTextAlign.Center, labelFont, labelPaint);
            }

            // Draw 7 axes from center
            var angleStep = 360.0 / 7.0; // 51.43 degrees
            var startAngle = -90.0; // Start at top

            using var axisPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1,
                IsAntialias = true,
                Color = SKColor.Parse("#9ca3af")
            };

            var dataPoints = new List<SKPoint>();

            for (int i = 0; i < 7; i++)
            {
                var angle = startAngle + (i * angleStep);
                var angleRad = angle * Math.PI / 180.0;
                
                var endX = centerX + maxRadius * (float)Math.Cos(angleRad);
                var endY = adjustedCenterY + maxRadius * (float)Math.Sin(angleRad);
                
                // Draw axis line
                canvas.DrawLine(centerX, adjustedCenterY, endX, endY, axisPaint);

                // Calculate data point position based on T-score
                var pattern = orderedPatterns[i];
                var dataRadius = MapTScoreToRadius(pattern.TScore, maxRadius);
                var dataX = centerX + dataRadius * (float)Math.Cos(angleRad);
                var dataY = adjustedCenterY + dataRadius * (float)Math.Sin(angleRad);
                dataPoints.Add(new SKPoint(dataX, dataY));

                // Draw Arabic label at end of axis - Increased distance for readability
                var labelDistance = maxRadius + 50; // Increased from 35 to 50 for better spacing
                var labelX = centerX + labelDistance * (float)Math.Cos(angleRad);
                var labelY = adjustedCenterY + labelDistance * (float)Math.Sin(angleRad);
                
                DrawArabicLabel(canvas, pattern.PatternNameAr, labelX, labelY, angle);
            }

            // Draw filled polygon connecting data points
            if (dataPoints.Count >= 3)
            {
                using var fillPaint = new SKPaint
                {
                    Style = SKPaintStyle.Fill,
                    IsAntialias = true,
                    Color = SKColor.Parse("#3b82f6").WithAlpha(50) // Blue with transparency
                };

                using var strokePaint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 2,
                    IsAntialias = true,
                    Color = SKColor.Parse("#2563eb") // Solid blue
                };

                using var path = new SKPath();
                path.MoveTo(dataPoints[0]);
                for (int i = 1; i < dataPoints.Count; i++)
                {
                    path.LineTo(dataPoints[i]);
                }
                path.Close();

                canvas.DrawPath(path, fillPaint);
                canvas.DrawPath(path, strokePaint);
            }

            // Draw data points as circles
            using var pointPaint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                IsAntialias = true,
                Color = SKColor.Parse("#1e40af") // Dark blue
            };

            foreach (var point in dataPoints)
            {
                canvas.DrawCircle(point.X, point.Y, 4, pointPaint);
            }

            // Encode to JPEG with HiFi quality
            canvas.Restore();
            using var image = surface.Snapshot();
            
            // Downscale to target size with high-quality filtering
            using var resizedBitmap = new SKBitmap(size, size);
            var pixmap = resizedBitmap.PeekPixels();
            var samplingOptions = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);
            image.ScalePixels(pixmap, samplingOptions);

            using var finalImage = SKImage.FromBitmap(resizedBitmap);
            using var data = finalImage.Encode(SKEncodedImageFormat.Jpeg, HiFiSettings.GetJpegQuality());
            return data.ToArray();
        }

        /// <summary>
        /// Maps T-score (20-80 range) to radius (0 to maxRadius)
        /// T=20 → 0, T=50 → mid, T=80 → maxRadius
        /// </summary>
        private static float MapTScoreToRadius(double tScore, float maxRadius)
        {
            const double minT = 20.0;
            const double maxT = 80.0;
            
            // Clamp T-score
            var clampedT = Math.Clamp(tScore, minT, maxT);
            
            // Normalize to 0-1 range
            var normalized = (clampedT - minT) / (maxT - minT);
            
            return maxRadius * (float)normalized;
        }

        /// <summary>
        /// Draws Arabic text label with HarfBuzz shaping for proper rendering
        /// </summary>
        private static void DrawArabicLabel(SKCanvas canvas, string text, float x, float y, double angle)
        {
            if (_arabicShaper == null || _arabicTypeface == null)
            {
                // Fallback to simple text if fonts not loaded
                using var fallbackPaint = new SKPaint { Color = SKColor.Parse("#1f2937"), IsAntialias = true };
                using var fallbackFont = new SKFont(SKTypeface.FromFamilyName("Arial"), 11);
                canvas.DrawText(text, x, y, SKTextAlign.Center, fallbackFont, fallbackPaint);
                return;
            }

            using var textPaint = new SKPaint
            {
                Color = SKColor.Parse("#1f2937"),
                IsAntialias = true
            };
            using var textFont = new SKFont(_arabicTypeface, 11);

            // Calculate text width with HarfBuzz
            var shapedText = _arabicShaper.Shape(text, textFont);
            var textWidth = shapedText.Width;

            // Adjust text alignment based on angle
            float offsetX = 0;
            float offsetY = 0;

            if (angle >= -100 && angle <= -80)
            {
                // Top - center
                offsetX = -textWidth / 2;
                offsetY = -5;
            }
            else if (angle > -80 && angle < 0)
            {
                // Top-right
                offsetX = 5;
                offsetY = 0;
            }
            else if (angle >= 0 && angle <= 100)
            {
                // Bottom-right
                offsetX = 5;
                offsetY = 0;
            }
            else if (angle > 100 && angle < 170)
            {
                // Bottom - center
                offsetX = -textWidth / 2;
                offsetY = 15;
            }
            else
            {
                // Left side - right align
                offsetX = -textWidth - 5;
                offsetY = 0;
            }

            // Handle multi-line wrapping for long labels (> 20 chars)
            if (text.Length > 20)
            {
                var words = text.Split(' ');
                if (words.Length > 2)
                {
                    var line1 = string.Join(" ", words.Take(2));
                    var line2 = string.Join(" ", words.Skip(2));
                    
                    var shaped1 = _arabicShaper.Shape(line1, textFont);
                    var shaped2 = _arabicShaper.Shape(line2, textFont);
                    
                    canvas.DrawShapedText(_arabicShaper, line1, x + offsetX, y + offsetY - 8, textFont, textPaint);
                    canvas.DrawShapedText(_arabicShaper, line2, x + offsetX, y + offsetY + 8, textFont, textPaint);
                    return;
                }
            }

            canvas.DrawShapedText(_arabicShaper, text, x + offsetX, y + offsetY, textFont, textPaint);
        }

        /// <summary>
        /// Ensures Arabic font and HarfBuzz shaper are loaded
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
                        Console.WriteLine("[HeptagonRenderer] ✓ Arabic font loaded with HarfBuzz shaper");
                    }
                    else
                    {
                        Console.WriteLine($"[HeptagonRenderer] ⚠ Arabic font not found: {regularPath}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[HeptagonRenderer] ⚠ Font loading error: {ex.Message}");
                }
            }
        }
    }
}
