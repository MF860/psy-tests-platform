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
    /// Modern SDJ PDF Report Service with 7-Pattern Analysis
    /// Arabic-only, RTL, HarfBuzz shaping, clean design
    /// Features: centered logo, 7 major patterns, heptagon radar chart, weak subdimensions + courses
    /// </summary>
    public class ModernSdjSevenPatternReportService
    {
        private static bool _fontsRegistered;
        private static readonly object _lock = new();
        private static byte[]? _logoBytes;

        public ModernSdjSevenPatternReportService()
        {
            EnsureFonts();
            LoadLogo();
        }

        /// <summary>
        /// Renders the enhanced SDJ PDF report with 7-pattern analysis
        /// </summary>
        public Task<byte[]> RenderSdjSevenPatternPdfAsync(
            Result result, 
            User user, 
            SdjScoreSummary sdjData,
            CancellationToken ct = default)
        {
            var startTime = DateTime.Now;

            try
            {
                Console.WriteLine($"\n{'═',70}");
                Console.WriteLine($"🎯 SDJ SEVEN-PATTERN REPORT GENERATION");
                Console.WriteLine($"{'═',70}");
                Console.WriteLine($"👤 User: {user.FullName ?? "غير محدد"} (ID: {user.NationalId ?? "N/A"})");
                Console.WriteLine($"📋 Session: {result.SessionId}");
                Console.WriteLine($"🕐 Time: {startTime:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"📊 Version: {sdjData.Version}");

                Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("ar-JO");
                QuestPDF.Settings.License = LicenseType.Community;
                QuestPDF.Settings.EnableDebugging = false;

                // Validate data
                if (sdjData.SevenPatternScores == null || sdjData.SevenPatternScores.Count == 0)
                    throw new InvalidOperationException("No 7-pattern scores found");

                var patterns = sdjData.SevenPatternScores.OrderBy(p => p.PatternKey).ToList();
                var weakSubDimensions = sdjData.SubDimensions.Where(s => s.T < 40).OrderBy(s => s.T).ToList();
                
                Console.WriteLine($"   7 Patterns: {patterns.Count}");
                Console.WriteLine($"   Weak Sub-dimensions (<40): {weakSubDimensions.Count}");
                Console.WriteLine($"   Total Sub-dimensions: {sdjData.SubDimensions.Count}");

                // Generate course recommendations
                var courseRecommendations = CourseRecommendationsMapper
                    .GetRecommendationsForWeakSubDimensions(sdjData.SubDimensions);

                Console.WriteLine($"   Course Recommendations: {courseRecommendations.Count}");

                // Generate PDF
                var pdf = Document.Create(document =>
                {
                    // Page 1: Cover with STEST logo, participant info, professional intro
                    document.Page(page =>
                    {
                        ConfigurePageDefaults(page);
                        ComposePage1_CoverAndIntro(page, user, result);
                    });

                    // Page 2: Visual analytics - charts (subdimensions bar + 7-pattern radar)
                    document.Page(page =>
                    {
                        ConfigurePageDefaults(page);
                        ComposePage2_ChartsAndVisualizations(page, patterns, sdjData.SubDimensions);
                    });

                    // Page 3: Seven Tracks Summary (NEW - as per requirements)
                    document.Page(page =>
                    {
                        ConfigurePageDefaults(page);
                        ComposePage3_SevenTracksSummary(page, patterns, sdjData.SubDimensions);
                    });

                    // Page 4: Detailed track analyses with courses (restructured)
                    document.Page(page =>
                    {
                        ConfigurePageDefaults(page);
                        ComposePage4_DetailedTrackAnalysesAndCourses(page, patterns, sdjData.SubDimensions, courseRecommendations);
                    });
                });

                var pdfBytes = pdf.GeneratePdf();
                var endTime = DateTime.Now;
                var duration = endTime - startTime;

                Console.WriteLine($"\n✅ SDJ 7-PATTERN PDF COMPLETE");
                Console.WriteLine($"   Pages: 4 (Cover + Charts + Seven Tracks + Courses)");
                Console.WriteLine($"   Size: {pdfBytes.Length / 1024:F1} KB");
                Console.WriteLine($"   Duration: {duration.TotalMilliseconds:F0}ms");
                Console.WriteLine($"{'═',70}\n");

                return Task.FromResult(pdfBytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ SDJ 7-PATTERN PDF GENERATION FAILED: {ex.Message}");
                Console.WriteLine($"   Stack: {ex.StackTrace}");
                throw;
            }
        }

        #region Page Composition

        /// <summary>
        /// Page 1: Professional cover with STEST logo, Arabic title, participant info, and professional intro
        /// </summary>
        private void ComposePage1_CoverAndIntro(
            PageDescriptor page,
            User user,
            Result result)
        {
            page.Content().Column(column =>
            {
                // CENTERED LOGO AT TOP (STEST.PNG - high DPI, professional placement)
                if (_logoBytes != null && _logoBytes.Length > 0)
                {
                    column.Item()
                        .AlignCenter()
                        .PaddingBottom(12)
                        .Width(100)
                        .Image(_logoBytes);
                }

                // MAIN TITLE - as specified: "التقرير النفسي الشامل — نتائج القياس والتحليل"
                column.Item().AlignCenter()
                    .Text("التقرير النفسي الشامل — نتائج القياس والتحليل")
                    .Style(ReportTheme.ArabicTextStyle(22, true, "#1e40af"));

                column.Item().PaddingTop(24);

                // PARTICIPANT INFO BLOCK - centered title, RTL table layout
                column.Item()
                    .Background("#f9fafb")
                    .Border(1)
                    .BorderColor("#e5e7eb")
                    .Padding(16)
                    .Column(info =>
                    {
                        // Centered block title
                        info.Item().AlignCenter().Text("بيانات المشارك")
                            .Style(ReportTheme.ArabicTextStyle(16, true, "#111827"));
                        
                        info.Item().PaddingTop(12);
                        
                        // RTL table-style layout with labels on right, values on left
                        info.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(2); // Label column (right)
                                cols.RelativeColumn(3); // Value column (left)
                            });

                            // Row 1: Full Name
                            table.Cell().AlignRight().PaddingVertical(4).Text("الاسم الكامل:")
                                .Style(ReportTheme.ArabicTextStyle(12, true, "#6b7280"));
                            table.Cell().AlignRight().PaddingVertical(4).Text(user.FullName ?? "غير محدد")
                                .Style(ReportTheme.ArabicTextStyle(12, false, "#111827"));

                            // Row 2: National ID
                            table.Cell().AlignRight().PaddingVertical(4).Text("الرقم الوطني:")
                                .Style(ReportTheme.ArabicTextStyle(12, true, "#6b7280"));
                            table.Cell().AlignRight().PaddingVertical(4).Text(user.NationalId ?? "N/A")
                                .Style(ReportTheme.ArabicTextStyle(12, false, "#111827"));

                            // Row 3: Session ID
                            table.Cell().AlignRight().PaddingVertical(4).Text("رقم الجلسة:")
                                .Style(ReportTheme.ArabicTextStyle(12, true, "#6b7280"));
                            var sessionDisplay = result.SessionId.ToString();
                            if (sessionDisplay.Length > 15) sessionDisplay = sessionDisplay[..15] + "...";
                            table.Cell().AlignRight().PaddingVertical(4).Text(sessionDisplay)
                                .Style(ReportTheme.ArabicTextStyle(12, false, "#111827"));

                            // Row 4: Date/Time
                            table.Cell().AlignRight().PaddingVertical(4).Text("التاريخ/الوقت:")
                                .Style(ReportTheme.ArabicTextStyle(12, true, "#6b7280"));
                            table.Cell().AlignRight().PaddingVertical(4).Text(result.CreatedAt.ToString("dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture))
                                .Style(ReportTheme.ArabicTextStyle(12, false, "#111827"));
                        });
                    });

                column.Item().PaddingTop(20);

                // PROFESSIONAL INTRO PARAGRAPH - as specified
                column.Item().AlignRight()
                    .Padding(12)
                    .Background("#eef2ff")
                    .Border(0.5f).BorderColor("#c7d2fe")
                    .Column(intro =>
                    {
                        intro.Item().Text(
                            "يُقدم هذا التقرير تحليلاً شاملاً لسماتك النفسية وقدراتك المهنية بناءً على إطار التنمية المستدامة SDJ. " +
                            "تم تقييم أدائك عبر سبعة أنماط رئيسية و24 بُعداً فرعياً، وتمثل الدرجات كنسب معيارية T-Scores حيث " +
                            "50 هي المتوسط، وكلما ارتفعت الدرجة دل ذلك على قوة أكبر في البُعد المقاس. يتضمن التقرير توصيات تطويرية " +
                            "ودورات تدريبية مخصصة لتعزيز جوانب القوة ومعالجة مجالات التحسين."
                        ).Style(ReportTheme.ArabicTextStyle(11, false, "#1e3a8a")).LineHeight(1.6f);
                    });
            });

            // Footer
            page.Footer().AlignCenter().Text("صفحة 1")
                .Style(ReportTheme.ArabicTextStyle(9, false, "#9ca3af"));
        }

        /// <summary>
        /// Page 2: Visual analytics - horizontal bar chart (subdimensions) + radar chart (7 patterns)
        /// </summary>
        private void ComposePage2_ChartsAndVisualizations(
            PageDescriptor page,
            List<SevenPatternScore> patterns,
            List<SdjSubDimensionScore> subDimensions)
        {
            page.Content().Column(column =>
            {
                // MAIN SECTION TITLE
                column.Item().AlignCenter().Text("التحليل البصري للأنماط")
                    .Style(ReportTheme.ArabicTextStyle(20, true, "#111827"));

                column.Item().PaddingTop(12);

                // ✨ KPI CARDS: Average T-Score, Variance, Top/Bottom Dimensions
                var avgTScore = patterns.Average(p => p.TScore);
                var variance = Math.Round(patterns.Select(p => p.TScore).Max() - patterns.Select(p => p.TScore).Min(), 1);
                var topDim = subDimensions.OrderByDescending(s => s.T).First();
                var bottomDim = subDimensions.OrderBy(s => s.T).First();

                column.Item().Row(row =>
                {
                    row.RelativeItem().Kpi(ReportTheme.FormatNum(avgTScore, 1), "متوسط T-Score", ReportTheme.GetBandColor(avgTScore));
                    row.RelativeItem().PaddingHorizontal(8);
                    row.RelativeItem().Kpi(ReportTheme.FormatNum(variance, 1), "التباين", ReportTheme.Colors.Info);
                    row.RelativeItem().PaddingHorizontal(8);
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Badge("أقوى: " + topDim.SubDimension, ReportTheme.Colors.Success, "#FFFFFF", 10f);
                        col.Item().PaddingTop(4);
                        col.Item().Badge("أضعف: " + bottomDim.SubDimension, ReportTheme.Colors.Warning, "#FFFFFF", 10f);
                    });
                });

                column.Item().PaddingTop(16);

                // SUB-SECTION 1: Horizontal Bar Chart for Subdimensions (restored as requested)
                column.Item().AlignRight().Text("توزيع الدرجات التفصيلي عبر الأبعاد الفرعية - T-Score")
                    .Style(ReportTheme.ArabicTextStyle(14, true, "#374151"));

                column.Item().PaddingTop(8);

                try
                {
                    // Sort subdimensions ascending by T-score (weakest first, strongest last)
                    var sortedSubDims = subDimensions
                        .OrderBy(s => s.T)
                        .Take(18) // Max 18 for readability on one page
                        .ToList();

                    // Convert to DimensionScore format for the renderer
                    var dimensionScores = sortedSubDims.Select(s => new DimensionScore
                    {
                        Dimension = s.SubDimension,
                        T = s.T,
                        Percentile = s.Percentile
                    }).ToList();

                    var barChartBytes = HorizontalBarChartRenderer.RenderHorizontalBars(
                        dimensionScores,
                        width: 540,
                        maxDimensions: 18);
                    
                    column.Item().AlignCenter().Image(barChartBytes);
                    Console.WriteLine($"   ✓ Horizontal subdimension bar chart rendered ({sortedSubDims.Count} dimensions)");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ⚠ Bar chart rendering failed: {ex.Message}");
                    Console.WriteLine($"   Stack: {ex.StackTrace}");
                    column.Item().AlignCenter().PaddingVertical(20)
                        .Text($"[خطأ في رسم المخطط الشريطي: {ex.Message}]")
                        .Style(TextStyle.Default.FontSize(10).FontColor("#ef4444"));
                }

                column.Item().PaddingTop(24);

                // SUB-SECTION 2: Heptagon Radar Chart for 7 Patterns (fixed Arabic labels with HarfBuzz)
                column.Item().AlignRight().Text("التحليل البصري للأنماط الرئيسية")
                    .Style(ReportTheme.ArabicTextStyle(14, true, "#374151"));

                column.Item().PaddingTop(8).AlignCenter()
                    .Text("الرسم البياني السباعي يُظهر توزيع الأنماط السبعة الرئيسية")
                    .Style(ReportTheme.ArabicTextStyle(9, false, "#6b7280"));

                column.Item().PaddingTop(12);

                try
                {
                    var heptagonBytes = HeptagonRadarChartRenderer.RenderHeptagonChart(
                        patterns, 
                        size: 420, 
                        title: "الخريطة النفسية السباعية");
                    
                    column.Item().AlignCenter().Height(420).Image(heptagonBytes);
                    Console.WriteLine("   ✓ Heptagon radar chart rendered with Arabic labels (HarfBuzz enabled)");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ⚠ Heptagon chart rendering failed: {ex.Message}");
                    column.Item().AlignCenter().PaddingVertical(20)
                        .Text("[خطأ في رسم المخطط السباعي]")
                        .Style(TextStyle.Default.FontSize(11).FontColor("#ef4444"));
                }
            });

            // Footer
            page.Footer().AlignCenter().Text("صفحة 2")
                .Style(ReportTheme.ArabicTextStyle(9, false, "#9ca3af"));
        }

        /// <summary>
        /// Page 3: Seven Tracks Summary with statistical insights and detailed analysis
        /// Shows all 7 SDJ tracks with T-scores, bands, interpretations, and modern analytics
        /// </summary>
        private void ComposePage3_SevenTracksSummary(
            PageDescriptor page,
            List<SevenPatternScore> patterns,
            List<SdjSubDimensionScore> subDimensions)
        {
            page.Content().Column(column =>
            {
                // MAIN TITLE
                column.Item().AlignCenter().Text("ملخص الأنماط الرئيسية")
                    .Style(ReportTheme.ArabicTextStyle(20, true, "#111827"));

                column.Item().PaddingTop(6).AlignCenter()
                    .Text("(تحليل متقدم للأنماط السبعة مع رؤى إحصائية وتفسيرات احترافية)")
                    .Style(ReportTheme.ArabicTextStyle(9, false, "#6b7280"));

                column.Item().PaddingTop(12);

                // STATISTICAL OVERVIEW PANEL (Modern Analytics)
                var avgTScore = patterns.Average(p => p.TScore);
                var maxTScore = patterns.Max(p => p.TScore);
                var minTScore = patterns.Min(p => p.TScore);
                var excellentCount = patterns.Count(p => p.TScore >= 55);
                var averageCount = patterns.Count(p => p.TScore >= 45 && p.TScore < 55);
                var needsDevelopmentCount = patterns.Count(p => p.TScore < 45);

                column.Item()
                    .Background("#f0f9ff")
                    .Border(1).BorderColor("#bae6fd")
                    .Padding(12)
                    .Column(statsPanel =>
                    {
                        statsPanel.Item().Row(statsRow =>
                        {
                            statsRow.RelativeItem().AlignCenter().Column(stat =>
                            {
                                stat.Item().Text(ReportTheme.FormatNum(avgTScore, 1))
                                    .Style(TextStyle.Default.FontSize(18).FontColor("#0369a1").Bold());
                                stat.Item().Text("المتوسط العام")
                                    .Style(ReportTheme.ArabicTextStyle(9, false, "#6b7280"));
                            });

                            statsRow.RelativeItem().AlignCenter().Column(stat =>
                            {
                                stat.Item().Text($"{excellentCount} / {patterns.Count}")
                                    .Style(TextStyle.Default.FontSize(18).FontColor("#10b981").Bold());
                                stat.Item().Text("أنماط متقدمة")
                                    .Style(ReportTheme.ArabicTextStyle(9, false, "#6b7280"));
                            });

                            statsRow.RelativeItem().AlignCenter().Column(stat =>
                            {
                                stat.Item().Text(ReportTheme.FormatNum(maxTScore - minTScore, 1))
                                    .Style(TextStyle.Default.FontSize(18).FontColor("#f59e0b").Bold());
                                stat.Item().Text("التفاوت")
                                    .Style(ReportTheme.ArabicTextStyle(9, false, "#6b7280"));
                            });

                            statsRow.RelativeItem().AlignCenter().Column(stat =>
                            {
                                var balanceScore = CalculateBalanceScore(patterns);
                                stat.Item().Text($"{balanceScore}%")
                                    .Style(TextStyle.Default.FontSize(18).FontColor("#8b5cf6").Bold());
                                stat.Item().Text("التوازن النفسي")
                                    .Style(ReportTheme.ArabicTextStyle(9, false, "#6b7280"));
                            });
                        });
                    });

                column.Item().PaddingTop(14);

                // Sort patterns by T-score descending (best first)
                var sortedPatterns = patterns.OrderByDescending(p => p.TScore).ToList();

                // Display all 7 tracks with enhanced interpretations
                foreach (var pattern in sortedPatterns)
                {
                    column.Item().PaddingBottom(12)
                        .Border(1)
                        .BorderColor("#e5e7eb")
                        .Background(GetPatternBackgroundColor(pattern.TScore))
                        .Padding(12)
                        .Column(trackCard =>
                        {
                            // Track header: Name + T-score badge + Band + Percentile
                            trackCard.Item().Row(header =>
                            {
                                header.RelativeItem(4).Column(nameCol =>
                                {
                                    nameCol.Item().Text(pattern.PatternNameAr)
                                        .Style(ReportTheme.ArabicTextStyle(13, true, "#111827"));
                                    nameCol.Item().PaddingTop(2).Text(pattern.PatternNameEn)
                                        .Style(TextStyle.Default.FontSize(8).FontColor("#9ca3af").Italic());
                                });
                                
                                header.AutoItem().PaddingHorizontal(8).Column(scoreCol =>
                                {
                                    scoreCol.Item().AlignCenter()
                                        .Background(ReportTheme.GetBandColor(pattern.TScore))
                                        .PaddingVertical(3)
                                        .PaddingHorizontal(8)
                                        .Text($"T = {ReportTheme.FormatNum(pattern.TScore, 1)}")
                                        .Style(TextStyle.Default.FontSize(11).FontColor("#ffffff").Bold());
                                    
                                    scoreCol.Item().PaddingTop(2).AlignCenter()
                                        .Text($"المئين: {ReportTheme.FormatNum(pattern.Percentile * 100, 0)}%")
                                        .Style(TextStyle.Default.FontSize(7).FontColor("#6b7280"));
                                });
                                
                                var bandLabel = GetBandLabel(pattern.TScore);
                                header.AutoItem().AlignMiddle().PaddingLeft(6)
                                    .Background(GetBandLabelBg(pattern.TScore))
                                    .PaddingVertical(4)
                                    .PaddingHorizontal(8)
                                    .Text(bandLabel)
                                    .Style(ReportTheme.ArabicTextStyle(10, true, GetBandColor(pattern.TScore)));
                            });

                            // Enhanced interpretation with professional insights
                            trackCard.Item().PaddingTop(8).Text(GetEnhancedTrackInterpretation(pattern, subDimensions))
                                .Style(ReportTheme.ArabicTextStyle(10, false, "#374151")).LineHeight(1.6f);

                            // Key competencies from this pattern
                            if (pattern.SubDimensions.Any())
                            {
                                var topSubDims = pattern.SubDimensions.OrderByDescending(s => s.T).Take(3).ToList();
                                trackCard.Item().PaddingTop(6).Row(compRow =>
                                {
                                    compRow.AutoItem().Text("• الكفاءات الرئيسية: ")
                                        .Style(ReportTheme.ArabicTextStyle(9, true, "#6b7280"));
                                    compRow.RelativeItem().Text(string.Join(" • ", topSubDims.Select(s => s.SubDimension)))
                                        .Style(ReportTheme.ArabicTextStyle(9, false, "#4b5563"));
                                });
                            }

                            // Professional implications
                            trackCard.Item().PaddingTop(4).Row(implRow =>
                            {
                                implRow.AutoItem().Text("• التطبيق المهني: ")
                                    .Style(ReportTheme.ArabicTextStyle(9, true, "#6b7280"));
                                implRow.RelativeItem().Text(GetProfessionalImplication(pattern))
                                    .Style(ReportTheme.ArabicTextStyle(9, false, "#4b5563"));
                            });
                        });
                }
            });

            // Footer
            page.Footer().AlignCenter().Text("صفحة 3")
                .Style(ReportTheme.ArabicTextStyle(9, false, "#9ca3af"));
        }

        /// <summary>
        /// Page 4: Detailed track analyses with comprehensive course recommendations and development roadmap
        /// Title: "الدورات المقترحة لكل مسار" (no mention of "weak")
        /// </summary>
        private void ComposePage4_DetailedTrackAnalysesAndCourses(
            PageDescriptor page,
            List<SevenPatternScore> patterns,
            List<SdjSubDimensionScore> subDimensions,
            List<SubDimensionCourseRecommendation> courseRecommendations)
        {
            page.Content().Column(column =>
            {
                // MAIN TITLE - restructured as per requirements
                column.Item().AlignCenter().Text("خطة التطوير المهني المخصصة")
                    .Style(ReportTheme.ArabicTextStyle(20, true, "#111827"));

                column.Item().PaddingTop(6).AlignCenter()
                    .Text("(توصيات تفصيلية ودورات تدريبية مُستهدفة لكل نمط من الأنماط السبعة)")
                    .Style(ReportTheme.ArabicTextStyle(9, false, "#6b7280"));

                column.Item().PaddingTop(12);

                // DEVELOPMENT PRIORITY MATRIX (Modern visual guide)
                column.Item()
                    .Background("#fef3c7")
                    .Border(1).BorderColor("#fde047")
                    .Padding(10)
                    .Row(priorityRow =>
                    {
                        priorityRow.RelativeItem().AlignRight().Column(priorityText =>
                        {
                            priorityText.Item().Text("🎯 أولويات التطوير:")
                                .Style(ReportTheme.ArabicTextStyle(11, true, "#92400e"));
                            priorityText.Item().PaddingTop(4).Text(GetDevelopmentPriorities(patterns))
                                .Style(ReportTheme.ArabicTextStyle(9, false, "#78350f"))
                                .LineHeight(1.5f);
                        });
                    });

                column.Item().PaddingTop(14);

                // Organize by tracks - show patterns that need most attention first
                var sortedPatterns = patterns.OrderBy(p => p.TScore).ToList(); // Weakest first for focused development
                
                // Show all 7 tracks with varying detail levels
                foreach (var pattern in sortedPatterns)
                {
                    var needsAttention = pattern.TScore < 50;
                    
                    column.Item().PaddingBottom(14)
                        .Border(1.5f)
                        .BorderColor(needsAttention ? "#f59e0b" : "#d1d5db")
                        .Background(needsAttention ? "#fffbeb" : "#ffffff")
                        .Padding(12)
                        .Column(trackSection =>
                        {
                            // Track heading with priority indicator
                            trackSection.Item()
                                .Background(needsAttention ? "#fef3c7" : "#f3f4f6")
                                .Padding(10)
                                .Row(header =>
                                {
                                    if (needsAttention)
                                    {
                                        header.AutoItem().Width(24).Height(24)
                                            .Background("#f59e0b")
                                            .AlignMiddle().AlignCenter()
                                            .Text("⚠")
                                            .Style(TextStyle.Default.FontSize(14).FontColor("#ffffff"));
                                        header.AutoItem().Width(8);
                                    }
                                    
                                    header.RelativeItem().AlignMiddle().Text(pattern.PatternNameAr)
                                        .Style(ReportTheme.ArabicTextStyle(13, true, "#1f2937"));
                                    
                                    header.AutoItem().AlignMiddle().PaddingHorizontal(8)
                                        .Background(ReportTheme.GetBandColor(pattern.TScore))
                                        .PaddingVertical(3).PaddingHorizontal(8)
                                        .Text($"T = {ReportTheme.FormatNum(pattern.TScore, 1)}")
                                        .Style(TextStyle.Default.FontSize(10).FontColor("#ffffff").Bold());
                                    
                                    header.AutoItem().AlignMiddle().Text(GetDevelopmentPriority(pattern.TScore))
                                        .Style(ReportTheme.ArabicTextStyle(10, true, 
                                            needsAttention ? "#92400e" : "#6b7280"));
                                });

                            // Subdimensions detailed table with this track
                            var trackSubDims = pattern.SubDimensions.OrderBy(s => s.T).ToList();
                            if (trackSubDims.Any())
                            {
                                trackSection.Item().PaddingTop(10).Column(subDimSection =>
                                {
                                    subDimSection.Item().Text("الأبعاد الفرعية:")
                                        .Style(ReportTheme.ArabicTextStyle(10, true, "#374151"));
                                    
                                    subDimSection.Item().PaddingTop(6).Table(table =>
                                    {
                                        table.ColumnsDefinition(cols =>
                                        {
                                            cols.RelativeColumn(3); // Name
                                            cols.RelativeColumn(1); // T-Score
                                            cols.RelativeColumn(2); // Assessment
                                        });

                                        // Header
                                        table.Header(h =>
                                        {
                                            h.Cell().Background("#f3f4f6").Padding(4).Text("البُعد")
                                                .Style(ReportTheme.ArabicTextStyle(9, true, "#6b7280"));
                                            h.Cell().Background("#f3f4f6").Padding(4).Text("الدرجة")
                                                .Style(ReportTheme.ArabicTextStyle(9, true, "#6b7280"));
                                            h.Cell().Background("#f3f4f6").Padding(4).Text("التقييم")
                                                .Style(ReportTheme.ArabicTextStyle(9, true, "#6b7280"));
                                        });

                                        // Show up to 4 subdimensions per track
                                        foreach (var subDim in trackSubDims.Take(4))
                                        {
                                            table.Cell().Padding(4).Text(subDim.SubDimension)
                                                .Style(ReportTheme.ArabicTextStyle(9, false, "#374151"));
                                            table.Cell().Padding(4).Text(ReportTheme.FormatNum(subDim.T, 1))
                                                .Style(TextStyle.Default.FontSize(9).FontColor("#6b7280"));
                                            table.Cell().Padding(4).Text(GetDetailedAssessment(subDim.T))
                                                .Style(ReportTheme.ArabicTextStyle(8, false, "#4b5563"));
                                        }
                                    });
                                });
                            }

                            // Course recommendations for this track
                            var trackCourses = courseRecommendations
                                .Where(c => pattern.SubDimensions.Any(s => s.SubDimension == c.SubDimensionAr && s.T < 50))
                                .SelectMany(c => c.RecommendedCourses)
                                .Distinct()
                                .Take(4)
                                .ToList();

                            if (trackCourses.Any())
                            {
                                trackSection.Item().PaddingTop(8).Column(courses =>
                                {
                                    courses.Item().Text("📚 الدورات التدريبية المُوصى بها:")
                                        .Style(ReportTheme.ArabicTextStyle(10, true, "#1e40af"));
                                    courses.Item().PaddingTop(4).Column(courseList =>
                                    {
                                        foreach (var course in trackCourses)
                                        {
                                            courseList.Item().PaddingVertical(2).Row(courseRow =>
                                            {
                                                courseRow.AutoItem().Width(16).Height(16)
                                                    .Background("#3b82f6")
                                                    .AlignMiddle().AlignCenter()
                                                    .Text("✓")
                                                    .Style(TextStyle.Default.FontSize(9).FontColor("#ffffff").Bold());
                                                courseRow.AutoItem().Width(6);
                                                courseRow.RelativeItem().AlignMiddle().Text(course)
                                                    .Style(ReportTheme.ArabicTextStyle(9, false, "#1e3a8a"));
                                            });
                                        }
                                    });
                                });
                            }

                            // Development timeline suggestion
                            if (pattern.TScore < 50)
                            {
                                trackSection.Item().PaddingTop(6)
                                    .Background("#dbeafe")
                                    .Padding(6)
                                    .Text($"⏱ الإطار الزمني المُقترح: {GetTimelineEstimate(pattern.TScore)}")
                                    .Style(ReportTheme.ArabicTextStyle(8, false, "#1e40af"));
                            }
                        });
                }

                column.Item().PaddingTop(16);

                // COMPREHENSIVE USER ANALYSIS SECTION
                column.Item().AlignCenter()
                    .Background("#f0f9ff")
                    .Border(1).BorderColor("#bfdbfe")
                    .Padding(14)
                    .Column(analysisSection =>
                    {
                        analysisSection.Item().AlignCenter().Text("التحليل الشامل لنتائجك")
                            .Style(ReportTheme.ArabicTextStyle(15, true, "#1e40af"));
                        
                        analysisSection.Item().PaddingTop(10).AlignRight()
                            .Text(GenerateComprehensiveUserAnalysis(patterns, subDimensions))
                            .Style(ReportTheme.ArabicTextStyle(10, false, "#1e3a8a"))
                            .LineHeight(1.7f);
                    });

                column.Item().PaddingTop(14);

                // CLOSING LINE - as specified
                column.Item().AlignCenter()
                    .Background("#eef2ff")
                    .Padding(12)
                    .Text("ابدأ رحلتك التدريبية المخصصة عبر منصة استدامة.")
                    .Style(ReportTheme.ArabicTextStyle(12, true, "#1e40af"));
            });

            // Footer
            page.Footer().AlignCenter().Text("صفحة 4")
                .Style(ReportTheme.ArabicTextStyle(9, false, "#9ca3af"));
        }

        /// <summary>
        /// Page 3 (OLD): Weak sub-dimensions table with course recommendations
        /// REPLACED by new structure above - keeping for reference during migration
        /// </summary>
        private void ComposePage3_WeakAreasAndCourses(
            PageDescriptor page,
            List<SubDimensionCourseRecommendation> courseRecommendations)
        {
            page.Content().Column(column =>
            {
                // Title
                column.Item().Text("الأبعاد الفرعية الضعيفة والدورات المقترحة")
                    .Style(ReportTheme.ArabicTextStyle(20, true, "#111827"));

                column.Item().PaddingTop(12).Text(
                    "يعرض هذا القسم الأبعاد التي تحتاج إلى تطوير (درجة T < 40) مع توصيات للدورات التدريبية المقترحة."
                ).Style(ReportTheme.ArabicTextStyle(11, false, "#6b7280")).LineHeight(1.5f);

                column.Item().PaddingTop(24);

                if (!courseRecommendations.Any())
                {
                    column.Item().AlignCenter()
                        .Background("#ecfdf5")
                        .Padding(20)
                        .Text("ممتاز! لا توجد أبعاد ضعيفة تحتاج إلى تطوير.")
                        .Style(ReportTheme.ArabicTextStyle(13, false, "#065f46"));
                }
                else
                {
                    // Table of weak subdimensions + courses
                    foreach (var recommendation in courseRecommendations.Take(10)) // Max 10 to fit on page
                    {
                        column.Item().PaddingBottom(16)
                            .Border(1)
                            .BorderColor("#e5e7eb")
                            .Padding(16)
                            .Column(item =>
                            {
                                // Header: Sub-dimension name + T-score badge
                                item.Item().Row(header =>
                                {
                                    header.RelativeItem().Text(recommendation.SubDimensionAr)
                                        .Style(ReportTheme.ArabicTextStyle(14, true, "#111827"));
                                    
                                    header.AutoItem().PaddingHorizontal(8)
                                        .Background(ReportTheme.GetBandColor(recommendation.TScore))
                                        .PaddingVertical(4)
                                        .PaddingHorizontal(6)
                                        .Text($"T={ReportTheme.FormatNum(recommendation.TScore, 1)}")
                                        .Style(TextStyle.Default.FontSize(10).FontColor("#ffffff"));
                                });

                                // Status description
                                item.Item().PaddingTop(8).Text(recommendation.Status)
                                    .Style(ReportTheme.ArabicTextStyle(11, false, "#6b7280"));

                                // Courses list
                                item.Item().PaddingTop(8).Text("الدورات المقترحة:")
                                    .Style(ReportTheme.ArabicTextStyle(12, true, "#374151"));

                                item.Item().PaddingTop(4).Column(courses =>
                                {
                                    foreach (var course in recommendation.RecommendedCourses.Take(3))
                                    {
                                        courses.Item().PaddingVertical(2).Row(courseRow =>
                                        {
                                            courseRow.AutoItem().Width(16).Text("•")
                                                .Style(TextStyle.Default.FontSize(11).FontColor("#2563eb"));
                                            courseRow.RelativeItem().Text(course)
                                                .Style(ReportTheme.ArabicTextStyle(10, false, "#4b5563"));
                                        });
                                    }
                                });
                            });
                    }
                }
            });

            // Footer
            page.Footer().AlignCenter().Text("صفحة 3 / 3")
                .Style(ReportTheme.ArabicTextStyle(9, false, "#9ca3af"));
        }

        #endregion

        #region Helper Methods

        private void ConfigurePageDefaults(PageDescriptor page)
        {
            page.Size(PageSizes.A4);
            page.Margin(16, Unit.Millimetre); // 16mm margins as specified
            page.DefaultTextStyle(style => ReportTheme.ArabicTextStyle(11, false, "#374151"));

            // Professional Header
            page.Header()
                .AlignRight()
                .Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("تقرير تحليل أنماط الشخصية السبعة - SDJ")
                            .Style(ReportTheme.ArabicTextStyle(10, true, ReportTheme.Colors.Primary));
                        col.Item().Text($"تاريخ التوليد: {DateTime.Now:yyyy-MM-dd HH:mm}")
                            .Style(ReportTheme.ArabicTextStyle(8, false, ReportTheme.Colors.TextSecondary));
                    });
                });

            // Professional Footer with Page Numbers (RTL)
            page.Footer()
                .AlignCenter()
                .Text(text =>
                {
                    text.Span("صفحة ").Style(ReportTheme.ArabicTextStyle(9, false, ReportTheme.Colors.TextSecondary));
                    text.CurrentPageNumber().Style(ReportTheme.ArabicTextStyle(9, true, ReportTheme.Colors.Primary));
                    text.Span(" من ").Style(ReportTheme.ArabicTextStyle(9, false, ReportTheme.Colors.TextSecondary));
                    text.TotalPages().Style(ReportTheme.ArabicTextStyle(9, true, ReportTheme.Colors.Primary));
                });
        }

        /// <summary>
        /// Get Arabic band label based on T-score
        /// </summary>
        private string GetBandLabel(double tScore)
        {
            if (tScore >= 55) return "متقدّم";
            if (tScore >= 45) return "متوسط";
            return "بحاجة للتنمية";
        }

        /// <summary>
        /// Get band color based on T-score
        /// </summary>
        private string GetBandColor(double tScore)
        {
            if (tScore >= 55) return "#10b981"; // Green
            if (tScore >= 40) return "#f59e0b"; // Orange
            return "#ef4444"; // Red
        }

        /// <summary>
        /// Generate track interpretation based on pattern score and subdimensions (data-driven, no AI)
        /// </summary>
        private string GetTrackInterpretation(SevenPatternScore pattern, List<SdjSubDimensionScore> allSubDimensions)
        {
            var trackSubDims = pattern.SubDimensions.OrderByDescending(s => s.T).ToList();
            var avgT = pattern.TScore;
            
            // Get top strengths and weaknesses
            var topStrengths = trackSubDims.Take(2).Select(s => s.SubDimension).ToList();
            var topWeaknesses = trackSubDims.OrderBy(s => s.T).Take(2).Select(s => s.SubDimension).ToList();

            string interpretation;
            
            if (avgT >= 55) // Excellent
            {
                interpretation = $"يُظهر أداءً متقدماً في {pattern.PatternNameAr} (T = {avgT:F1})، مع نقاط قوة بارزة في: {string.Join("، ", topStrengths)}. " +
                                 $"يُنصح بالحفاظ على هذا المستوى من خلال الممارسة المستمرة والتحديات الجديدة.";
            }
            else if (avgT >= 45) // Average
            {
                interpretation = $"يُظهر مستوى متوسط في {pattern.PatternNameAr} (T = {avgT:F1})، مع إمكانيات للتطوير. " +
                                 $"يُنصح بالتركيز على تحسين: {string.Join("، ", topWeaknesses)} من خلال التدريب المستهدف والممارسة العملية.";
            }
            else // Needs development
            {
                interpretation = $"يحتاج {pattern.PatternNameAr} إلى تطوير مكثف (T = {avgT:F1}). " +
                                 $"يُنصح بالبدء بتحسين: {string.Join("، ", topWeaknesses)} من خلال دورات تدريبية متخصصة ومتابعة منتظمة. " +
                                 $"التحسن في هذه المجالات سيرفع الأداء العام بشكل ملحوظ.";
            }

            return interpretation;
        }

        /// <summary>
        /// Get mini assessment for subdimension (one-liner)
        /// </summary>
        private string GetMiniAssessment(double tScore)
        {
            if (tScore >= 55) return "قوي";
            if (tScore >= 45) return "مقبول";
            if (tScore >= 35) return "يحتاج تطوير";
            return "ضعيف";
        }

        /// <summary>
        /// Calculate psychological balance score (0-100%) based on variance between patterns
        /// Lower variance = higher balance
        /// </summary>
        private int CalculateBalanceScore(List<SevenPatternScore> patterns)
        {
            if (!patterns.Any()) return 50;
            
            var scores = patterns.Select(p => p.TScore).ToList();
            var mean = scores.Average();
            var variance = scores.Sum(s => Math.Pow(s - mean, 2)) / scores.Count;
            var stdDev = Math.Sqrt(variance);
            
            // Lower std dev = more balanced
            // Perfect balance (stdDev=0) = 100%, High variance (stdDev=15+) = 0%
            var balanceScore = Math.Max(0, 100 - (stdDev * 6.67)); // Scale: 15 stdDev = 0%
            return (int)Math.Round(balanceScore);
        }

        /// <summary>
        /// Get subtle background color based on pattern performance
        /// </summary>
        private string GetPatternBackgroundColor(double tScore)
        {
            if (tScore >= 55) return "#f0fdf4"; // Very light green
            if (tScore >= 45) return "#fffbeb"; // Very light amber
            return "#fef2f2"; // Very light red
        }

        /// <summary>
        /// Get band label background color
        /// </summary>
        private string GetBandLabelBg(double tScore)
        {
            if (tScore >= 55) return "#d1fae5"; // Light green
            if (tScore >= 45) return "#fef3c7"; // Light amber
            return "#fee2e2"; // Light red
        }

        /// <summary>
        /// Generate development priorities summary for the matrix panel on Page 4
        /// </summary>
        private static string GetDevelopmentPriorities(List<SevenPatternScore> patterns)
        {
            var lowPatterns = patterns.Where(p => p.TScore < 40).OrderBy(p => p.TScore).ToList();
            var avgPatterns = patterns.Where(p => p.TScore >= 40 && p.TScore < 60).ToList();
            var highPatterns = patterns.Where(p => p.TScore >= 60).ToList();

            var priorities = new List<string>();
            
            if (lowPatterns.Any())
            {
                priorities.Add($"أولوية عالية: {lowPatterns.Count} مسارات تحتاج اهتماماً خاصاً");
            }
            if (avgPatterns.Any())
            {
                priorities.Add($"أولوية متوسطة: {avgPatterns.Count} مسارات للتطوير المستمر");
            }
            if (highPatterns.Any())
            {
                priorities.Add($"المحافظة على التميز: {highPatterns.Count} مسارات متقدمة");
            }

            return string.Join(" • ", priorities);
        }

        /// <summary>
        /// Get development priority label for a single pattern
        /// </summary>
        private static string GetDevelopmentPriority(double tScore)
        {
            if (tScore < 40) return "⚠ أولوية عالية";
            if (tScore < 50) return "📋 للمتابعة";
            if (tScore < 60) return "✓ مُرضٍ";
            return "⭐ ممتاز";
        }

        /// <summary>
        /// Get detailed Arabic assessment for subdimensions
        /// </summary>
        private static string GetDetailedAssessment(double tScore)
        {
            if (tScore >= 60) return "متقدم جداً - يُنصح بتعزيزه";
            if (tScore >= 55) return "متقدم - أداء جيد";
            if (tScore >= 50) return "مقبول - يحتاج تطوير بسيط";
            if (tScore >= 45) return "دون المتوسط - يحتاج تدريب مستهدف";
            if (tScore >= 40) return "ضعيف - أولوية تطوير";
            return "ضعيف جداً - يتطلب تدخل فوري";
        }

        /// <summary>
        /// Estimate training timeline based on score gap
        /// </summary>
        private static string GetTimelineEstimate(double tScore)
        {
            var gap = 50 - tScore; // Gap from acceptable level
            if (gap <= 5) return "4-6 أسابيع من التدريب المركز";
            if (gap <= 10) return "8-12 أسبوعاً مع متابعة دورية";
            if (gap <= 15) return "3-4 أشهر مع خطة تطوير شاملة";
            return "6 أشهر مع إشراف مستمر ومتابعة دقيقة";
        }

        /// <summary>
        /// Get enhanced track interpretation with psychological insights and professional context
        /// </summary>
        private string GetEnhancedTrackInterpretation(SevenPatternScore pattern, List<SdjSubDimensionScore> allSubDimensions)
        {
            var trackSubDims = pattern.SubDimensions.OrderByDescending(s => s.T).ToList();
            var avgT = pattern.TScore;
            
            var topStrengths = trackSubDims.Take(2).Select(s => s.SubDimension).ToList();
            var topWeaknesses = trackSubDims.OrderBy(s => s.T).Take(2).Select(s => s.SubDimension).ToList();

            var interpretation = new System.Text.StringBuilder();
            
            if (avgT >= 55) // Excellent - متقدم
            {
                interpretation.Append($"تُحقق أداءً متقدماً ومتميزاً في {pattern.PatternNameAr} (T={avgT:F1})، ");
                interpretation.Append($"مع تفوق واضح في {string.Join(" و", topStrengths)}. ");
                interpretation.Append("هذا المستوى يضعك ضمن أعلى 25% من الأفراد في هذا النمط، ");
                interpretation.Append("مما يؤهلك للأدوار المتقدمة والقيادية في هذا المجال. ");
                interpretation.Append("استمر في صقل هذه المهارات من خلال التحديات المتقدمة والتوجيه المهني المتخصص.");
            }
            else if (avgT >= 45) // Average - متوسط
            {
                interpretation.Append($"تُظهر مستوى متوسطاً في {pattern.PatternNameAr} (T={avgT:F1})، ");
                interpretation.Append($"مع إمكانيات واضحة للتطور. ");
                if (topStrengths.Any())
                {
                    interpretation.Append($"نقاط قوتك في {string.Join(" و", topStrengths)} توفر أساساً جيداً للبناء عليه. ");
                }
                interpretation.Append($"للارتقاء إلى المستوى المتقدم، ركّز على تعزيز {string.Join(" و", topWeaknesses)} ");
                interpretation.Append("من خلال التدريب المنظم والممارسة المستمرة على مدى 3-6 أشهر.");
            }
            else // Needs development - بحاجة لتنمية
            {
                interpretation.Append($"يتطلب {pattern.PatternNameAr} تطويراً مكثفاً (T={avgT:F1}). ");
                interpretation.Append($"الأبعاد الأكثر أولوية للتحسين هي: {string.Join(" و", topWeaknesses)}. ");
                interpretation.Append("نُوصي ببرنامج تطويري مُنظم يبدأ بالأساسيات ويتدرج نحو المهارات الأكثر تعقيداً، ");
                interpretation.Append("مع متابعة دورية كل شهرين لقياس التقدم. ");
                interpretation.Append("التحسن في هذا النمط سيرفع من أدائك الشامل بشكل ملحوظ.");
            }

            return interpretation.ToString();
        }

        /// <summary>
        /// Get professional implication for each pattern
        /// </summary>
        private string GetProfessionalImplication(SevenPatternScore pattern)
        {
            var implications = new Dictionary<string, string>
            {
                ["personality"] = "مناسب للأدوار التي تتطلب تفاعلاً اجتماعياً، عمل جماعي، وقدرة على فهم الآخرين.",
                ["cognitive"] = "يؤهلك للمهام التحليلية، التخطيط الاستراتيجي، وحل المشكلات المعقدة.",
                ["psychological"] = "يعكس قدرتك على التعامل مع الضغوط، الحفاظ على التوازن، والتكيف مع التغييرات.",
                ["behavioral"] = "يُظهر مدى فعاليتك في التواصل، القيادة، وإدارة المواقف الديناميكية.",
                ["numerical_logical"] = "يُؤهلك للأدوار المالية، التحليل الكمي، وصنع القرار المبني على البيانات.",
                ["leadership"] = "يعكس إمكاناتك القيادية، قدرتك على التأثير، واتخاذ القرارات الاستراتيجية.",
                ["professional_readiness"] = "يُظهر استعدادك للتحديات المهنية المعقدة والمسؤوليات العالية."
            };

            return implications.GetValueOrDefault(pattern.PatternKey, "يساهم في تطوير أدائك المهني الشامل.");
        }

        /// <summary>
        /// Generate comprehensive user analysis paragraph (data-driven, no AI)
        /// Provides detailed interpretation of all patterns, strengths, and development areas
        /// </summary>
        private string GenerateComprehensiveUserAnalysis(
            List<SevenPatternScore> patterns,
            List<SdjSubDimensionScore> subDimensions)
        {
            var sortedPatterns = patterns.OrderByDescending(p => p.TScore).ToList();
            var avgTScore = patterns.Average(p => p.TScore);
            
            // Identify strengths (T >= 55) and development areas (T < 45)
            var strengths = sortedPatterns.Where(p => p.TScore >= 55).ToList();
            var developmentAreas = sortedPatterns.Where(p => p.TScore < 45).ToList();
            
            // Get top 3 subdimensions (strengths) and bottom 3 (needs work)
            var topSubDims = subDimensions.OrderByDescending(s => s.T).Take(3).ToList();
            var bottomSubDims = subDimensions.OrderBy(s => s.T).Take(3).ToList();
            
            // Build comprehensive analysis
            var analysis = new System.Text.StringBuilder();
            
            // Overall assessment
            analysis.Append($"بناءً على تحليل شامل لأدائك عبر الأنماط السبعة، يتضح أن متوسط أدائك العام يبلغ T={avgTScore:F1}، ");
            
            if (avgTScore >= 55)
            {
                analysis.Append("وهو مستوى متقدم يعكس قدرات قوية ومتوازنة. ");
            }
            else if (avgTScore >= 45)
            {
                analysis.Append("وهو مستوى متوسط مع إمكانيات كبيرة للتطوير. ");
            }
            else
            {
                analysis.Append("مما يشير إلى حاجة للتركيز على التطوير المستمر. ");
            }
            
            // Strengths section
            if (strengths.Any())
            {
                analysis.Append($"\n\nتُظهر نتائجك تميزاً بارزاً في {strengths.Count} من الأنماط السبعة، وهي: ");
                analysis.Append(string.Join("، ", strengths.Select(s => s.PatternNameAr)));
                analysis.Append(". ");
                
                analysis.Append($"على مستوى الأبعاد الفرعية، تبرز قوتك بشكل خاص في: ");
                analysis.Append(string.Join("، ", topSubDims.Select(s => $"{s.SubDimension} (T={s.T:F1})")));
                analysis.Append(". هذه المهارات تشكل أساساً متيناً يمكن البناء عليه في المجالات المهنية والشخصية.");
            }
            
            // Development areas section
            if (developmentAreas.Any())
            {
                analysis.Append($"\n\nمن جهة أخرى، هناك {developmentAreas.Count} من الأنماط تحتاج إلى تطوير مستهدف، وهي: ");
                analysis.Append(string.Join("، ", developmentAreas.Select(d => d.PatternNameAr)));
                analysis.Append(". ");
                
                analysis.Append($"وعلى مستوى أكثر دقة، الأبعاد الفرعية التالية تتطلب اهتماماً خاصاً: ");
                analysis.Append(string.Join("، ", bottomSubDims.Select(s => $"{s.SubDimension} (T={s.T:F1})")));
                analysis.Append(". التحسن في هذه المجالات سيرفع من أدائك العام بشكل ملموس.");
            }
            
            // Personalized recommendations based on profile
            analysis.Append("\n\nبالنظر إلى ملفك النفسي المتكامل، ");
            
            var personalityPattern = patterns.FirstOrDefault(p => p.PatternKey == "personality");
            var cognitivePattern = patterns.FirstOrDefault(p => p.PatternKey == "cognitive");
            var professionalPattern = patterns.FirstOrDefault(p => p.PatternKey == "professional_readiness");
            
            if (personalityPattern != null && personalityPattern.TScore >= 55 && cognitivePattern != null && cognitivePattern.TScore >= 55)
            {
                analysis.Append("يتضح أن لديك مزيجاً قوياً من السمات الشخصية الإيجابية والقدرات المعرفية العالية، ");
                analysis.Append("مما يجعلك مؤهلاً للأدوار القيادية والتخطيطية الاستراتيجية. ");
            }
            else if (professionalPattern != null && professionalPattern.TScore >= 55)
            {
                analysis.Append("تُظهر استعداداً مهنياً عالياً يؤهلك للنجاح في بيئات العمل المعقدة والديناميكية. ");
            }
            else
            {
                analysis.Append("يُنصح بالتركيز على بناء قاعدة صلبة من المهارات الأساسية قبل الانتقال إلى مستويات أعلى من التحديات. ");
            }
            
            // Action-oriented conclusion
            analysis.Append("لتحقيق أقصى استفادة من هذا التقرير، ");
            analysis.Append("ابدأ بالتركيز على الدورات التدريبية المقترحة للأبعاد ذات الأولوية القصوى، ");
            analysis.Append("وضع خطة تطويرية زمنية واضحة (3-6 أشهر)، ");
            analysis.Append("واحرص على قياس التقدم بشكل دوري من خلال إعادة التقييم.");
            
            return analysis.ToString();
        }

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
                        Console.WriteLine($"[ModernSdjSevenPatternReportService] ✓ Font loaded: Noto Naskh Arabic Regular");
                    }

                    if (File.Exists(boldPath))
                    {
                        QuestPDF.Drawing.FontManager.RegisterFont(File.OpenRead(boldPath));
                        Console.WriteLine($"[ModernSdjSevenPatternReportService] ✓ Font loaded: Noto Naskh Arabic Bold");
                    }

                    _fontsRegistered = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ModernSdjSevenPatternReportService] ❌ Font registration failed: {ex.Message}");
                }
            }
        }

        private static void LoadLogo()
        {
            try
            {
                // PRIMARY: Use STEST.PNG as per requirements
                var logoPath = Path.Combine(AppContext.BaseDirectory, "Resources", "Brand", "STEST.png");
                
                // Fallbacks if STEST.png not found
                if (!File.Exists(logoPath))
                {
                    logoPath = Path.Combine(AppContext.BaseDirectory, "Resources", "Brand", "SAITEST-ICON.png");
                }
                if (!File.Exists(logoPath))
                {
                    logoPath = Path.Combine(AppContext.BaseDirectory, "Resources", "Brand", "SAITES-ICON.png");
                }

                if (File.Exists(logoPath))
                {
                    _logoBytes = File.ReadAllBytes(logoPath);
                    Console.WriteLine($"[ModernSdjSevenPatternReportService] ✓ Logo loaded from {Path.GetFileName(logoPath)}");
                }
                else
                {
                    Console.WriteLine($"[ModernSdjSevenPatternReportService] ⚠ Logo not found in Resources/Brand/");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ModernSdjSevenPatternReportService] ⚠ Logo loading error: {ex.Message}");
            }
        }

        #endregion
    }
}
