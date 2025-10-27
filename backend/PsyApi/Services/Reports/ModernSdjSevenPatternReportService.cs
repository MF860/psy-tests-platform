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
                    // Page 1: Cover with participant info, logo, and 7-pattern summary
                    document.Page(page =>
                    {
                        ConfigurePageDefaults(page);
                        ComposePage1_CoverAndSevenPatternSummary(page, user, result, patterns);
                    });

                    // Page 2: Heptagon chart + horizontal bar chart for dimensions
                    document.Page(page =>
                    {
                        ConfigurePageDefaults(page);
                        ComposePage2_ChartsAndVisualizations(page, patterns, sdjData.SubDimensions);
                    });

                    // Page 3: Weak sub-dimensions table with course recommendations
                    document.Page(page =>
                    {
                        ConfigurePageDefaults(page);
                        ComposePage3_WeakAreasAndCourses(page, courseRecommendations);
                    });
                });

                var pdfBytes = pdf.GeneratePdf();
                var endTime = DateTime.Now;
                var duration = endTime - startTime;

                Console.WriteLine($"\n✅ SDJ 7-PATTERN PDF COMPLETE");
                Console.WriteLine($"   Pages: 3");
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
        /// Page 1: Cover with centered logo, Arabic title, participant info, and 7-pattern summary
        /// </summary>
        private void ComposePage1_CoverAndSevenPatternSummary(
            PageDescriptor page,
            User user,
            Result result,
            List<SevenPatternScore> patterns)
        {
            page.Content().Column(column =>
            {
                // Centered logo at top (SAITEST-ICON.png)
                if (_logoBytes != null && _logoBytes.Length > 0)
                {
                    column.Item()
                        .AlignCenter()
                        .PaddingBottom(16)
                        .Width(120)
                        .Height(120)
                        .Image(_logoBytes);
                }

                // Main title: منصة التحليل النفسي المتقدم (big, bold)
                column.Item().AlignCenter()
                    .Text("منصة التحليل النفسي المتقدم")
                    .Style(ReportTheme.ArabicTextStyle(24, true, "#1e40af"));

                // Arabic subtitle (short, inspiring phrase)
                column.Item().PaddingTop(8).AlignCenter()
                    .Text("تقرير الأنماط السباعية للتحليل النفسي الشامل")
                    .Style(ReportTheme.ArabicTextStyle(12, false, "#6b7280"));

                column.Item().PaddingTop(32);

                // Participant Info Block (improved spacing & typography)
                column.Item()
                    .Background("#f9fafb")
                    .Border(1)
                    .BorderColor("#e5e7eb")
                    .Padding(20)
                    .Column(info =>
                    {
                        info.Item().Text("معلومات المشارك")
                            .Style(ReportTheme.ArabicTextStyle(16, true, "#111827"));
                        
                        info.Item().PaddingTop(12).Row(row =>
                        {
                            row.RelativeItem().Text($"الاسم: {user.FullName ?? "غير محدد"}")
                                .Style(ReportTheme.ArabicTextStyle(12, false, "#374151"));
                            row.RelativeItem().Text($"الرقم الوطني: {user.NationalId ?? "N/A"}")
                                .Style(ReportTheme.ArabicTextStyle(12, false, "#374151"));
                        });
                        
                        info.Item().PaddingTop(8).Row(row =>
                        {
                            var sessionDisplay = result.SessionId.ToString();
                            if (sessionDisplay.Length > 12) sessionDisplay = sessionDisplay[..12];
                            
                            row.RelativeItem().Text($"رقم الجلسة: {sessionDisplay}")
                                .Style(ReportTheme.ArabicTextStyle(12, false, "#374151"));
                            row.RelativeItem().Text($"التاريخ: {result.CreatedAt:dd-MM-yyyy}")
                                .Style(ReportTheme.ArabicTextStyle(12, false, "#374151"));
                        });
                        
                        if (!string.IsNullOrEmpty(user.Email))
                        {
                            info.Item().PaddingTop(8).Text($"البريد الإلكتروني: {user.Email}")
                                .Style(ReportTheme.ArabicTextStyle(11, false, "#6b7280"));
                        }
                    });

                column.Item().PaddingTop(32);

                // 7 Pattern Scores Summary (compact grid with colored badges)
                column.Item().Text("ملخص الأنماط السباعية")
                    .Style(ReportTheme.ArabicTextStyle(18, true, "#111827"));

                column.Item().PaddingTop(16).Column(summary =>
                {
                    foreach (var pattern in patterns)
                    {
                        summary.Item().PaddingVertical(6).Row(row =>
                        {
                            // Pattern name (Arabic)
                            row.RelativeItem(3).Text(pattern.PatternNameAr)
                                .Style(ReportTheme.ArabicTextStyle(12, false, "#374151"));
                            
                            // T-Score with colored badge
                            row.AutoItem().PaddingHorizontal(8)
                                .Background(ReportTheme.GetBandColor(pattern.TScore))
                                .PaddingVertical(4)
                                .PaddingHorizontal(6)
                                .Text($"T={ReportTheme.FormatNum(pattern.TScore, 1)}")
                                .Style(TextStyle.Default.FontSize(11).FontColor("#ffffff").Bold());
                            
                            // Band label in Arabic
                            var bandAr = pattern.Band switch
                            {
                                "Excellent" => "ممتاز",
                                "Average" => "متوسط",
                                _ => "يحتاج تطوير"
                            };
                            row.AutoItem().Text(bandAr)
                                .Style(ReportTheme.ArabicTextStyle(11, false, "#6b7280"));
                        });
                    }
                });
            });

            // Footer
            page.Footer().AlignCenter().Text("صفحة 1 / 3")
                .Style(ReportTheme.ArabicTextStyle(9, false, "#9ca3af"));
        }

        /// <summary>
        /// Page 2: Heptagon radar chart + horizontal bar chart for detail
        /// </summary>
        private void ComposePage2_ChartsAndVisualizations(
            PageDescriptor page,
            List<SevenPatternScore> patterns,
            List<SdjSubDimensionScore> subDimensions)
        {
            page.Content().Column(column =>
            {
                // Title
                column.Item().Text("التحليل البصري للأنماط")
                    .Style(ReportTheme.ArabicTextStyle(20, true, "#111827"));

                column.Item().PaddingTop(20);

                // Section 1: Heptagon Radar Chart (الخريطة النفسية السباعية)
                column.Item().Text("الخريطة النفسية السباعية")
                    .Style(ReportTheme.ArabicTextStyle(16, true, "#374151"));

                column.Item().PaddingTop(12);

                try
                {
                    var heptagonBytes = HeptagonRadarChartRenderer.RenderHeptagonChart(
                        patterns, 
                        size: 480, 
                        title: "الخريطة النفسية السباعية");
                    
                    column.Item().AlignCenter().Height(480).Image(heptagonBytes);
                    Console.WriteLine("   ✓ Heptagon chart rendered successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ⚠ Heptagon chart failed: {ex.Message}");
                    column.Item().AlignCenter().Text("[Heptagon Chart Error]")
                        .Style(TextStyle.Default.FontSize(10).FontColor("#ef4444"));
                }

                column.Item().PaddingTop(32);

                // Section 2: Horizontal Bar Chart (kept as-is per requirements)
                column.Item().Text("توزيع الدرجات التفصيلية")
                    .Style(ReportTheme.ArabicTextStyle(16, true, "#374151"));

                column.Item().PaddingTop(12);

                try
                {
                    // Take top 12 dimensions for horizontal bar chart
                    var topSubDims = subDimensions
                        .OrderByDescending(s => s.T)
                        .Take(12)
                        .Select(s => new DimensionScore
                        {
                            Dimension = s.SubDimension,
                            T = s.T,
                            Percentile = s.Percentile
                        })
                        .ToList();

                    var barChartBytes = HorizontalBarChartRenderer.RenderHorizontalBars(
                        topSubDims,
                        width: 560,
                        maxDimensions: 12);
                    
                    column.Item().AlignCenter().Image(barChartBytes);
                    Console.WriteLine($"   ✓ Horizontal bar chart rendered ({topSubDims.Count} dimensions)");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ⚠ Bar chart failed: {ex.Message}");
                }
            });

            // Footer
            page.Footer().AlignCenter().Text("صفحة 2 / 3")
                .Style(ReportTheme.ArabicTextStyle(9, false, "#9ca3af"));
        }

        /// <summary>
        /// Page 3: Weak sub-dimensions table with course recommendations
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
                var logoPath = Path.Combine(AppContext.BaseDirectory, "Resources", "Brand", "SAITEST-ICON.png");
                
                // Fallback to SAITES-ICON.png if SAITEST not found
                if (!File.Exists(logoPath))
                {
                    logoPath = Path.Combine(AppContext.BaseDirectory, "Resources", "Brand", "SAITES-ICON.png");
                }

                if (File.Exists(logoPath))
                {
                    _logoBytes = File.ReadAllBytes(logoPath);
                    Console.WriteLine($"[ModernSdjSevenPatternReportService] ✓ Logo loaded from {logoPath}");
                }
                else
                {
                    Console.WriteLine($"[ModernSdjSevenPatternReportService] ⚠ Logo not found: {logoPath}");
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
