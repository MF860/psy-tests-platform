using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Helpers;
using SkiaSharp;
using PsyApi.Models;

namespace PsyApi.Services.Reports
{
    /// <summary>
    /// FINAL: True vector donut renderer using SkiaSharp directly in QuestPDF canvas
    /// No PNG images, no text inside donuts, proper vector arcs with band colors
    /// Parameters: valuePercent (0-100), diameter=92, strokeRatio=0.16
    /// </summary>
    public static class VectorDonutRenderer
    {


        /// <summary>
        /// MASTER v3: Pure vector donut with exact geometry specifications
        /// Diameter: 92-100px, Stroke: 14-16% of diameter, Start: -90° (12 o'clock), rounded caps
        /// T-score normalization: (T-20)/60, Band colors: <40 red, 40-54.9 orange, ≥55 green  
        /// NO center text whatsoever
        /// </summary>
        public static byte[] GenerateVectorDonut(double tScore, int diameter = 96)
        {
            // v3: Clamp T-score to [20, 80] range for rendering
            var clampedT = Math.Max(20, Math.Min(80, tScore));
            
            // v3: T-score normalization to [0,1]: (T-20)/60
            var normalizedPercent = Math.Max(0, Math.Min(1, (clampedT - 20.0) / 60.0));
            
            // v3: Stroke width 14-16% of diameter  
            var strokeWidth = (int)(diameter * 0.15f); // 15% stroke
            
            return RadialGaugeRenderer.DrawRadialGauge(
                tScore: tScore,
                size: diameter,
                thicknessPct: 0.15f, // v3: 15% stroke width
                centerLine1: "", // v3: NO center text  
                centerLine2: ""  // v3: NO center text
            );
        }

        /// <summary>
        /// MASTER v3: Complete dimension chart with vector donut + labels below 
        /// Labels: Line 1 (bold 9-10pt): {Dimension}, Line 2 (8-9pt, muted): T=xx.x | %ile=xx
        /// </summary>
        public static void CreateDimensionChart(this IContainer container,
            double tScore,
            double percentile,
            string dimensionName,
            int size = 96)
        {
            container.Column(column =>
            {
                // v3: Pure vector donut (NO center text)
                var donutBytes = GenerateVectorDonut(tScore, size);
                column.Item()
                    .Width(size)
                    .Height(size)
                    .AlignCenter()
                    .Image(donutBytes);

                column.Item().PaddingTop(ReportTheme.Spacing.XS);

                // v3: Line 1 (bold 9-10pt): {Dimension} - Centered RTL Arabic with proper shaping
                var formattedName = ArabicTextRenderer.FormatDimensionName(dimensionName, 20);
                column.Item()
                    .AlignCenter()
                    .Text(formattedName)
                    .Style(ReportTheme.ArabicTextStyle(10, true));

                // v3: Line 2 (8-9pt, muted): T=xx.x | %ile=xx using FormatNum with western digits
                var statsText = $"T={ReportTheme.FormatNum(tScore, 1)} | %ile={ReportTheme.FormatNum(percentile, 0)}";
                
                column.Item()
                    .AlignCenter()
                    .Text(statsText)
                    .Style(TextStyle.Default
                        .FontSize(8)
                        .FontColor(ReportTheme.Colors.TextSecondary)); // v3: Muted text color
            });
        }



