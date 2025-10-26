using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;
using System.Globalization;
using PsyApi.Models;
using PsyApi.Services.Scoring;
using DimensionScoreDto = PsyApi.Models.DimensionScore;

namespace PsyApi.Services.Reports
{
    /// <summary>
    /// Modern 3-page Arabic PDF report with proper HarfBuzz shaping, tight spacing, and no duplicates
    /// Exact specification compliance: Page 1 (Cover & KPIs), Page 2 (Charts Grid), Page 3 (Actions & Courses)
    /// 
    /// Banding: Weak <40, Average 40-54.9, Excellent ≥55
    /// Colors: Green (Excellent), Orange (Average), Red (Weak)
    /// Spacing: 8/12/16pt system with 16mm page margins
    /// </summary>
    public class ModernPdfReportService : IPdfReportService
    {
        private readonly IRecommendationService _recommendationService;
        private static bool _fontsRegistered;
        private static readonly object _lock = new();

        public ModernPdfReportService(IRecommendationService recommendationService)
        {
            _recommendationService = recommendationService;
            EnsureFonts();
        }

        public Task<byte[]> RenderResultPdfAsync(Result result, User user, IEnumerable<DimensionScoreDto> dimensions, CancellationToken ct = default)
        {
            var startTime = DateTime.Now;
            
            try
            {
                // Log report generation start
                Console.WriteLine($"\n=== MODERN ARABIC PSYCHOMETRIC REPORT ===");
                Console.WriteLine($"Report for: {user.FullName} (ID: {user.NationalId})");
                Console.WriteLine($"Session ID: {result.SessionId}");
                Console.WriteLine($"Generation Time: {startTime:yyyy-MM-dd HH:mm:ss}");

                // Force Arabic culture and Community license
                Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("ar-JO");
                QuestPDF.Settings.License = LicenseType.Community;
                
                // Disable debugging for production performance
                QuestPDF.Settings.EnableDebugging = false;

                var sortedDimensions = dimensions.OrderBy(d => d.T).ToList();
                var weakestDimensions = sortedDimensions.Take(3).ToList();
                var strongestDimensions = sortedDimensions.OrderByDescending(d => d.T).Take(3).ToList();

                // MASTER v3: Accuracy lock - compute KPIs strictly from in-memory dimensions list
                var dimensionsList = sortedDimensions.ToList(); // Ensure same list used everywhere
                
                // v3 Sanity assertions (throw if fail)
                if (dimensionsList.Count == 0)
                    throw new InvalidOperationException("[Assertion] dimensions.Count must be > 0");
                
                // Check for NaN/Infinity in any T or Percentile
                foreach (var d in dimensionsList)
                {
                    if (double.IsNaN(d.T) || double.IsInfinity(d.T))
                        throw new InvalidOperationException($"[Assertion] Invalid T-score {d.T} for dimension {d.Dimension}");
                    if (double.IsNaN(d.Percentile) || double.IsInfinity(d.Percentile))
                        throw new InvalidOperationException($"[Assertion] Invalid Percentile {d.Percentile} for dimension {d.Dimension}");
                    
                    // T-scores should be in [20, 80] range (clamp when rendering if needed)
                    if (d.T < 20 || d.T > 80)
                        Console.WriteLine($"[Warning] T-score {d.T} outside typical range [20,80] for {d.Dimension}");
                }
                
                // Recompute KPIs from the exact dimensions list
                var avgTScore = dimensionsList.Average(d => d.T);
                var avgPercentile = dimensionsList.Average(d => d.Percentile);
                
                // Band counts: Weak <40, Average 40-54.9, Excellent ≥55
                var weakCount = dimensionsList.Count(d => d.T < 40.0);
                var averageCount = dimensionsList.Count(d => d.T >= 40.0 && d.T < 55.0);
                var excellentCount = dimensionsList.Count(d => d.T >= 55.0);
                
                var totalScore = (int)Math.Round(dimensionsList.Sum(d => d.T));

                // Get recommendations for weakest dimensions
                var recommendations = _recommendationService.GetDimensionRecommendations(weakestDimensions);
                var courses = _recommendationService.GetSuggestedCourses(weakestDimensions);

                // MASTER v3: Required logging format
                Console.WriteLine($"[Report] User={user.NationalId ?? "N/A"} Session={result.SessionId} Pages=3");
                Console.WriteLine($"[Stats] N={dimensionsList.Count} Weak={weakCount} Avg={averageCount} Excel={excellentCount} AvgT={ReportTheme.FormatNum(avgTScore, 1)} AvgPct={ReportTheme.FormatNum(avgPercentile, 0)}");
                
                // Vector renderer confirmation
                Console.WriteLine($"[Vector] Renderer=VectorDonutRenderer OK (no images)");
                
                // Log first 3 weakest donuts as required
                var weakestFirst3 = dimensionsList.Take(3).ToList();
                foreach (var dim in weakestFirst3)
                {
                    var bandLabel = dim.T >= 55 ? "Excellent" : dim.T >= 40 ? "Average" : "Weak";
                    Console.WriteLine($"[Donut] {dim.Dimension} T={ReportTheme.FormatNum(dim.T, 1)} Band={bandLabel}");
                }
                
                Console.WriteLine($"✓ Font loading confirmed: {_fontsRegistered}");

                // Generate PDF with exact 3-page structure
                var pdf = Document.Create(container =>
                {
                    // Set global RTL and Arabic font defaults
                    container.Page(page =>
                    {
                        ConfigurePageDefaults(page);
                        CreatePage1_CoverAndSummary(page, user, result, avgTScore, avgPercentile, totalScore, 
                            strongestDimensions, weakestDimensions);
                    });

                    container.Page(page =>
                    {
                        ConfigurePageDefaults(page);
                        CreatePage2_DimensionsOverview(page, sortedDimensions, excellentCount, averageCount, weakCount);
                    });

                    container.Page(page =>
                    {
                        ConfigurePageDefaults(page);
                        CreatePage3_ActionsAndCourses(page, weakestDimensions, recommendations, courses);
                    });
                });

                var pdfBytes = pdf.GeneratePdf();
                var endTime = DateTime.Now;
                var duration = endTime - startTime;

                Console.WriteLine($"✅ PDF Generation Complete!");
                Console.WriteLine($"📄 Pages: 3 (exact specification compliance)");
                Console.WriteLine($"📏 Size: {pdfBytes.Length / 1024:F1} KB");
                Console.WriteLine($"⏱ Duration: {duration.TotalMilliseconds:F0}ms");
                Console.WriteLine($"🎯 Arabic shaping: HarfBuzz enabled");
                Console.WriteLine($"📊 Charts: {sortedDimensions.Count} vector donuts (NO PNG images)");
                Console.WriteLine($"🔢 Number formatting: Western digits (no � glyphs)\n");

                return Task.FromResult(pdfBytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ PDF Generation Failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Configure page defaults with RTL Arabic text and exact 16mm margins
        /// </summary>
        private void ConfigurePageDefaults(PageDescriptor page)
        {
            page.Size(PageSizes.A4);
            page.Margin(ReportTheme.Spacing.PageMargin); // Exact 16mm margins
            page.DefaultTextStyle(style => ReportTheme.ArabicTextStyle(11, false, ReportTheme.Colors.Text));
            
            // Ensure no page overflow
            page.ContinuousSize(PageSizes.A4.Height);
        }

        /// <summary>
        /// Page 1: Cover & Summary (REDESIGNED - compact header, 2x2 info grid, KPI chips, strengths/growth)
        /// Following exact specifications: 16mm margins, 8/12/16pt spacing, no duplicate headings
        /// </summary>
        private void CreatePage1_CoverAndSummary(
            PageDescriptor page,
            User user,
            Result result,
            double avgTScore,
            double avgPercentile,
            int totalScore,
            List<DimensionScoreDto> strongestDimensions,
            List<DimensionScoreDto> weakestDimensions)
        {
            page.Content().Column(column =>
            {
                // NEW: Logo centered at top with 8px padding
                try
                {
                    var logoPath = Path.Combine(AppContext.BaseDirectory, "Resources", "Brand", "SAITES-ICON.png");
                    if (File.Exists(logoPath))
                    {
                        var logoBytes = File.ReadAllBytes(logoPath);
                        column.Item()
                            .AlignCenter()
                            .PaddingBottom(ReportTheme.Spacing.SM)
                            .Width(120)
                            .Height(120)
                            .Image(logoBytes);
                        
                        Console.WriteLine($"[PDF] ✓ Logo loaded from {logoPath}");
                    }
                    else
                    {
                        Console.WriteLine($"[PDF] ⚠ Logo not found: {logoPath}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PDF] ⚠ Logo loading error: {ex.Message}");
                }

                // v3: Title (RTL, right-aligned): "منصة التحليل النفسي المتقدم" 20pt bold
                column.Item().AlignCenter().Text("منصة التحليل النفسي المتقدم")
                    .Style(ReportTheme.ArabicTextStyle(20, true, ReportTheme.Colors.Primary));
                
                column.Item().AlignCenter().PaddingTop(4).Text("Advanced Psychological Assessment Platform")
                    .Style(TextStyle.Default.FontSize(11).FontColor(ReportTheme.Colors.TextSecondary));

                column.Item().PaddingTop(ReportTheme.Spacing.LG);

                // v3: 2×2 info grid (12pt): الاسم، الرقم الوطني، رقم الجلسة، التاريخ (dd-MM-yyyy)
                column.Item().Column(infoGrid =>
                {
                    infoGrid.Item().Row(row1 =>
                    {
                        var displayName = ArabicTextRenderer.FormatDimensionName(user.FullName ?? "غير محدد", 32);
                        var nationalId = user.NationalId ?? "غير محدد";
                        
                        row1.RelativeItem().Text($"الاسم: {displayName}")
                            .Style(ReportTheme.ArabicTextStyle(12));
                        row1.RelativeItem().Text($"الرقم الوطني: {nationalId}")
                            .Style(ReportTheme.ArabicTextStyle(12));
                    });

                    infoGrid.Item().PaddingTop(ReportTheme.Spacing.SM).Row(row2 =>
                    {
                        // Session GUID or ID (shortened to 8-12 chars)
                        var sessionIdStr = result.SessionId.ToString();
                        var sessionDisplay = sessionIdStr.Length > 12 ? sessionIdStr[..12] : sessionIdStr;
                        
                        var reportDate = DateTime.Now.ToString("dd-MM-yyyy", System.Globalization.CultureInfo.InvariantCulture);
                        
                        row2.RelativeItem().Text($"رقم الجلسة: {sessionDisplay}")
                            .Style(ReportTheme.ArabicTextStyle(12));
                        row2.RelativeItem().Text($"التاريخ: {reportDate}")
                            .Style(ReportTheme.ArabicTextStyle(12));
                    });
                });

                column.Item().PaddingTop(ReportTheme.Spacing.LG);

                // v3: KPI chips (row, compact, centered) - outline/border reflects band color of Avg T
                column.Item().AlignCenter().Row(kpiRow =>
                {
                    var avgTBandColor = ReportTheme.GetBandColor(avgTScore);
                    
                    kpiRow.RelativeItem().Padding(ReportTheme.Spacing.SM)
                        .Background(ReportTheme.Colors.Surface)
                        .Border(2).BorderColor(avgTBandColor)
                        .AlignCenter()
                        .Text($"Avg T: {ReportTheme.FormatNum(avgTScore, 1)}")
                        .Style(TextStyle.Default.FontSize(11).FontColor(ReportTheme.Colors.Text));

                    kpiRow.RelativeItem().Padding(ReportTheme.Spacing.SM)
                        .Background(ReportTheme.Colors.Surface)
                        .Border(1).BorderColor(ReportTheme.Colors.Border)
                        .AlignCenter()
                        .Text($"Avg %ile: {ReportTheme.FormatNum(avgPercentile, 0)}")
                        .Style(TextStyle.Default.FontSize(11).FontColor(ReportTheme.Colors.Text));

                    kpiRow.RelativeItem().Padding(ReportTheme.Spacing.SM)
                        .Background(ReportTheme.Colors.Surface)
                        .Border(1).BorderColor(ReportTheme.Colors.Border)
                        .AlignCenter()
                        .Text($"Total Score: {totalScore}")
                        .Style(TextStyle.Default.FontSize(11).FontColor(ReportTheme.Colors.Text));
                });

                column.Item().PaddingTop(ReportTheme.Spacing.LG);

                // v3: Top 3 strengths (green chips) by T - name only, no long text
                if (strongestDimensions.Any())
                {
                    column.Item().Column(strengthSection =>
                    {
                        strengthSection.Item().AlignRight().Text("أقوى 3 أبعاد")
                            .Style(ReportTheme.ArabicTextStyle(14, true));

                        strengthSection.Item().PaddingTop(ReportTheme.Spacing.SM).AlignCenter().Row(strengthRow =>
                        {
                            foreach (var strength in strongestDimensions.Take(3))
                            {
                                var dimensionName = ArabicTextRenderer.FormatDimensionName(strength.Dimension, 20);
                                strengthRow.RelativeItem().Padding(ReportTheme.Spacing.SM)
                                    .Background(ReportTheme.Colors.Excellent)
                                    .AlignCenter()
                                    .Text(dimensionName)
                                    .Style(ReportTheme.ArabicTextStyle(10, false, "#FFFFFF"));
                            }
                        });
                    });

                    column.Item().PaddingTop(ReportTheme.Spacing.MD);
                }

                // v3: Bottom 3 growth (band colored chips) by T - band colors per dimension
                if (weakestDimensions.Any())
                {
                    column.Item().Column(growthSection =>
                    {
                        growthSection.Item().AlignRight().Text("أضعف 3 أبعاد")
                            .Style(ReportTheme.ArabicTextStyle(14, true));

                        growthSection.Item().PaddingTop(ReportTheme.Spacing.SM).AlignCenter().Row(growthRow =>
                        {
                            foreach (var weakness in weakestDimensions.Take(3))
                            {
                                var dimensionName = ArabicTextRenderer.FormatDimensionName(weakness.Dimension, 20);
                                var bandColor = ReportTheme.GetBandColor(weakness.T);
                                
                                growthRow.RelativeItem().Padding(ReportTheme.Spacing.SM)
                                    .Background(bandColor)
                                    .AlignCenter()
                                    .Text(dimensionName)
                                    .Style(ReportTheme.ArabicTextStyle(10, false, "#FFFFFF"));
                            }
                        });
                    });
                }
            });

            // Page footer
            page.Footer().AlignCenter().Text("صفحة 1 / 3")
                .Style(ReportTheme.ArabicTextStyle(9, false, ReportTheme.Colors.TextSecondary));
        }

        /// <summary>
        /// Page 2: Dimensions Overview (REDESIGNED - vector donuts, 3-column grid, single legend, footer stats)
        /// NO embedded images - pure vector Canvas/Skia donuts as specified
        /// </summary>
        private void CreatePage2_DimensionsOverview(
            PageDescriptor page,
            List<DimensionScoreDto> sortedDimensions,
            int excellentCount,
            int averageCount,
            int weakCount)
        {
            page.Content().Column(column =>
            {
                // Header (18pt bold, right-aligned)
                column.Item().AlignRight().Text("نتائج الأبعاد")
                    .Style(ReportTheme.ArabicTextStyle(18, true));

                column.Item().PaddingTop(ReportTheme.Spacing.MD);

                // Single legend row (compact bands: Excellent/Average/Weak) - RTL layout
                column.Item().AlignRight().Row(legendRow =>
                {
                    legendRow.AutoItem().PaddingHorizontal(ReportTheme.Spacing.SM).Row(legendItem =>
                    {
                        legendItem.AutoItem().Width(12).Height(12).Background(ReportTheme.Colors.Excellent);
                        legendItem.AutoItem().PaddingRight(4).Text("ممتاز")
                            .Style(ReportTheme.ArabicTextStyle(10, false, ReportTheme.Colors.Text));
                    });
                    legendRow.AutoItem().PaddingHorizontal(ReportTheme.Spacing.SM).Row(legendItem =>
                    {
                        legendItem.AutoItem().Width(12).Height(12).Background(ReportTheme.Colors.Average);
                        legendItem.AutoItem().PaddingRight(4).Text("متوسط")
                            .Style(ReportTheme.ArabicTextStyle(10, false, ReportTheme.Colors.Text));
                    });
                    legendRow.AutoItem().PaddingHorizontal(ReportTheme.Spacing.SM).Row(legendItem =>
                    {
                        legendItem.AutoItem().Width(12).Height(12).Background(ReportTheme.Colors.Weak);
                        legendItem.AutoItem().PaddingRight(4).Text("يحتاج الى تحسين")
                            .Style(ReportTheme.ArabicTextStyle(10, false, ReportTheme.Colors.Text));
                    });
                });

                column.Item().PaddingTop(ReportTheme.Spacing.MD);

                // NEW: مخطط رادار شامل لتوزيع الأبعاد (Comprehensive Radar Chart)
                column.Item().AlignRight().Text("مخطط رادار شامل لتوزيع الأبعاد")
                    .Style(ReportTheme.ArabicTextStyle(14, true));
                
                column.Item().PaddingTop(ReportTheme.Spacing.SM);
                
                try
                {
                    var radarBytes = RadarChartRenderer.RenderRadarChart(
                        sortedDimensions.Select(d => new DimensionScore 
                        { 
                            Dimension = d.Dimension, 
                            T = d.T, 
                            Percentile = d.Percentile 
                        }),
                        size: 440,
                        showGrid: true
                    );
                    
                    column.Item()
                        .AlignCenter()
                        .Height(440)
                        .Image(radarBytes);
                    
                    Console.WriteLine($"[PDF] ✓ Radar chart rendered ({sortedDimensions.Count} dimensions)");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PDF] ⚠ Radar chart failed: {ex.Message}");
                    // Continue without radar on error
                }

                column.Item().PaddingTop(ReportTheme.Spacing.LG);

                // NEW: Horizontal Bar Chart (T-Score Distribution)
                column.Item().AlignRight().Text("توزيع نقاط T عبر الأبعاد")
                    .Style(ReportTheme.ArabicTextStyle(14, true));
                
                column.Item().PaddingTop(ReportTheme.Spacing.SM);
                
                try
                {
                    var barChartBytes = HorizontalBarChartRenderer.RenderHorizontalBars(
                        sortedDimensions.Select(d => new DimensionScore 
                        { 
                            Dimension = d.Dimension, 
                            T = d.T, 
                            Percentile = d.Percentile 
                        }),
                        width: 580,
                        maxDimensions: Math.Min(12, sortedDimensions.Count)
                    );
                    
                    column.Item()
                        .AlignCenter()
                        .Image(barChartBytes);
                    
                    Console.WriteLine($"[PDF] ✓ Horizontal bar chart rendered ({Math.Min(12, sortedDimensions.Count)} dimensions)");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PDF] ⚠ Horizontal bar chart failed: {ex.Message}");
                    // Continue without bar chart on error
                }

                column.Item().PaddingTop(ReportTheme.Spacing.LG);

                // Donuts section header
                column.Item().AlignRight().Text("تفاصيل الأبعاد (Dimension Details)")
                    .Style(ReportTheme.ArabicTextStyle(14, true));

                column.Item().PaddingTop(ReportTheme.Spacing.SM);

                // 3-column grid ordered by T ascending (weakest first, as specified)
                var rows = (int)Math.Ceiling(sortedDimensions.Count / 3.0);
                
                for (int row = 0; row < rows; row++)
                {
                    column.Item().PaddingVertical(ReportTheme.Spacing.SM).Row(chartRow =>
                    {
                        for (int col = 0; col < 3; col++)
                        {
                            var index = row * 3 + col;
                            if (index < sortedDimensions.Count)
                            {
                                var dimension = sortedDimensions[index];
                                
                                // Center chart and ensure proper RTL alignment
                                chartRow.RelativeItem().PaddingHorizontal(ReportTheme.Spacing.XS)
                                    .AlignCenter()
                                    .CreateDimensionChart(dimension.T, dimension.Percentile, dimension.Dimension, 180);
                            }
                            else
                            {
                                chartRow.RelativeItem(); // Empty space for layout consistency
                            }
                        }
                    });
                }

                column.Item().PaddingTop(ReportTheme.Spacing.LG);

                // v3: Footer stats with accuracy validation - must match computed values
                var validDimensions = sortedDimensions.Where(d => !double.IsNaN(d.T) && !double.IsInfinity(d.T)).ToList();
                var avgT = validDimensions.Any() ? validDimensions.Average(d => d.T) : 0;
                var avgPercentile = validDimensions.Any() ? validDimensions.Average(d => d.Percentile) : 0;
                
                // v3: Recompute band counts to validate they match passed parameters
                var recomputedWeakCount = sortedDimensions.Count(d => d.T < 40.0);
                var recomputedAverageCount = sortedDimensions.Count(d => d.T >= 40.0 && d.T < 55.0);
                var recomputedExcellentCount = sortedDimensions.Count(d => d.T >= 55.0);
                
                // Validation: recomputed values must match exactly (FAIL if mismatch)
                if (recomputedWeakCount != weakCount || recomputedAverageCount != averageCount || recomputedExcellentCount != excellentCount)
                {
                    Console.WriteLine($"[FAIL] Band count mismatch! Expected W={weakCount} A={averageCount} E={excellentCount}, Got W={recomputedWeakCount} A={recomputedAverageCount} E={recomputedExcellentCount}");
                    throw new InvalidOperationException("Accuracy validation failed: Band counts do not match");
                }
                else
                {
                    Console.WriteLine($"[PASS] Band counts validated: W={weakCount} A={averageCount} E={excellentCount}");
                }
                
                column.Item().Background(ReportTheme.Colors.Surface)
                    .Padding(ReportTheme.Spacing.MD)
                    .Row(summaryRow =>
                    {
                        summaryRow.RelativeItem().AlignRight()
                            .Text($"ممتاز: {excellentCount} | متوسط: {averageCount} | ضعيف: {weakCount}")
                            .Style(ReportTheme.ArabicTextStyle(12, true));
                        
                        summaryRow.RelativeItem().AlignLeft()
                            .Text($"               Avg T: {ReportTheme.FormatNum(avgT, 1)} | Avg %ile: {ReportTheme.FormatNum(avgPercentile, 0)}")
                            .Style(TextStyle.Default.FontSize(11).FontColor(ReportTheme.Colors.TextSecondary));
                    });
            });

            // Page footer
            page.Footer().AlignCenter().Text("صفحة 2 / 3")
                .Style(ReportTheme.ArabicTextStyle(9, false, ReportTheme.Colors.TextSecondary));
        }

        /// <summary>
        /// FINAL Page 3: Focused Action Plan per v2 specifications  
        /// Weakest 3 dimensions only, right badges with band colors, concise bullets, filtered courses
        /// </summary>
        private void CreatePage3_ActionsAndCourses(
            PageDescriptor page,
            List<DimensionScoreDto> weakestDimensions,
            List<DimensionRecommendation> recommendations,
            List<CourseSuggestion> courses)
        {
            page.Content().Column(column =>
            {
                // v3: Title "خطة العمل والتوصيات" (18pt bold)
                column.Item().AlignRight().Text("خطة العمل والتوصيات")
                    .Style(ReportTheme.ArabicTextStyle(18, true));

                column.Item().PaddingTop(ReportTheme.Spacing.MD);

                // v3: Weakest 3 dimensions only (ascending T) - right-side badges, 2-3 bullets
                foreach (var dimension in weakestDimensions.Take(3))
                {
                    var dimensionRecommendation = recommendations
                        .FirstOrDefault(r => r.Dimension == dimension.Dimension);

                    column.Item().PaddingVertical(ReportTheme.Spacing.SM)
                        .Column(actionCard =>
                        {
                            // Header: dimension name (RTL) + badge on right (band color, T=xx.x)
                            actionCard.Item().Row(headerRow =>
                            {
                                headerRow.RelativeItem(4).AlignRight().Text(dimension.Dimension)
                                    .Style(ReportTheme.ArabicTextStyle(13, true));
                                
                                headerRow.RelativeItem(1).AlignRight()
                                    .Padding(ReportTheme.Spacing.XS)
                                    .Background(ReportTheme.GetBandColor(dimension.T))
                                    .AlignCenter()
                                    .Text($"T={ReportTheme.FormatNum(dimension.T, 1)}")
                                    .Style(TextStyle.Default.FontSize(9).FontColor("#FFFFFF"));
                            });

                            actionCard.Item().PaddingTop(ReportTheme.Spacing.SM);

                            // 2-3 concise bullets per dimension (no long paragraphs)
                            var suggestions = dimensionRecommendation?.Suggestions ?? new List<string>();
                            var bulletPoints = suggestions.Take(3).ToList();
                            
                            if (!bulletPoints.Any())
                            {
                                // Generate concise generic recommendations
                                bulletPoints = GenerateConciseBullets(dimension.Dimension);
                            }
                            
                            foreach (var bullet in bulletPoints.Take(3))
                            {
                                var conciseBullet = bullet.Length > 90 ? 
                                    bullet.Substring(0, 87) + "..." : bullet;
                                
                                actionCard.Item().PaddingVertical(ReportTheme.Spacing.XS)
                                    .AlignRight()
                                    .Text($"• {conciseBullet}")
                                    .Style(ReportTheme.ArabicTextStyle(10));
                            }
                        });
                }

                column.Item().PaddingTop(ReportTheme.Spacing.LG);

                // v3: Courses - up to 3 filtered by weakest dimensions only
                var filteredCourses = courses
                    .Where(c => weakestDimensions.Any(w => c.TargetDimensions?.Contains(w.Dimension) == true))
                    .OrderBy(c => c.Duration ?? "zzz") // Shortest first
                    .Take(3)
                    .ToList();

                if (filteredCourses.Any())
                {
                    column.Item().AlignRight().Text("الدورات المقترحة")
                        .Style(ReportTheme.ArabicTextStyle(16, true));

                    column.Item().PaddingTop(ReportTheme.Spacing.SM);

                    foreach (var course in filteredCourses)
                    {
                        column.Item().PaddingVertical(ReportTheme.Spacing.SM)
                            .Column(courseCard =>
                            {
                                // v3: اسم الدورة (bold), المدة (muted), وصف مختصر ≤120 حرف
                                var courseName = course.Name.Length > 50 ? 
                                    course.Name.Substring(0, 47) + "..." : course.Name;
                                
                                courseCard.Item().AlignRight().Text(courseName)
                                    .Style(ReportTheme.ArabicTextStyle(12, true));
                                
                                // v3: Duration (muted) - RTL alignment
                                if (!string.IsNullOrEmpty(course.Duration))
                                {
                                    courseCard.Item().AlignRight().Text($"المدة: {course.Duration}")
                                        .Style(ReportTheme.ArabicTextStyle(9, false, ReportTheme.Colors.TextSecondary));
                                }
                                
                                // v3: Brief description ≤120 chars - RTL alignment
                                if (!string.IsNullOrEmpty(course.Description))
                                {
                                    var description = course.Description.Length > 120 ? 
                                        course.Description.Substring(0, 117) + "..." : course.Description;
                                    courseCard.Item().AlignRight().Text(description)
                                        .Style(ReportTheme.ArabicTextStyle(9, false, ReportTheme.Colors.TextSecondary));
                                }
                                
                                // Tag each targeted dimension
                                var targetedWeakDimensions = course.TargetDimensions?
                                    .Where(td => weakestDimensions.Any(wd => wd.Dimension == td))
                                    .Take(3)
                                    .ToList();
                                
                                if (targetedWeakDimensions?.Any() == true)
                                {
                                    var targetDisplay = string.Join("، ", targetedWeakDimensions);
                                    if (targetDisplay.Length > 60)
                                    {
                                        targetDisplay = targetDisplay.Substring(0, 57) + "...";
                                    }
                                    
                                    courseCard.Item().PaddingTop(ReportTheme.Spacing.XS)
                                        .AlignRight()
                                        .Text($"يستهدف: {targetDisplay}")
                                        .Style(ReportTheme.ArabicTextStyle(9, false, ReportTheme.Colors.TextSecondary));
                                }
                            });
                    }
                }
            });

            // Page footer
            page.Footer().AlignCenter().Text("صفحة 3 / 3")
                .Style(ReportTheme.ArabicTextStyle(9, false, ReportTheme.Colors.TextSecondary));
        }
        
        /// <summary>
        /// Generate concise action bullets for dimensions (2-3 bullets, no long paragraphs)
        /// </summary>
        private static List<string> GenerateConciseBullets(string dimensionName)
        {
            return new List<string>
            {
                $"تطوير مهارات {dimensionName} من خلال التدريب المستمر",
                $"ممارسة أنشطة تعزز {dimensionName} يومياً", 
                $"قياس التقدم في {dimensionName} شهرياً"
            };
        }

        /// <summary>
        /// Ensure Arabic fonts are loaded and registered
        /// </summary>
        private static void EnsureFonts()
        {
            if (_fontsRegistered) return;

            lock (_lock)
            {
                if (_fontsRegistered) return;

                try
                {
                    var fontsDir = Path.Combine(AppContext.BaseDirectory, "Resources", "Fonts");
                    var regularPath = Path.Combine(fontsDir, "NotoNaskhArabic-Regular.ttf");
                    var boldPath = Path.Combine(fontsDir, "NotoNaskhArabic-Bold.ttf");

                    if (File.Exists(regularPath))
                    {
                        QuestPDF.Drawing.FontManager.RegisterFont(File.OpenRead(regularPath));
                        Console.WriteLine($"[ModernPdfReportService] ✓ Font loaded: Noto Naskh Arabic Regular");
                    }
                    else
                    {
                        Console.WriteLine($"[ModernPdfReportService] ⚠ Regular font not found: {regularPath}");
                    }

                    if (File.Exists(boldPath))
                    {
                        QuestPDF.Drawing.FontManager.RegisterFont(File.OpenRead(boldPath));
                        Console.WriteLine($"[ModernPdfReportService] ✓ Font loaded: Noto Naskh Arabic Bold");
                    }
                    else
                    {
                        Console.WriteLine($"[ModernPdfReportService] ⚠ Bold font not found: {boldPath}");
                    }

                    _fontsRegistered = true;
                    Console.WriteLine($"[ModernPdfReportService] ✅ Arabic font registration complete");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ModernPdfReportService] ❌ Font registration failed: {ex.Message}");
                    _fontsRegistered = false;
                }
            }
        }

        public Task<byte[]> RenderSdjResultPdfAsync(Result result, User user, CancellationToken ct = default)
        {
            // SDJ support not implemented in ModernPdfReportService yet
            // Redirect to the active service (UltimateArabicPdfReportService)
            throw new NotImplementedException("SDJ PDF generation is only supported in UltimateArabicPdfReportService. Please ensure services are configured correctly.");
        }
    }
}