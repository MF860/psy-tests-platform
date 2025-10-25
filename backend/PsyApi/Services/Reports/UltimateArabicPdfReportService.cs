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
    /// خدمة التقارير النفسية العربية الاحترافية - إصدار نهائي متقدم
    /// تقرير شامل 4-6 صفحات بـ Vector Charts + شعار + سرد عربي غني
    /// بدون أي اتصال خارجي - كل التحليل داخلي
    /// </summary>
    public class UltimateArabicPdfReportService : IPdfReportService
    {
        private readonly IRecommendationService _recommendationService;
        private static bool _fontsRegistered;
        private static readonly object _lock = new();
        private static byte[]? _logoBytes;

        public UltimateArabicPdfReportService(IRecommendationService recommendationService)
        {
            _recommendationService = recommendationService;
            EnsureFonts();
            LoadLogo();
        }

        public Task<byte[]> RenderResultPdfAsync(
            Result result, 
            User user, 
            IEnumerable<DimensionScoreDto> dimensions, 
            CancellationToken ct = default)
        {
            var startTime = DateTime.Now;

            try
            {
                Console.WriteLine($"\n{'═',60}");
                Console.WriteLine($"🎯 ULTIMATE ARABIC PSYCHOMETRIC REPORT");
                Console.WriteLine($"{'═',60}");
                Console.WriteLine($"👤 User: {user.FullName ?? "غير محدد"} (ID: {user.NationalId ?? "N/A"})");
                Console.WriteLine($"📋 Session: {result.SessionId}");
                Console.WriteLine($"🕐 Time: {startTime:yyyy-MM-dd HH:mm:ss}");

                // التهيئة
                Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("ar-JO");
                QuestPDF.Settings.License = LicenseType.Community;
                QuestPDF.Settings.EnableDebugging = false;

                // تحضير البيانات
                var dimensionsList = dimensions.OrderBy(d => d.T).ToList();
                if (dimensionsList.Count == 0)
                    throw new InvalidOperationException("No dimensions provided");

                // التحقق من صحة البيانات
                foreach (var d in dimensionsList)
                {
                    if (double.IsNaN(d.T) || double.IsInfinity(d.T))
                        throw new InvalidOperationException($"Invalid T-score for {d.Dimension}");
                }

                // حساب الإحصائيات
                var avgTScore = dimensionsList.Average(d => d.T);
                var avgPercentile = dimensionsList.Average(d => d.Percentile);
                var totalScore = (int)Math.Round(dimensionsList.Sum(d => d.T));

                // Band counts (Excellent ≥65, Good 55-64, Average 40-54, Weak <40)
                var excellentCount = dimensionsList.Count(d => d.T >= 65);
                var goodCount = dimensionsList.Count(d => d.T >= 55 && d.T < 65);
                var averageCount = dimensionsList.Count(d => d.T >= 40 && d.T < 55);
                var weakCount = dimensionsList.Count(d => d.T < 40);

                // أقوى وأضعف الأبعاد
                var strongestDimensions = dimensionsList.OrderByDescending(d => d.T).Take(5).ToList();
                var weakestDimensions = dimensionsList.Take(5).ToList();

                // تجميع حسب المحاور
                var clusterGroups = ReportAnalytics.GroupDimensionsByClusters(dimensionsList);

                // التوصيات
                var recommendations = _recommendationService.GetDimensionRecommendations(weakestDimensions);
                var courses = _recommendationService.GetSuggestedCourses(weakestDimensions);
                var actionPlan = ReportAnalytics.GenerateActionPlan(weakestDimensions, clusterGroups);

                // توليد الرؤى
                var insights = ReportAnalytics.GenerateInsights(
                    dimensionsList, excellentCount, goodCount, averageCount, weakCount);
                var riskFlags = ReportAnalytics.RiskFlags(dimensionsList).ToList();

                // Logging مفصل
                Console.WriteLine($"\n📊 STATISTICS:");
                Console.WriteLine($"   Dimensions: {dimensionsList.Count}");
                Console.WriteLine($"   Excellent (≥65): {excellentCount}");
                Console.WriteLine($"   Good (55-64): {goodCount}");
                Console.WriteLine($"   Average (40-54): {averageCount}");
                Console.WriteLine($"   Weak (<40): {weakCount}");
                Console.WriteLine($"   Avg T-Score: {ReportTheme.FormatNum(avgTScore, 1)}");
                Console.WriteLine($"   Avg Percentile: {ReportTheme.FormatNum(avgPercentile, 0)}");
                Console.WriteLine($"   Total Score: {totalScore}");

                // توليد PDF
                var pdf = Document.Create(container =>
                {
                    // صفحة 1: الغلاف والملخص
                    container.Page(page =>
                    {
                        ConfigurePageDefaults(page);
                        ComposePage1_CoverAndSummary(
                            page, user, result, avgTScore, avgPercentile, totalScore,
                            strongestDimensions, weakestDimensions, dimensionsList.Count);
                    });

                    // صفحة 2: الرسوم التوضيحية (Charts)
                    container.Page(page =>
                    {
                        ConfigurePageDefaults(page);
                        ComposePage2_ChartsOverview(
                            page, dimensionsList, clusterGroups, 
                            excellentCount, goodCount, averageCount, weakCount);
                    });

                    // صفحة 3: التحليل والسرد العربي
                    container.Page(page =>
                    {
                        ConfigurePageDefaults(page);
                        ComposePage3_AnalysisAndNarrative(
                            page, dimensionsList, clusterGroups, insights, riskFlags);
                    });

                    // صفحة 4: خطة التطوير والتوصيات
                    container.Page(page =>
                    {
                        ConfigurePageDefaults(page);
                        ComposePage4_ActionPlanAndRecommendations(
                            page, weakestDimensions, actionPlan, recommendations);
                    });

                    // صفحة 5 (اختيارية): الدورات التدريبية
                    if (courses.Any())
                    {
                        container.Page(page =>
                        {
                            ConfigurePageDefaults(page);
                            ComposePage5_TrainingCourses(page, courses, weakestDimensions);
                        });
                    }
                });

                var pdfBytes = pdf.GeneratePdf();
                var endTime = DateTime.Now;
                var duration = endTime - startTime;

                var pageCount = 4 + (courses.Any() ? 1 : 0);

                Console.WriteLine($"\n{'─',60}");
                Console.WriteLine($"✅ PDF GENERATION COMPLETE");
                Console.WriteLine($"{'─',60}");
                Console.WriteLine($"📄 Pages: {pageCount}");
                Console.WriteLine($"📏 Size: {pdfBytes.Length / 1024:F1} KB");
                Console.WriteLine($"⏱ Duration: {duration.TotalMilliseconds:F0}ms");
                Console.WriteLine($"🎯 Font: Noto Naskh Arabic with HarfBuzz");
                Console.WriteLine($"📊 Charts: Radar + Bar + Donut (100% Vector)");
                Console.WriteLine($"🔢 Numbers: Western digits (no � glyphs)");
                Console.WriteLine($"{'═',60}\n");

                return Task.FromResult(pdfBytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ PDF GENERATION FAILED: {ex.Message}");
                Console.WriteLine($"   Stack: {ex.StackTrace}");
                throw;
            }
        }

        #region Page Configuration

        private void ConfigurePageDefaults(PageDescriptor page)
        {
            page.Size(PageSizes.A4);
            page.Margin(ReportTheme.Spacing.PageMargin);
            page.DefaultTextStyle(style => ReportTheme.ArabicTextStyle(11, false, ReportTheme.Colors.Text));
        }

        #endregion

        #region Page 1: Cover & Summary

        private void ComposePage1_CoverAndSummary(
            PageDescriptor page,
            User user,
            Result result,
            double avgTScore,
            double avgPercentile,
            int totalScore,
            List<DimensionScoreDto> strongestDimensions,
            List<DimensionScoreDto> weakestDimensions,
            int totalDimensions)
        {
            page.Content().Column(column =>
            {
                // Header Section: Logo + Title (vNext specs: 60-72px logo + 18-20pt title)
                column.Item().PaddingTop(8).Column(header =>
                {
                    // الشعار (إذا كان موجوداً) - مركّز بعرض 66px (mid-range of 60-72px)
                    if (_logoBytes != null && _logoBytes.Length > 0)
                    {
                        header.Item().AlignCenter().Width(66).Image(_logoBytes);
                        header.Item().PaddingTop(8); // 8pt spacing after logo
                    }

                    // العنوان الرئيسي - بارز وواضح (18-20pt Bold as per specs)
                    header.Item().AlignCenter().Text("منصة التحليل النفسي المتقدم")
                        .Style(ReportTheme.ArabicTextStyle(20, true, "#1F2937")); // 20pt Bold Dark Gray

                    header.Item().PaddingTop(12); // 12pt spacing after title
                });

                column.Item().PaddingTop(16); // 16pt margin before user info

                column.Item().PaddingTop(ReportTheme.Spacing.LG);

                // معلومات المستخدم (شبكة 2×2)
                column.Item().Background(ReportTheme.Colors.Surface)
                    .Padding(ReportTheme.Spacing.MD)
                    .Column(infoGrid =>
                    {
                        infoGrid.Item().Row(row1 =>
                        {
                            var displayName = ArabicTextRenderer.FormatDimensionName(
                                user.FullName ?? "غير محدد", 35);
                            var nationalId = user.NationalId ?? "غير محدد";

                            row1.RelativeItem().Text($"الاسم: {displayName}")
                                .Style(ReportTheme.ArabicTextStyle(12));
                            row1.RelativeItem().Text($"الرقم الوطني: {nationalId}")
                                .Style(ReportTheme.ArabicTextStyle(12));
                        });

                        infoGrid.Item().PaddingTop(ReportTheme.Spacing.SM).Row(row2 =>
                        {
                            var sessionIdStr = result.SessionId.ToString();
                            var sessionDisplay = sessionIdStr.Length > 12 
                                ? sessionIdStr[..12] + "..." 
                                : sessionIdStr;
                            var reportDate = DateTime.Now.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);

                            row2.RelativeItem().Text($"رقم الجلسة: {sessionDisplay}")
                                .Style(ReportTheme.ArabicTextStyle(12));
                            row2.RelativeItem().Text($"التاريخ: {reportDate}")
                                .Style(ReportTheme.ArabicTextStyle(12));
                        });
                    });

                column.Item().PaddingTop(ReportTheme.Spacing.LG);

                // مؤشرات الأداء (KPIs)
                column.Item().AlignCenter().Text("مؤشرات الأداء العامة")
                    .Style(ReportTheme.ArabicTextStyle(14, true));

                column.Item().PaddingTop(ReportTheme.Spacing.SM).Row(kpiRow =>
                {
                    var avgTBandColor = ReportTheme.GetBandColor(avgTScore);

                    // متوسط T-Score
                    kpiRow.RelativeItem().Padding(ReportTheme.Spacing.SM)
                        .Background(ReportTheme.Colors.Surface)
                        .Border(2).BorderColor(avgTBandColor)
                        .AlignCenter()
                        .Column(kpi =>
                        {
                            kpi.Item().Text(ReportTheme.FormatNum(avgTScore, 1))
                                .Style(TextStyle.Default.FontSize(20).FontColor(avgTBandColor).Bold());
                            kpi.Item().Text("متوسط T-Score")
                                .Style(ReportTheme.ArabicTextStyle(9, false, ReportTheme.Colors.TextSecondary));
                        });

                    // متوسط المئيني
                    kpiRow.RelativeItem().Padding(ReportTheme.Spacing.SM)
                        .Background(ReportTheme.Colors.Surface)
                        .Border(1).BorderColor(ReportTheme.Colors.Border)
                        .AlignCenter()
                        .Column(kpi =>
                        {
                            kpi.Item().Text(ReportTheme.FormatNum(avgPercentile, 0))
                                .Style(TextStyle.Default.FontSize(20).FontColor(ReportTheme.Colors.Primary).Bold());
                            kpi.Item().Text("المئيني المتوسط")
                                .Style(ReportTheme.ArabicTextStyle(9, false, ReportTheme.Colors.TextSecondary));
                        });

                    // عدد الأبعاد
                    kpiRow.RelativeItem().Padding(ReportTheme.Spacing.SM)
                        .Background(ReportTheme.Colors.Surface)
                        .Border(1).BorderColor(ReportTheme.Colors.Border)
                        .AlignCenter()
                        .Column(kpi =>
                        {
                            kpi.Item().Text(totalDimensions.ToString())
                                .Style(TextStyle.Default.FontSize(20).FontColor(ReportTheme.Colors.Text).Bold());
                            kpi.Item().Text("عدد الأبعاد")
                                .Style(ReportTheme.ArabicTextStyle(9, false, ReportTheme.Colors.TextSecondary));
                        });
                });

                column.Item().PaddingTop(ReportTheme.Spacing.LG);

                // أقوى 3 أبعاد
                if (strongestDimensions.Any())
                {
                    column.Item().AlignRight().Text("⭐ أقوى 3 أبعاد")
                        .Style(ReportTheme.ArabicTextStyle(13, true, ReportTheme.Colors.Excellent));

                    column.Item().PaddingTop(ReportTheme.Spacing.SM).AlignCenter().Row(strengthRow =>
                    {
                        foreach (var strength in strongestDimensions.Take(3))
                        {
                            var dimensionName = ArabicTextRenderer.FormatDimensionName(strength.Dimension, 18);
                            strengthRow.RelativeItem().Padding(ReportTheme.Spacing.SM)
                                .Background(ReportTheme.Colors.Excellent)
                                .AlignCenter()
                                .Column(chip =>
                                {
                                    chip.Item().Text(dimensionName)
                                        .Style(ReportTheme.ArabicTextStyle(10, false, "#FFFFFF"));
                                    chip.Item().Text($"T={ReportTheme.FormatNum(strength.T, 1)}")
                                        .Style(TextStyle.Default.FontSize(8).FontColor("#FFFFFF"));
                                });
                        }
                    });

                    column.Item().PaddingTop(ReportTheme.Spacing.MD);
                }

                // أضعف 3 أبعاد
                if (weakestDimensions.Any())
                {
                    column.Item().AlignRight().Text("⚠️ أبعاد تحتاج تطوير")
                        .Style(ReportTheme.ArabicTextStyle(13, true, ReportTheme.Colors.Weak));

                    column.Item().PaddingTop(ReportTheme.Spacing.SM).AlignCenter().Row(growthRow =>
                    {
                        foreach (var weakness in weakestDimensions.Take(3))
                        {
                            var dimensionName = ArabicTextRenderer.FormatDimensionName(weakness.Dimension, 18);
                            var bandColor = ReportTheme.GetBandColor(weakness.T);

                            growthRow.RelativeItem().Padding(ReportTheme.Spacing.SM)
                                .Background(bandColor)
                                .AlignCenter()
                                .Column(chip =>
                                {
                                    chip.Item().Text(dimensionName)
                                        .Style(ReportTheme.ArabicTextStyle(10, false, "#FFFFFF"));
                                    chip.Item().Text($"T={ReportTheme.FormatNum(weakness.T, 1)}")
                                        .Style(TextStyle.Default.FontSize(8).FontColor("#FFFFFF"));
                                });
                        }
                    });
                }
            });

            // تذييل الصفحة
            page.Footer().Column(footer =>
            {
                footer.Item().PaddingBottom(8).AlignCenter()
                    .Text("تم إنشاء هذا التقرير بواسطة منصة التحليل النفسي المتقدم © 2025")
                    .Style(ReportTheme.ArabicTextStyle(9, false, "#6B7280"));
                
                footer.Item().AlignCenter().Text("صفحة 1")
                    .Style(ReportTheme.ArabicTextStyle(8, false, ReportTheme.Colors.TextSecondary));
            });
        }

        #endregion

        #region Page 2: Charts Overview

        private void ComposePage2_ChartsOverview(
            PageDescriptor page,
            List<DimensionScoreDto> sortedDimensions,
            Dictionary<string, List<DimensionScoreDto>> clusterGroups,
            int excellentCount,
            int goodCount,
            int averageCount,
            int weakCount)
        {
            // vNext: Light gray background with improved spacing
            page.Content().Background("#F9FAFB").Padding(20).Column(column =>
            {
                // العنوان (14-16pt Bold as per specs)
                column.Item().AlignRight().Text("📊 النتائج عبر الأبعاد والمحاور")
                    .Style(ReportTheme.ArabicTextStyle(16, true));

                column.Item().PaddingTop(16);

                // مخطط الرادار (160-180px radius effective = ~360px total)
                column.Item().Background("#FFFFFF").Padding(20).Column(radarCard =>
                {
                    try
                    {
                        var radarBytes = RadarChartRenderer.RenderRadarChart(
                            sortedDimensions.Take(12), // max 12 dimensions
                            size: 400, // 200px radius = 180px effective with padding
                            showGrid: true);

                        radarCard.Item().AlignCenter().MinHeight(200).MaxHeight(220).Image(radarBytes);
                        radarCard.Item().PaddingTop(8).AlignCenter()
                            .Text("مخطط رادار شامل لتوزيع الأبعاد")
                            .Style(ReportTheme.ArabicTextStyle(10, false, ReportTheme.Colors.TextSecondary));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Charts] Radar rendering error: {ex.Message}");
                        radarCard.Item().AlignCenter().Text("خطأ في رسم مخطط الرادار")
                            .Style(ReportTheme.ArabicTextStyle(10, false, "#E53935"));
                    }
                });

                column.Item().PaddingTop(12);
                
                // Subtle divider
                column.Item().Height(1).Background("#E5E7EB");
                
                column.Item().PaddingTop(12);

                // مخطط الأعمدة الأفقي (vNext: 520-600px, horizontal bars, RTL labels)
                column.Item().Background("#FFFFFF").Padding(20).Column(barCard =>
                {
                    try
                    {
                        var barBytes = HorizontalBarChartRenderer.RenderHorizontalBars(
                            sortedDimensions,
                            width: 580, // 520-600px as per specs
                            maxDimensions: 12);

                        barCard.Item().AlignCenter().MinHeight(220).MaxHeight(350).Image(barBytes);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Charts] Horizontal Bar rendering error: {ex.Message}");
                        barCard.Item().AlignCenter().Text("خطأ في رسم مخطط الأعمدة")
                            .Style(ReportTheme.ArabicTextStyle(10, false, "#E53935"));
                    }
                });

                column.Item().PaddingTop(12);
                
                // Subtle divider
                column.Item().Height(1).Background("#E5E7EB");
                
                column.Item().PaddingTop(12);

                // مخطط الدونات (vNext: 150-170px diameter)
                column.Item().Background("#FFFFFF").Padding(20).Column(donutCard =>
                {
                    try
                    {
                        var clusterDistribution = DonutChartRenderer.CalculateClusterDistribution(sortedDimensions);
                        
                        if (clusterDistribution.Any())
                        {
                            var donutBytes = DonutChartRenderer.RenderClusterDonut(
                                clusterDistribution,
                                size: 160, // 150-170px diameter as per specs
                                centerText: "المحاور");

                            donutCard.Item().AlignCenter().MinHeight(140).MaxHeight(180).Image(donutBytes);
                            donutCard.Item().PaddingTop(8).AlignCenter()
                                .Text("توزيع متوسط T-Score حسب المحاور الأربعة")
                                .Style(ReportTheme.ArabicTextStyle(10, false, ReportTheme.Colors.TextSecondary));
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Charts] Donut rendering error: {ex.Message}");
                        donutCard.Item().AlignCenter().Text("خطأ في رسم مخطط الدونات")
                            .Style(ReportTheme.ArabicTextStyle(10, false, "#E53935"));
                    }
                });

                column.Item().PaddingTop(16);

                // ملخص إحصائي (white background with better spacing)
                column.Item().Background("#FFFFFF").Padding(16)
                    .Row(summaryRow =>
                    {
                        summaryRow.RelativeItem().AlignRight()
                            .Text($"ممتاز: {excellentCount} | جيد: {goodCount} | متوسط: {averageCount} | ضعيف: {weakCount}")
                            .Style(ReportTheme.ArabicTextStyle(12, true)); // 11-12pt as per specs
                    });
            });

            page.Footer().Column(footer =>
            {
                footer.Item().PaddingBottom(8).AlignCenter()
                    .Text("تم إنشاء هذا التقرير بواسطة منصة التحليل النفسي المتقدم © 2025")
                    .Style(ReportTheme.ArabicTextStyle(9, false, "#6B7280"));
                
                footer.Item().AlignCenter().Text("صفحة 2")
                    .Style(ReportTheme.ArabicTextStyle(8, false, ReportTheme.Colors.TextSecondary));
            });
        }

        #endregion

        #region Page 3: Analysis & Narrative

        private void ComposePage3_AnalysisAndNarrative(
            PageDescriptor page,
            List<DimensionScoreDto> dimensions,
            Dictionary<string, List<DimensionScoreDto>> clusterGroups,
            List<string> insights,
            List<string> riskFlags)
        {
            page.Content().Column(column =>
            {
                // العنوان
                column.Item().AlignRight().Text("📖 التحليل والسرد التفصيلي")
                    .Style(ReportTheme.ArabicTextStyle(18, true));

                column.Item().PaddingTop(ReportTheme.Spacing.MD);

                // الرؤى العامة
                if (insights.Any())
                {
                    column.Item().AlignRight().Text("💡 رؤى عامة")
                        .Style(ReportTheme.ArabicTextStyle(14, true, ReportTheme.Colors.Primary));

                    column.Item().PaddingTop(ReportTheme.Spacing.SM);

                    foreach (var insight in insights)
                    {
                        column.Item().PaddingVertical(ReportTheme.Spacing.XS)
                            .AlignRight()
                            .Text($"• {insight}")
                            .Style(ReportTheme.ArabicTextStyle(10, false, ReportTheme.Colors.Text));
                    }

                    column.Item().PaddingTop(ReportTheme.Spacing.MD);
                }

                // تحليل المحاور
                column.Item().AlignRight().Text("🎯 تحليل المحاور")
                    .Style(ReportTheme.ArabicTextStyle(14, true, ReportTheme.Colors.Primary));

                column.Item().PaddingTop(ReportTheme.Spacing.SM);

                foreach (var (cluster, clusterDims) in clusterGroups)
                {
                    if (cluster == "OTHER" || !clusterDims.Any()) continue;

                    var analysis = ReportAnalytics.AnalyzeCluster(cluster, clusterDims);

                    column.Item().PaddingVertical(ReportTheme.Spacing.SM)
                        .Background(ReportTheme.Colors.Surface)
                        .Padding(ReportTheme.Spacing.MD)
                        .Column(clusterCard =>
                        {
                            var clusterName = ReportAnalytics.ClusterNames.GetValueOrDefault(cluster, cluster);
                            
                            clusterCard.Item().AlignRight().Text(clusterName)
                                .Style(ReportTheme.ArabicTextStyle(13, true, ReportTheme.Colors.Text));

                            clusterCard.Item().PaddingTop(ReportTheme.Spacing.XS)
                                .AlignRight()
                                .Text(analysis)
                                .Style(ReportTheme.ArabicTextStyle(10, false, ReportTheme.Colors.Text));

                            // أبرز 3 أبعاد في هذا المحور
                            var topDims = clusterDims.OrderByDescending(d => d.T).Take(3);
                            clusterCard.Item().PaddingTop(ReportTheme.Spacing.SM)
                                .Row(dimRow =>
                                {
                                    foreach (var dim in topDims)
                                    {
                                        var dimName = ArabicTextRenderer.FormatDimensionName(dim.Dimension, 15);
                                        var bandColor = ReportTheme.GetBandColor(dim.T);

                                        dimRow.RelativeItem().Padding(4)
                                            .Background(bandColor)
                                            .AlignCenter()
                                            .Text($"{dimName} ({ReportTheme.FormatNum(dim.T, 1)})")
                                            .Style(TextStyle.Default.FontSize(8).FontColor("#FFFFFF"));
                                    }
                                });
                        });
                }

                column.Item().PaddingTop(ReportTheme.Spacing.LG);

                // مؤشرات المخاطرة
                if (riskFlags.Any())
                {
                    column.Item().AlignRight().Text("⚠️ مؤشرات تحتاج انتباه")
                        .Style(ReportTheme.ArabicTextStyle(14, true, ReportTheme.Colors.Weak));

                    column.Item().PaddingTop(ReportTheme.Spacing.SM);

                    foreach (var flag in riskFlags.Take(5)) // حد أقصى 5 تنبيهات
                    {
                        column.Item().PaddingVertical(ReportTheme.Spacing.XS)
                            .Background("#FFF3CD")
                            .Padding(ReportTheme.Spacing.SM)
                            .AlignRight()
                            .Text(flag)
                            .Style(ReportTheme.ArabicTextStyle(9, false, "#856404"));
                    }
                }
            });

            page.Footer().Column(footer =>
            {
                footer.Item().PaddingBottom(8).AlignCenter()
                    .Text("تم إنشاء هذا التقرير بواسطة منصة التحليل النفسي المتقدم © 2025")
                    .Style(ReportTheme.ArabicTextStyle(9, false, "#6B7280"));
                
                footer.Item().AlignCenter().Text("صفحة 3")
                    .Style(ReportTheme.ArabicTextStyle(8, false, ReportTheme.Colors.TextSecondary));
            });
        }

        #endregion

        #region Page 4: Action Plan

        private void ComposePage4_ActionPlanAndRecommendations(
            PageDescriptor page,
            List<DimensionScoreDto> weakestDimensions,
            List<ActionItem> actionPlan,
            List<DimensionRecommendation> recommendations)
        {
            page.Content().Column(column =>
            {
                // العنوان
                column.Item().AlignRight().Text("🎯 خطة التطوير والتوصيات العملية")
                    .Style(ReportTheme.ArabicTextStyle(18, true));

                column.Item().PaddingTop(ReportTheme.Spacing.MD);

                // مقدمة
                column.Item().AlignRight()
                    .Text("هذه الخطة مصممة خصيصاً لتطوير الأبعاد الأضعف. يُنصح بالالتزام بها لمدة 6-8 أسابيع مع متابعة دورية.")
                    .Style(ReportTheme.ArabicTextStyle(10, false, ReportTheme.Colors.TextSecondary));

                column.Item().PaddingTop(ReportTheme.Spacing.MD);

                // خطة العمل
                if (actionPlan.Any())
                {
                    foreach (var action in actionPlan)
                    {
                        column.Item().PaddingVertical(ReportTheme.Spacing.SM)
                            .Border(1).BorderColor(ReportTheme.Colors.Border)
                            .Padding(ReportTheme.Spacing.MD)
                            .Column(actionCard =>
                            {
                                // العنوان والأولوية
                                actionCard.Item().Row(headerRow =>
                                {
                                    headerRow.RelativeItem(3).AlignRight().Text(action.Title)
                                        .Style(ReportTheme.ArabicTextStyle(12, true));

                                    var priorityColor = action.Priority == "عالية" 
                                        ? ReportTheme.Colors.Weak
                                        : action.Priority == "متوسطة"
                                        ? ReportTheme.Colors.Average
                                        : ReportTheme.Colors.Excellent;

                                    headerRow.RelativeItem(1).AlignRight()
                                        .Padding(4)
                                        .Background(priorityColor)
                                        .AlignCenter()
                                        .Text(action.Priority)
                                        .Style(TextStyle.Default.FontSize(9).FontColor("#FFFFFF"));
                                });

                                // الوصف
                                actionCard.Item().PaddingTop(ReportTheme.Spacing.SM)
                                    .AlignRight()
                                    .Text(action.Description)
                                    .Style(ReportTheme.ArabicTextStyle(10));

                                // الإطار الزمني والـ KPI
                                actionCard.Item().PaddingTop(ReportTheme.Spacing.SM)
                                    .Row(footerRow =>
                                    {
                                        if (!string.IsNullOrEmpty(action.Timeline))
                                        {
                                            footerRow.RelativeItem().AlignRight()
                                                .Text($"⏰ {action.Timeline}")
                                                .Style(ReportTheme.ArabicTextStyle(9, false, ReportTheme.Colors.TextSecondary));
                                        }

                                        if (!string.IsNullOrEmpty(action.KPI))
                                        {
                                            footerRow.RelativeItem().AlignRight()
                                                .Text($"📊 {action.KPI}")
                                                .Style(ReportTheme.ArabicTextStyle(9, false, ReportTheme.Colors.Primary));
                                        }
                                    });
                            });
                    }
                }

                column.Item().PaddingTop(ReportTheme.Spacing.LG);

                // نصائح إضافية
                column.Item().Background("#E7F3FF")
                    .Padding(ReportTheme.Spacing.MD)
                    .Column(tipsBox =>
                    {
                        tipsBox.Item().AlignRight().Text("💡 نصائح للنجاح")
                            .Style(ReportTheme.ArabicTextStyle(12, true, ReportTheme.Colors.Primary));

                        var tips = new[]
                        {
                            "التزم بالتمارين اليومية حتى لو كانت قصيرة (15-20 دقيقة)",
                            "سجّل تقدمك أسبوعياً واحتفل بالإنجازات الصغيرة",
                            "اطلب التغذية الراجعة من الأقران أو المشرفين",
                            "راجع هذا التقرير شهرياً لتقييم التحسن",
                            "لا تتردد في طلب المساعدة المهنية عند الحاجة"
                        };

                        foreach (var tip in tips)
                        {
                            tipsBox.Item().PaddingVertical(ReportTheme.Spacing.XS)
                                .AlignRight()
                                .Text($"✓ {tip}")
                                .Style(ReportTheme.ArabicTextStyle(9, false, ReportTheme.Colors.Text));
                        }
                    });
            });

            page.Footer().Column(footer =>
            {
                footer.Item().PaddingBottom(8).AlignCenter()
                    .Text("تم إنشاء هذا التقرير بواسطة منصة التحليل النفسي المتقدم © 2025")
                    .Style(ReportTheme.ArabicTextStyle(9, false, "#6B7280"));
                
                footer.Item().AlignCenter().Text("صفحة 4")
                    .Style(ReportTheme.ArabicTextStyle(8, false, ReportTheme.Colors.TextSecondary));
            });
        }

        #endregion

        #region Page 5: Training Courses (Optional)

        private void ComposePage5_TrainingCourses(
            PageDescriptor page,
            List<CourseSuggestion> courses,
            List<DimensionScoreDto> weakestDimensions)
        {
            page.Content().Column(column =>
            {
                // العنوان
                column.Item().AlignRight().Text("📚 الدورات التدريبية المقترحة")
                    .Style(ReportTheme.ArabicTextStyle(18, true));

                column.Item().PaddingTop(ReportTheme.Spacing.SM).AlignRight()
                    .Text("دورات مُوصى بها بناءً على الأبعاد التي تحتاج تطوير:")
                    .Style(ReportTheme.ArabicTextStyle(10, false, ReportTheme.Colors.TextSecondary));

                column.Item().PaddingTop(ReportTheme.Spacing.MD);

                // الدورات
                var filteredCourses = courses
                    .Where(c => weakestDimensions.Any(w => c.TargetDimensions?.Contains(w.Dimension) == true))
                    .OrderBy(c => c.Duration ?? "zzz")
                    .Take(6) // حد أقصى 6 دورات
                    .ToList();

                foreach (var course in filteredCourses)
                {
                    column.Item().PaddingVertical(ReportTheme.Spacing.SM)
                        .Border(1).BorderColor(ReportTheme.Colors.Border)
                        .Background(ReportTheme.Colors.Surface)
                        .Padding(ReportTheme.Spacing.MD)
                        .Column(courseCard =>
                        {
                            // اسم الدورة
                            courseCard.Item().AlignRight().Text(course.Name)
                                .Style(ReportTheme.ArabicTextStyle(12, true, ReportTheme.Colors.Primary));

                            // المدة
                            if (!string.IsNullOrEmpty(course.Duration))
                            {
                                courseCard.Item().PaddingTop(ReportTheme.Spacing.XS)
                                    .AlignRight()
                                    .Text($"⏱ المدة: {course.Duration}")
                                    .Style(ReportTheme.ArabicTextStyle(9, false, ReportTheme.Colors.TextSecondary));
                            }

                            // الوصف
                            if (!string.IsNullOrEmpty(course.Description))
                            {
                                var description = ArabicTextRenderer.FormatDescription(course.Description, 150);
                                courseCard.Item().PaddingTop(ReportTheme.Spacing.SM)
                                    .AlignRight()
                                    .Text(description)
                                    .Style(ReportTheme.ArabicTextStyle(10));
                            }

                            // الأبعاد المستهدفة
                            var targetedWeakDimensions = course.TargetDimensions?
                                .Where(td => weakestDimensions.Any(wd => wd.Dimension == td))
                                .Take(3)
                                .ToList();

                            if (targetedWeakDimensions?.Any() == true)
                            {
                                courseCard.Item().PaddingTop(ReportTheme.Spacing.SM)
                                    .AlignRight()
                                    .Row(tagRow =>
                                    {
                                        tagRow.AutoItem().Text("يستهدف: ")
                                            .Style(ReportTheme.ArabicTextStyle(9, false, ReportTheme.Colors.TextSecondary));

                                        foreach (var target in targetedWeakDimensions)
                                        {
                                            tagRow.AutoItem().Padding(2)
                                                .Background(ReportTheme.Colors.Primary)
                                                .Padding(4)
                                                .Text(target)
                                                .Style(TextStyle.Default.FontSize(8).FontColor("#FFFFFF"));
                                        }
                                    });
                            }
                        });
                }

                // ملاحظة ختامية
                column.Item().PaddingTop(ReportTheme.Spacing.LG)
                    .AlignRight()
                    .Text("💡 يُنصح بالتسجيل في دورة واحدة أو دورتين في نفس الوقت لتجنب الإرهاق، مع التركيز على التطبيق العملي.")
                    .Style(ReportTheme.ArabicTextStyle(9, false, ReportTheme.Colors.TextSecondary));
            });

            page.Footer().Column(footer =>
            {
                footer.Item().PaddingBottom(8).AlignCenter()
                    .Text("تم إنشاء هذا التقرير بواسطة منصة التحليل النفسي المتقدم © 2025")
                    .Style(ReportTheme.ArabicTextStyle(9, false, "#6B7280"));
                
                footer.Item().AlignCenter().Text("صفحة 5")
                    .Style(ReportTheme.ArabicTextStyle(8, false, ReportTheme.Colors.TextSecondary));
            });
        }

        #endregion

        #region Font & Logo Loading

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
                        Console.WriteLine("[UltimatePdfService] ✓ Noto Naskh Arabic Regular loaded");
                    }

                    if (File.Exists(boldPath))
                    {
                        QuestPDF.Drawing.FontManager.RegisterFont(File.OpenRead(boldPath));
                        Console.WriteLine("[UltimatePdfService] ✓ Noto Naskh Arabic Bold loaded");
                    }

                    _fontsRegistered = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[UltimatePdfService] ⚠ Font loading error: {ex.Message}");
                    _fontsRegistered = false;
                }
            }
        }

        private static void LoadLogo()
        {
            try
            {
                // Try multiple logo variations (vNext: prioritize SAITES-ICON.png)
                var logoOptions = new[]
                {
                    "SAITES-ICON.png",
                    "SAITEST.jpeg",
                    "SITES-ICON.png"
                };
                
                foreach (var logoFile in logoOptions)
                {
                    var logoPath = Path.Combine(AppContext.BaseDirectory, "Resources", "Brand", logoFile);
                    if (File.Exists(logoPath))
                    {
                        _logoBytes = File.ReadAllBytes(logoPath);
                        Console.WriteLine($"[UltimatePdfService] ✓ Logo loaded: {logoFile}");
                        return;
                    }
                }
                
                Console.WriteLine($"[UltimatePdfService] ℹ Logo not found in Resources/Brand/");
                _logoBytes = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UltimatePdfService] ⚠ Logo loading error: {ex.Message}");
                _logoBytes = null;
            }
        }

        #endregion
    }
}
