using SkiaSharp;
using PsyApi.Services.Scoring;

namespace PsyApi.Services.Reports
{
    /// <summary>
    /// Renders a heptagon (7-sided) radar/polar chart for the Seven Patterns
    /// Pure vector rendering with Skia - no PNG images
    /// Arabic labels with proper RTL support
    /// </summary>
    public static class HeptagonRadarChartRenderer
    {
        /// <summary>
        /// Renders a heptagon radar chart showing 7 pattern scores
        /// </summary>
        /// <param name="patternScores">The 7 pattern scores to visualize</param>
        /// <param name="size">Chart size (width and height in pixels)</param>
        /// <param name="title">Chart title in Arabic</param>
        /// <returns>PNG byte array for QuestPDF rendering</returns>
        public static byte[] RenderHeptagonChart(
            List<SevenPatternScore> patternScores,
            int size = 500,
            string title = "الخريطة النفسية السباعية")
        {
            // Ensure we have exactly 7 patterns (pad with defaults if needed)
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

            using var surface = SKSurface.Create(new SKImageInfo(size, size));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);

            // Chart parameters
            var centerX = size / 2f;
            var centerY = size / 2f;
            var maxRadius = size * 0.35f; // Leave room for labels
            var titleHeight = 50f;
            var adjustedCenterY = centerY + (titleHeight / 4); // Shift chart down for title

            // Draw title
            using var titlePaint = new SKPaint
            {
                Color = SKColor.Parse("#1a365d"),
                IsAntialias = true
            };
            using var titleFont = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold), 22);
            canvas.DrawText(title, centerX, titleHeight - 10, SKTextAlign.Center, titleFont, titlePaint);

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

                // Draw Arabic label at end of axis
                var labelDistance = maxRadius + 35;
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

            // Encode to PNG
            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
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
        /// Draws Arabic text label with proper positioning based on angle
        /// </summary>
        private static void DrawArabicLabel(SKCanvas canvas, string text, float x, float y, double angle)
        {
            using var textPaint = new SKPaint
            {
                Color = SKColor.Parse("#1f2937"),
                IsAntialias = true
            };
            using var textFont = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal), 11);

            // Adjust text alignment based on position
            SKTextAlign align;
            if (angle >= -100 && angle <= -80)
            {
                // Top - center
                align = SKTextAlign.Center;
            }
            else if (angle > -80 && angle < 0)
            {
                // Top-right - left align
                align = SKTextAlign.Left;
                x += 5;
            }
            else if (angle >= 0 && angle <= 100)
            {
                // Bottom-right - left align
                align = SKTextAlign.Left;
                x += 5;
            }
            else if (angle > 100 && angle < 170)
            {
                // Bottom - center
                align = SKTextAlign.Center;
                y += 5;
            }
            else
            {
                // Left side - right align
                align = SKTextAlign.Right;
                x -= 5;
            }

            // Handle multi-line wrapping for long labels
            var words = text.Split(' ');
            if (words.Length > 3)
            {
                var line1 = string.Join(" ", words.Take(3));
                var line2 = string.Join(" ", words.Skip(3));
                canvas.DrawText(line1, x, y - 6, align, textFont, textPaint);
                canvas.DrawText(line2, x, y + 6, align, textFont, textPaint);
            }
            else if (words.Length > 2)
            {
                var line1 = string.Join(" ", words.Take(2));
                var line2 = words.Last();
                canvas.DrawText(line1, x, y - 6, align, textFont, textPaint);
                canvas.DrawText(line2, x, y + 6, align, textFont, textPaint);
            }
            else
            {
                canvas.DrawText(text, x, y, align, textFont, textPaint);
            }
        }
    }
}
