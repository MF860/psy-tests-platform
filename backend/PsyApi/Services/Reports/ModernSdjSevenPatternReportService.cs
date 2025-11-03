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
                    // Page 1: Cover with STEST logo, participant info, professional intro, status badge
                    document.Page(page =>
                    {
                        ConfigurePageDefaults(page);
                        ComposePage1_CoverAndIntro(page, user, result, patterns);
                    });

                    // Page 2: Executive Summary with 4 KPI cards (HiFi)
                    document.Page(page =>
                    {
                        ConfigurePageDefaults(page);
                        ComposePage2_ExecutiveSummary(page, patterns, sdjData.SubDimensions);
                    });

                    // Page 3: Visual analytics - charts (subdimensions bar + 7-pattern radar)
                    document.Page(page =>
                    {
                        ConfigurePageDefaults(page);
                        ComposePage3_ChartsAndVisualizations(page, patterns, sdjData.SubDimensions);
                    });

                    // Page 4: Seven Tracks Summary
                    document.Page(page =>
                    {
                        ConfigurePageDefaults(page);
                        ComposePage4_SevenTracksSummary(page, patterns, sdjData.SubDimensions);
                    });

                    // Page 5: Detailed track analyses with courses
                    document.Page(page =>
                    {
                        ConfigurePageDefaults(page);
                        ComposePage5_DetailedTrackAnalysesAndCourses(page, patterns, sdjData.SubDimensions, courseRecommendations);
                    });
                });

                var pdfBytes = pdf.GeneratePdf();
                var endTime = DateTime.Now;
                var duration = endTime - startTime;

                Console.WriteLine($"\n✅ SDJ 7-PATTERN PDF COMPLETE");
                Console.WriteLine($"   Pages: 5 (Cover + Executive Summary + Charts + Seven Tracks + Courses)");
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
        /// Page 1: Professional HiFi cover with large logo, title, participant grid, and status badge
        /// v3.0 Enhanced with overall performance indicator
        /// </summary>
        private void ComposePage1_CoverAndIntro(
            PageDescriptor page,
            User user,
            Result result,
            List<SevenPatternScore> patterns)
        {
            // Calculate overall performance for status badge
            var avgTScore = patterns.Average(p => p.TScore);
            var overallStatus = avgTScore >= 55 ? "أداء متقدم" : avgTScore >= 45 ? "أداء متوسط" : "يحتاج تطوير";
            var statusColor = ReportTheme.GetBandColor(avgTScore);

            page.Content().Column(column =>
            {
                column.Spacing(HiFiSettings.Spacing.LG);

                // LARGE CENTERED LOGO (Enhanced size for impact)
                if (_logoBytes != null && _logoBytes.Length > 0)
                {
                    column.Item()
                        .AlignCenter()
                        .Width(140) // Increased from 100
                        .Image(_logoBytes);
                }

                column.Item().PaddingTop(HiFiSettings.Spacing.XL);

                // MAIN TITLE (Enhanced typography)
                column.Item().AlignCenter()
                    .Text(UnicodeTextHelper.NormalizeNfc("التقرير النفسي الشامل — نتائج القياس والتحليل"))
                    .Style(ReportTheme.ArabicTextStyle(ReportTheme.Typography.H1, true, ReportTheme.Colors.Primary));

                // SUBTITLE
                column.Item()
                    .PaddingTop(HiFiSettings.Spacing.SM)
                    .AlignCenter()
                    .Text(UnicodeTextHelper.NormalizeNfc("تقرير تحليل الأنماط النفسية السبعة - SDJ v2.1"))
                    .Style(ReportTheme.ArabicTextStyle(ReportTheme.Typography.H3, false, ReportTheme.Colors.TextSecondary));

                column.Item().PaddingTop(HiFiSettings.Spacing.XXL);

                // OVERALL STATUS BADGE (New - shows overall performance)
                column.Item()
                    .AlignCenter()
                    .BadgeWithIcon(
                        $"{overallStatus} • {UnicodeTextHelper.FormatTScore(avgTScore, 1)}",
                        statusColor,
                        icon: avgTScore >= 55 ? "⭐" : avgTScore >= 45 ? "📊" : "📈",
                        textColor: "#FFFFFF",
                        fontSize: ReportTheme.Typography.H3
                    );

                column.Item().PaddingTop(HiFiSettings.Spacing.XXL);

                // PARTICIPANT INFO GRID (Enhanced with better spacing)
                column.Item()
                    .Background(ReportTheme.Colors.Surface)
                    .Border(2)
                    .BorderColor(ReportTheme.Colors.Primary)
                    .Padding(HiFiSettings.Spacing.XL)
                    .Column(info =>
                    {
                        // Centered block title
                        info.Item().AlignCenter()
                            .Text(UnicodeTextHelper.NormalizeNfc("بيانات المشارك"))
                            .Style(ReportTheme.ArabicTextStyle(ReportTheme.Typography.H2, true, ReportTheme.Colors.Text));
                        
                        info.Item().PaddingTop(HiFiSettings.Spacing.MD);
                        
                        // Divider
                        info.Item()
                            .Height(2)
                            .Background(ReportTheme.Colors.Primary);

                        info.Item().PaddingTop(HiFiSettings.Spacing.LG);
                        
                        // Use StatRow components for cleaner layout
                        info.Item().StatRow("الاسم الكامل", UnicodeTextHelper.NormalizeNfc(user.FullName ?? "غير محدد"));
                        info.Item().StatRow("الرقم الوطني", user.NationalId ?? "N/A");
                        info.Item().StatRow("رقم الجلسة", result.SessionId.ToString());
                        info.Item().StatRow("التاريخ والوقت", 
                            UnicodeTextHelper.NormalizeNfc(result.CreatedAt.ToString("dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture)));
                    });

                column.Item().PaddingTop(HiFiSettings.Spacing.XXL);

                // PROFESSIONAL INTRO (Enhanced with icon)
                column.Item()
                    .Background(ReportTheme.Colors.SurfaceHover)
                    .Border(1)
                    .BorderColor(ReportTheme.Colors.Border)
                    .Padding(HiFiSettings.Spacing.LG)
                    .Row(row =>
                    {
                        row.AutoItem()
                            .PaddingLeft(HiFiSettings.Spacing.MD)
                            .Text("ℹ️")
                            .FontSize(ReportTheme.Typography.KPI);

                        row.RelativeItem()
                            .Text(UnicodeTextHelper.NormalizeNfc(
                                "يُقدم هذا التقرير تحليلاً شاملاً لسماتك النفسية وقدراتك المهنية بناءً على إطار التنمية المستدامة SDJ. " +
                                "تم تقييم أدائك عبر سبعة أنماط رئيسية و٢٤ بُعداً فرعياً، وتمثل الدرجات كنسب معيارية T-Scores حيث " +
                                "٥٠ هي المتوسط. يتضمن التقرير توصيات تطويرية مخصصة لتعزيز جوانب القوة ومعالجة مجالات التحسين."
                            ))
                            .Style(ReportTheme.ArabicTextStyle(ReportTheme.Typography.Body, false, ReportTheme.Colors.Text))
                            .LineHeight(1.7f);
                    });
            });
        }

        /// <summary>
        /// Page 2: Executive Summary with 4 HiFi KPI cards and professional insight paragraph
        /// v3.0 HiFi: Large KPI values, trend indicators, balanced performance metrics
        /// </summary>
        private void ComposePage2_ExecutiveSummary(
            PageDescriptor page,
            List<SevenPatternScore> patterns,
            List<SdjSubDimensionScore> subDimensions)
        {
            page.Content().Column(column =>
            {
                column.Spacing(HiFiSettings.Spacing.LG);

                // SECTION HEADER
                column.Item().SectionHeader(
                    title: UnicodeTextHelper.NormalizeNfc("الملخص التنفيذي"),
                    subtitle: UnicodeTextHelper.NormalizeNfc("لمحة سريعة عن الأداء النفسي الشامل"),
                    icon: "📊"
                );

                column.Item().PaddingTop(HiFiSettings.Spacing.XL);

                // Calculate metrics for KPI cards
                var avgTScore = patterns.Average(p => p.TScore);
                var advancedCount = patterns.Count(p => p.TScore >= 55);
                var maxTScore = patterns.Max(p => p.TScore);
                var minTScore = patterns.Min(p => p.TScore);
                var variance = maxTScore - minTScore;
                
                // Balance Index: 100% means perfect balance (no variance), 0% means max variance (100 points)
                // Formula: 100 - (variance * 100 / 100) → simplified to 100 - variance
                var balanceIndex = Math.Max(0, 100 - variance);

                // Determine trend based on performance level
                string avgTrend = avgTScore >= 55 ? "أداء متميز" : avgTScore >= 45 ? "أداء متوسط" : "يحتاج دعم";
                string avgIcon = avgTScore >= 55 ? "⭐" : avgTScore >= 45 ? "📊" : "📈";
                
                string balanceTrend = balanceIndex >= 70 ? "توازن ممتاز" : balanceIndex >= 50 ? "توازن مقبول" : "يحتاج موازنة";
                string balanceIcon = balanceIndex >= 70 ? "⚖️" : "⚠️";

                // Row 1: Average T-Score + Advanced Patterns Count
                column.Item().Row(row =>
                {
                    row.Spacing(HiFiSettings.Spacing.MD);
                    
                    // KPI 1: Average T-Score
                    row.RelativeItem().KpiCard(
                        value: UnicodeTextHelper.FormatTScore(avgTScore, 1),
                        label: UnicodeTextHelper.NormalizeNfc("متوسط T-Score الشامل"),
                        valueColor: ReportTheme.GetBandColor(avgTScore),
                        trendIcon: avgIcon,
                        trendText: UnicodeTextHelper.NormalizeNfc(avgTrend)
                    );

                    // KPI 2: Advanced Patterns Count
                    row.RelativeItem().KpiCard(
                        value: UnicodeTextHelper.FormatTScore(advancedCount, 0) + " / ٧",
                        label: UnicodeTextHelper.NormalizeNfc("الأنماط المتقدمة"),
                        valueColor: advancedCount >= 5 ? ReportTheme.Colors.Success : 
                                   advancedCount >= 3 ? ReportTheme.Colors.Warning : ReportTheme.Colors.Danger,
                        trendIcon: advancedCount >= 5 ? "🎯" : advancedCount >= 3 ? "📌" : "🔄",
                        trendText: UnicodeTextHelper.NormalizeNfc(
                            advancedCount >= 5 ? "أغلبية متميزة" : 
                            advancedCount >= 3 ? "أداء جيد" : "يحتاج تطوير"
                        )
                    );
                });

                column.Item().PaddingTop(HiFiSettings.Spacing.MD);

                // Row 2: Variance + Balance Index
                column.Item().Row(row =>
                {
                    row.Spacing(HiFiSettings.Spacing.MD);
                    
                    // KPI 3: Variance (Range)
                    row.RelativeItem().KpiCard(
                        value: UnicodeTextHelper.FormatTScore(variance, 1),
                        label: UnicodeTextHelper.NormalizeNfc("التفاوت (المدى)"),
                        valueColor: variance <= 15 ? ReportTheme.Colors.Success : 
                                   variance <= 25 ? ReportTheme.Colors.Warning : ReportTheme.Colors.Danger,
                        trendIcon: variance <= 15 ? "✓" : variance <= 25 ? "⚠" : "⚡",
                        trendText: UnicodeTextHelper.NormalizeNfc(
                            variance <= 15 ? "تفاوت منخفض" : 
                            variance <= 25 ? "تفاوت متوسط" : "تفاوت عالٍ"
                        )
                    );

                    // KPI 4: Balance Index
                    row.RelativeItem().KpiCard(
                        value: UnicodeTextHelper.FormatTScore(balanceIndex, 0) + "%",
                        label: UnicodeTextHelper.NormalizeNfc("مؤشر التوازن"),
                        valueColor: balanceIndex >= 70 ? ReportTheme.Colors.Success : 
                                   balanceIndex >= 50 ? ReportTheme.Colors.Warning : ReportTheme.Colors.Info,
                        trendIcon: balanceIcon,
                        trendText: UnicodeTextHelper.NormalizeNfc(balanceTrend)
                    );
                });

                column.Item().PaddingTop(HiFiSettings.Spacing.XL);

                // INSIGHT PARAGRAPH (Professional 2-3 line summary)
                column.Item()
                    .Background(ReportTheme.Colors.SurfaceHover)
                    .Border(2)
                    .BorderColor(ReportTheme.Colors.Primary)
                    .Padding(HiFiSettings.Spacing.LG)
                    .Column(insightBox =>
                    {
                        insightBox.Item().Row(titleRow =>
                        {
                            titleRow.AutoItem()
                                .PaddingLeft(HiFiSettings.Spacing.SM)
                                .Text("💡")
                                .Style(TextStyle.Default.FontSize(ReportTheme.Typography.KPI));
                            
                            titleRow.RelativeItem()
                                .AlignRight()
                                .Text(UnicodeTextHelper.NormalizeNfc("الرؤية الإحصائية"))
                                .Style(ReportTheme.ArabicTextStyle(ReportTheme.Typography.H3, true, ReportTheme.Colors.Primary));
                        });

                        insightBox.Item().PaddingTop(HiFiSettings.Spacing.SM);

                        // Generate insight based on metrics
                        string insightText = GenerateExecutiveInsight(avgTScore, advancedCount, variance, balanceIndex);
                        
                        insightBox.Item()
                            .AlignRight()
                            .Text(UnicodeTextHelper.NormalizeNfc(insightText))
                            .Style(ReportTheme.ArabicTextStyle(
                                ReportTheme.Typography.Body, 
                                false, 
                                ReportTheme.Colors.Text
                            ))
                            .LineHeight(1.7f);
                    });

                column.Item().PaddingTop(HiFiSettings.Spacing.LG);

                // SUB-STATISTICS: Top 3 Strongest and Weakest Dimensions
                column.Item().Row(row =>
                {
                    row.Spacing(HiFiSettings.Spacing.LG);

                    // Strongest Dimensions
                    row.RelativeItem()
                        .Background(ReportTheme.Colors.Surface)
                        .Border(1)
                        .BorderColor(ReportTheme.Colors.Success)
                        .Padding(HiFiSettings.Spacing.MD)
                        .Column(strongCol =>
                        {
                            strongCol.Item()
                                .AlignRight()
                                .Text(UnicodeTextHelper.NormalizeNfc("🏆 أقوى ٣ أنماط"))
                                .Style(ReportTheme.ArabicTextStyle(
                                    ReportTheme.Typography.H4, 
                                    true, 
                                    ReportTheme.Colors.Success
                                ));

                            strongCol.Item().PaddingTop(HiFiSettings.Spacing.SM);

                            var topPatterns = patterns.OrderByDescending(p => p.TScore).Take(3).ToList();
                            foreach (var pattern in topPatterns)
                            {
                                strongCol.Item().StatRow(
                                    label: UnicodeTextHelper.NormalizeNfc(pattern.PatternNameAr),
                                    value: UnicodeTextHelper.FormatTScore(pattern.TScore, 1),
                                    valueColor: ReportTheme.GetBandColor(pattern.TScore)
                                );
                            }
                        });

                    // Weakest Dimensions (Needs Development)
                    row.RelativeItem()
                        .Background(ReportTheme.Colors.Surface)
                        .Border(1)
                        .BorderColor(ReportTheme.Colors.Warning)
                        .Padding(HiFiSettings.Spacing.MD)
                        .Column(weakCol =>
                        {
                            weakCol.Item()
                                .AlignRight()
                                .Text(UnicodeTextHelper.NormalizeNfc("📈 أنماط تحتاج دعم"))
                                .Style(ReportTheme.ArabicTextStyle(
                                    ReportTheme.Typography.H4, 
                                    true, 
                                    ReportTheme.Colors.Warning
                                ));

                            weakCol.Item().PaddingTop(HiFiSettings.Spacing.SM);

                            var bottomPatterns = patterns.OrderBy(p => p.TScore).Take(3).ToList();
                            foreach (var pattern in bottomPatterns)
                            {
                                weakCol.Item().StatRow(
                                    label: UnicodeTextHelper.NormalizeNfc(pattern.PatternNameAr),
                                    value: UnicodeTextHelper.FormatTScore(pattern.TScore, 1),
                                    valueColor: ReportTheme.GetBandColor(pattern.TScore)
                                );
                            }
                        });
                });
            });
        }

        /// <summary>
        /// Generate professional insight paragraph based on executive metrics
        /// </summary>
        private string GenerateExecutiveInsight(double avgTScore, int advancedCount, double variance, double balanceIndex)
        {
            string performanceLevel = avgTScore >= 55 ? "متميز" : avgTScore >= 45 ? "متوسط" : "يحتاج تطوير";
            string balanceStatus = balanceIndex >= 70 ? "متوازنة بشكل ممتاز" : 
                                  balanceIndex >= 50 ? "متوازنة بشكل مقبول" : "تحتاج إعادة موازنة";
            
            if (avgTScore >= 55 && advancedCount >= 5)
            {
                return $"يُظهر المشارك أداءً {performanceLevel} مع {advancedCount} أنماط متقدمة من أصل ٧، والدرجات {balanceStatus}. " +
                       $"هذا المستوى يعكس قدرات نفسية قوية وثبات في الأداء عبر الأنماط المختلفة. يُنصح بالاستمرار في التعزيز مع التركيز على الأنماط الأضعف لتحقيق التوازن الكامل.";
            }
            else if (avgTScore >= 45 && variance <= 20)
            {
                return $"الأداء العام {performanceLevel} مع تفاوت معتدل ({variance:F1} نقطة)، والدرجات {balanceStatus}. " +
                       $"يوجد ({advancedCount}) نمط متقدم، مما يشير إلى قدرات جيدة في مجالات محددة. " +
                       $"التركيز على تطوير الأنماط الأضعف من خلال البرامج التدريبية سيرفع الأداء الكلي بشكل ملحوظ.";
            }
            else
            {
                return $"الأداء العام {performanceLevel} مع تفاوت عالٍ ({variance:F1} نقطة)، والدرجات {balanceStatus}. " +
                       $"يوجد ({advancedCount}) نمط متقدم فقط من أصل ٧. " +
                       $"يُوصى بخطة تطوير شاملة تستهدف الأنماط الأضعف أولاً مع تعزيز نقاط القوة الموجودة لتحقيق توازن أفضل وأداء أكثر استقراراً.";
            }
        }

        /// <summary>
        /// Page 3: Visual analytics - horizontal bar chart (subdimensions) + radar chart (7 patterns)
        /// v3.0 HiFi: Enhanced with SectionHeader, interpretation text, Unicode formatting
        /// </summary>
        private void ComposePage3_ChartsAndVisualizations(
            PageDescriptor page,
            List<SevenPatternScore> patterns,
            List<SdjSubDimensionScore> subDimensions)
        {
            page.Content().Column(column =>
            {
                column.Spacing(HiFiSettings.Spacing.LG);

                // MAIN SECTION HEADER
                column.Item().SectionHeader(
                    title: UnicodeTextHelper.NormalizeNfc("التحليل البصري المتقدم"),
                    subtitle: UnicodeTextHelper.NormalizeNfc("رسوم بيانية عالية الجودة للأنماط والأبعاد الفرعية"),
                    icon: "📊"
                );

                column.Item().PaddingTop(HiFiSettings.Spacing.MD);

                // SUB-SECTION 1: Horizontal Bar Chart for Subdimensions
                column.Item()
                    .Background(ReportTheme.Colors.Surface)
                    .Border(2)
                    .BorderColor(ReportTheme.Colors.Primary)
                    .Padding(HiFiSettings.Spacing.MD)
                    .Column(barSection =>
                    {
                        // Title with icon
                        barSection.Item().Row(titleRow =>
                        {
                            titleRow.AutoItem()
                                .PaddingLeft(HiFiSettings.Spacing.SM)
                                .Text("📊")
                                .Style(TextStyle.Default.FontSize(ReportTheme.Typography.H3));
                            
                            titleRow.RelativeItem()
                                .AlignRight()
                                .Text(UnicodeTextHelper.NormalizeNfc("التوزيع التفصيلي للأبعاد الفرعية"))
                                .Style(ReportTheme.ArabicTextStyle(ReportTheme.Typography.H3, true, ReportTheme.Colors.Primary));
                        });

                        barSection.Item().PaddingTop(HiFiSettings.Spacing.XS);
                        
                        barSection.Item()
                            .AlignRight()
                            .Text(UnicodeTextHelper.NormalizeNfc("مخطط شريطي يوضح أداء جميع الأبعاد الفرعية مرتبة من الأضعف إلى الأقوى (T-Score)"))
                            .Style(ReportTheme.ArabicTextStyle(ReportTheme.Typography.BodySmall, false, ReportTheme.Colors.TextSecondary))
                            .LineHeight(1.5f);

                        barSection.Item().PaddingTop(HiFiSettings.Spacing.MD);

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
                            
                            barSection.Item().AlignCenter().Image(barChartBytes);
                            Console.WriteLine($"   ✓ HiFi horizontal bar chart rendered ({sortedSubDims.Count} dimensions at 3× scale)");
                            
                            // Chart interpretation
                            barSection.Item().PaddingTop(HiFiSettings.Spacing.MD);
                            
                            var avgSubDimTScore = sortedSubDims.Average(s => s.T);
                            var highPerformers = sortedSubDims.Count(s => s.T >= 55);
                            var lowPerformers = sortedSubDims.Count(s => s.T < 45);
                            
                            string interpretation = $"المخطط يُظهر {UnicodeTextHelper.FormatTScore(sortedSubDims.Count, 0)} بُعد فرعي بمتوسط " +
                                                  $"{UnicodeTextHelper.FormatTScore(avgSubDimTScore, 1)} نقطة. " +
                                                  $"هناك {UnicodeTextHelper.FormatTScore(highPerformers, 0)} بُعد متقدم (≥٥٥) و" +
                                                  $"{UnicodeTextHelper.FormatTScore(lowPerformers, 0)} بُعد يحتاج دعم (<٤٥).";
                            
                            barSection.Item()
                                .Background(ReportTheme.Colors.SurfaceHover)
                                .Padding(HiFiSettings.Spacing.SM)
                                .AlignRight()
                                .Text(UnicodeTextHelper.NormalizeNfc(interpretation))
                                .Style(ReportTheme.ArabicTextStyle(ReportTheme.Typography.BodySmall, false, ReportTheme.Colors.Text))
                                .LineHeight(1.6f);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"   ⚠ Bar chart rendering failed: {ex.Message}");
                            Console.WriteLine($"   Stack: {ex.StackTrace}");
                            barSection.Item().AlignCenter().PaddingVertical(HiFiSettings.Spacing.MD)
                                .Text($"[خطأ في رسم المخطط الشريطي: {ex.Message}]")
                                .Style(TextStyle.Default.FontSize(10).FontColor(ReportTheme.Colors.Danger));
                        }
                    });

                column.Item().PaddingTop(HiFiSettings.Spacing.LG);

                // SUB-SECTION 2: Heptagon Radar Chart for 7 Patterns
                column.Item()
                    .Background(ReportTheme.Colors.Surface)
                    .Border(2)
                    .BorderColor(ReportTheme.Colors.Primary)
                    .Padding(HiFiSettings.Spacing.MD)
                    .Column(radarSection =>
                    {
                        // Title with icon
                        radarSection.Item().Row(titleRow =>
                        {
                            titleRow.AutoItem()
                                .PaddingLeft(HiFiSettings.Spacing.SM)
                                .Text("🔷")
                                .Style(TextStyle.Default.FontSize(ReportTheme.Typography.H3));
                            
                            titleRow.RelativeItem()
                                .AlignRight()
                                .Text(UnicodeTextHelper.NormalizeNfc("الخريطة النفسية السباعية"))
                                .Style(ReportTheme.ArabicTextStyle(ReportTheme.Typography.H3, true, ReportTheme.Colors.Primary));
                        });

                        radarSection.Item().PaddingTop(HiFiSettings.Spacing.XS);
                        
                        radarSection.Item()
                            .AlignRight()
                            .Text(UnicodeTextHelper.NormalizeNfc("مخطط رادار يُظهر التوزيع المتوازن للأنماط السبعة الرئيسية بشكل بصري"))
                            .Style(ReportTheme.ArabicTextStyle(ReportTheme.Typography.BodySmall, false, ReportTheme.Colors.TextSecondary))
                            .LineHeight(1.5f);

                        radarSection.Item().PaddingTop(HiFiSettings.Spacing.MD);

                        try
                        {
                            var heptagonBytes = HeptagonRadarChartRenderer.RenderHeptagonChart(
                                patterns, 
                                size: 420, 
                                title: UnicodeTextHelper.NormalizeNfc("الخريطة النفسية السباعية"));
                            
                            radarSection.Item().AlignCenter().Height(420).Image(heptagonBytes);
                            Console.WriteLine("   ✓ HiFi heptagon radar chart rendered (3× scale, Quality=100, Arabic HarfBuzz)");
                            
                            // Chart interpretation
                            radarSection.Item().PaddingTop(HiFiSettings.Spacing.MD);
                            
                            var avgPatternTScore = patterns.Average(p => p.TScore);
                            var strongPatterns = patterns.Where(p => p.TScore >= 55).ToList();
                            var weakPatterns = patterns.Where(p => p.TScore < 45).ToList();
                            
                            string shapeAnalysis = "متوازنة" ;
                            if (strongPatterns.Count >= 5)
                                shapeAnalysis = "قوية ومتماسكة";
                            else if (weakPatterns.Count >= 4)
                                shapeAnalysis = "غير متوازنة وتحتاج تطوير";
                            
                            string interpretation = $"الخريطة السباعية تكشف شكل {shapeAnalysis} بمتوسط " +
                                                  $"{UnicodeTextHelper.FormatTScore(avgPatternTScore, 1)} نقطة. " +
                                                  $"الأنماط القوية ({strongPatterns.Count}): " +
                                                  string.Join("، ", strongPatterns.Select(p => UnicodeTextHelper.NormalizeNfc(p.PatternNameAr)).Take(3)) +
                                                  (strongPatterns.Count > 3 ? "..." : ".") +
                                                  (weakPatterns.Any() 
                                                    ? $" الأنماط تحتاج دعم ({weakPatterns.Count}): " +
                                                      string.Join("، ", weakPatterns.Select(p => UnicodeTextHelper.NormalizeNfc(p.PatternNameAr)).Take(2)) +
                                                      (weakPatterns.Count > 2 ? "..." : ".")
                                                    : "");
                            
                            radarSection.Item()
                                .Background(ReportTheme.Colors.SurfaceHover)
                                .Padding(HiFiSettings.Spacing.SM)
                                .AlignRight()
                                .Text(UnicodeTextHelper.NormalizeNfc(interpretation))
                                .Style(ReportTheme.ArabicTextStyle(ReportTheme.Typography.BodySmall, false, ReportTheme.Colors.Text))
                                .LineHeight(1.6f);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"   ⚠ Heptagon chart rendering failed: {ex.Message}");
                            radarSection.Item().AlignCenter().PaddingVertical(HiFiSettings.Spacing.MD)
                                .Text("[خطأ في رسم المخطط السباعي]")
                                .Style(TextStyle.Default.FontSize(11).FontColor(ReportTheme.Colors.Danger));
                        }
                    });
            });
        }

        /// <summary>
        /// Page 4: Seven Tracks Summary with statistical insights and detailed analysis
        /// v3.0 HiFi: Using PatternCard components for professional card-based layout
        /// </summary>
        private void ComposePage4_SevenTracksSummary(
            PageDescriptor page,
            List<SevenPatternScore> patterns,
            List<SdjSubDimensionScore> subDimensions)
        {
            page.Content().Column(column =>
            {
                column.Spacing(HiFiSettings.Spacing.MD);

                // SECTION HEADER
                column.Item().SectionHeader(
                    title: UnicodeTextHelper.NormalizeNfc("تحليل الأنماط السبعة"),
                    subtitle: UnicodeTextHelper.NormalizeNfc("بطاقات شاملة لكل نمط مع التفسير والتوصيات المهنية"),
                    icon: "🔷"
                );

                column.Item().PaddingTop(HiFiSettings.Spacing.SM);

                // STATISTICAL OVERVIEW (Quick stats bar)
                var avgTScore = patterns.Average(p => p.TScore);
                var excellentCount = patterns.Count(p => p.TScore >= 55);
                var averageCount = patterns.Count(p => p.TScore >= 45 && p.TScore < 55);
                var needsDevelopmentCount = patterns.Count(p => p.TScore < 45);

                column.Item()
                    .Background(ReportTheme.Colors.SurfaceHover)
                    .Border(1)
                    .BorderColor(ReportTheme.Colors.Border)
                    .Padding(HiFiSettings.Spacing.SM)
                    .Row(statsRow =>
                    {
                        statsRow.RelativeItem().AlignCenter().StatRow(
                            label: UnicodeTextHelper.NormalizeNfc("المتوسط العام"),
                            value: UnicodeTextHelper.FormatTScore(avgTScore, 1),
                            valueColor: ReportTheme.GetBandColor(avgTScore)
                        );
                        
                        statsRow.RelativeItem().AlignCenter().StatRow(
                            label: UnicodeTextHelper.NormalizeNfc("أنماط متقدمة"),
                            value: $"{UnicodeTextHelper.FormatTScore(excellentCount, 0)} / ٧",
                            valueColor: ReportTheme.Colors.Success
                        );
                        
                        statsRow.RelativeItem().AlignCenter().StatRow(
                            label: UnicodeTextHelper.NormalizeNfc("أنماط متوسطة"),
                            value: UnicodeTextHelper.FormatTScore(averageCount, 0),
                            valueColor: ReportTheme.Colors.Warning
                        );
                        
                        statsRow.RelativeItem().AlignCenter().StatRow(
                            label: UnicodeTextHelper.NormalizeNfc("تحتاج دعم"),
                            value: UnicodeTextHelper.FormatTScore(needsDevelopmentCount, 0),
                            valueColor: ReportTheme.Colors.Danger
                        );
                    });

                column.Item().PaddingTop(HiFiSettings.Spacing.LG);

                // Sort patterns by T-score descending (best first for positive tone)
                var sortedPatterns = patterns.OrderByDescending(p => p.TScore).ToList();

                // Display all 7 patterns using PatternCard component
                foreach (var pattern in sortedPatterns)
                {
                    // Generate description (2-3 lines about the pattern)
                    var description = GeneratePatternDescription(pattern, subDimensions);
                    
                    // Generate professional recommendation
                    var recommendation = GeneratePatternRecommendation(pattern, subDimensions);

                    // Create the PatternCard
                    column.Item().PatternCard(
                        title: UnicodeTextHelper.NormalizeNfc(pattern.PatternNameAr),
                        tScore: pattern.TScore,
                        description: UnicodeTextHelper.NormalizeNfc(description),
                        recommendation: UnicodeTextHelper.NormalizeNfc(recommendation)
                    );
                }
            });
        }

        /// <summary>
        /// Generate a 2-3 line description for a pattern based on its score and subdimensions
        /// </summary>
        private string GeneratePatternDescription(SevenPatternScore pattern, List<SdjSubDimensionScore> subDimensions)
        {
            var topSubDims = pattern.SubDimensions.OrderByDescending(s => s.T).Take(3).ToList();
            var strengths = string.Join("، ", topSubDims.Select(s => s.SubDimension));
            
            if (pattern.TScore >= 55)
            {
                return $"يُظهر المشارك قدرات متميزة في هذا النمط بدرجة {pattern.TScore:F1} نقطة. " +
                       $"الكفاءات البارزة: {strengths}. " +
                       $"يشير هذا المستوى إلى استعداد عالٍ للأداء في المجالات ذات الصلة والقدرة على التفوق المهني في هذا النمط.";
            }
            else if (pattern.TScore >= 45)
            {
                return $"يُظهر المشارك أداءً متوسطاً في هذا النمط بدرجة {pattern.TScore:F1} نقطة. " +
                       $"الكفاءات الرئيسية: {strengths}. " +
                       $"هناك قاعدة جيدة يمكن البناء عليها من خلال التدريب الموجه والممارسة المنتظمة.";
            }
            else
            {
                return $"يحتاج هذا النمط إلى دعم وتطوير بدرجة حالية {pattern.TScore:F1} نقطة. " +
                       $"المجالات الرئيسية: {strengths}. " +
                       $"التركيز على برامج تدريبية مُستهدفة وتمارين عملية سيساعد في رفع الأداء بشكل تدريجي وملموس.";
            }
        }

        /// <summary>
        /// Generate a professional recommendation for a pattern
        /// </summary>
        private string GeneratePatternRecommendation(SevenPatternScore pattern, List<SdjSubDimensionScore> subDimensions)
        {
            if (pattern.TScore >= 55)
            {
                return "الحفاظ على المستوى الحالي من خلال التطبيق المستمر والتحدي بمهام أكثر تعقيداً. " +
                       "يمكن استثمار هذا التميز في تدريب الآخرين أو قيادة مشاريع متخصصة في هذا المجال.";
            }
            else if (pattern.TScore >= 45)
            {
                return "التركيز على التدريب الموجه في المجالات الفرعية الأضعف. " +
                       "المشاركة في ورش عمل متخصصة وتطبيق المهارات في بيئة عملية سيرفع الأداء للمستوى المتقدم.";
            }
            else
            {
                return "وضع خطة تطوير مكثفة مع جدول زمني واضح. " +
                       "البدء بالأساسيات من خلال دورات تأسيسية، ثم الانتقال تدريجياً للمستويات المتقدمة مع متابعة دورية للتقدم.";
            }
        }

        /// <summary>
        /// Page 4: Detailed track analyses with comprehensive course recommendations and development roadmap
        /// Title: "الدورات المقترحة لكل مسار" (no mention of "weak")
        /// </summary>
        private void ComposePage5_DetailedTrackAnalysesAndCourses(
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