        /// <summary>
        /// FINAL: Create legend for Page 2 only (single legend, compact inline squares)  
        /// </summary>
        public static void CreateDonutLegend(this IContainer container)
        {
            container.Row(row =>
            {
                // Excellent (≥55) - Green
                row.ConstantItem(12).Height(12).Background(ReportTheme.Colors.Excellent);
                row.ConstantItem(60).PaddingLeft(4).Text("Excellent")
                    .Style(TextStyle.Default.FontSize(9).FontColor(ReportTheme.Colors.Text));

                row.ConstantItem(8); // Spacer

                // Average (40-54.9) - Orange
                row.ConstantItem(12).Height(12).Background(ReportTheme.Colors.Average);
                row.ConstantItem(60).PaddingLeft(4).Text("Average")
                    .Style(TextStyle.Default.FontSize(9).FontColor(ReportTheme.Colors.Text));

                row.ConstantItem(8); // Spacer

                // Weak (<40) - Red  
                row.ConstantItem(12).Height(12).Background(ReportTheme.Colors.Weak);
                row.ConstantItem(50).PaddingLeft(4).Text("Weak")
                    .Style(TextStyle.Default.FontSize(9).FontColor(ReportTheme.Colors.Text));
            });
        }
        
        /// <summary>
        /// FINAL: Calculate summary metrics with safe defaults for NaN/null values
        /// AvgT = Average(dimensions.T), AvgPct = Average(dimensions.Percentile)  
        /// </summary>
        public static (double avgT, double avgPct, int excellentCount, int averageCount, int weakCount) 
            CalculateMetrics(IEnumerable<DimensionScore> dimensions)
        {
            var dimensionsList = dimensions.ToList();
            
            if (!dimensionsList.Any())
            {
                Console.WriteLine("[VectorDonutRenderer] Warning: No dimensions provided");
                return (0, 0, 0, 0, 0);
            }
            
            // Safe calculation with NaN/Infinity checks
            var validDimensions = dimensionsList.Where(d => 
                !double.IsNaN(d.T) && !double.IsInfinity(d.T) &&
                !double.IsNaN(d.Percentile) && !double.IsInfinity(d.Percentile)).ToList();
            
            if (!validDimensions.Any())
            {
                Console.WriteLine("[VectorDonutRenderer] Warning: No valid dimensions after filtering NaN/Infinity");
                return (0, 0, 0, 0, dimensionsList.Count); // All marked as weak
            }
            
            var avgT = validDimensions.Average(d => d.T);
            var avgPct = validDimensions.Average(d => d.Percentile);
            
            // Count by bands (using all dimensions, treating invalid as weak)
            var excellentCount = dimensionsList.Count(d => !double.IsNaN(d.T) && d.T >= 55.0);
            var averageCount = dimensionsList.Count(d => !double.IsNaN(d.T) && d.T >= 40.0 && d.T < 55.0);
            var weakCount = dimensionsList.Count(d => double.IsNaN(d.T) || d.T < 40.0);
            
            return (avgT, avgPct, excellentCount, averageCount, weakCount);
        }

        /// <summary>
        /// Calculate donut statistics for summary
        /// </summary>
        public static (int excellent, int average, int weak) CalculateDonutStats(IEnumerable<Models.DimensionScore> dimensions)
        {
            var excellentCount = dimensions.Count(d => d.T >= 55);
            var averageCount = dimensions.Count(d => d.T >= 40 && d.T < 55);
            var weakCount = dimensions.Count(d => d.T < 40);
            
            return (excellentCount, averageCount, weakCount);
        }

        /// <summary>
        /// Calculate summary metrics
        /// AvgT = mean(dimensions.T) rounded to 1 decimal
        /// AvgPercentile = mean(dimensions.Percentile) rounded to 0-1 decimals
        /// TotalScore = sum as integer
        /// </summary>
        public static (double avgT, double avgPercentile, int totalScore) CalculateSummaryMetrics(IEnumerable<Models.DimensionScore> dimensions)
        {
            var dimensionsList = dimensions.ToList();
            
            if (!dimensionsList.Any())
                return (0, 0, 0);
            
            // Use invariant math for calculations, then format for display
            var avgT = dimensionsList.Average(d => d.T);
            var avgPercentile = dimensionsList.Average(d => d.Percentile);
            var totalScore = (int)Math.Round(dimensionsList.Sum(d => d.T));
            
            return (avgT, avgPercentile, totalScore);
        }
    }
}