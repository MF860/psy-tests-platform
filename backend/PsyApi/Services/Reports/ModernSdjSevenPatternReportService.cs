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
                            "يُقدم هذا التقرير تحليلاً شاملاً لسماتك النفسية وقدراتك المهنية بناءً على إطار التنمية المستدامة (SDJ). " +
                            "تم تقييم أدائك عبر سبعة أنماط رئيسية و24 بُعداً فرعياً، وتمثل الدرجات كنسب معيارية (T-Scores) حيث " +
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

                column.Item().PaddingTop(16);

                // SUB-SECTION 1: Horizontal Bar Chart for Subdimensions (restored as requested)
                column.Item().AlignRight().Text("توزيع الدرجات التفصيلي عبر الأبعاد الفرعية (T-Score)")
                    .Style(ReportTheme.ArabicTextStyle(14, true, "#374151"));

                column.Item().PaddingTop(8);

                try
                {
                    // Sort subdimensions ascending by T-score (weakest first, strongest last)
                    var sortedSubDims = subDimensions
                        .OrderBy(s => s.T)
                        .Take(18) // Max 18 for readability on one page
                        .Select(s => new DimensionScore
                        {
                            Dimension = s.SubDimension,
                            T = s.T,
                            Percentile = s.Percentile
                        })
                        .ToList();

                    var barChartBytes = HorizontalBarChartRenderer.RenderHorizontalBars(
                        sortedSubDims,
                        width: 540,
                        maxDimensions: 18);
                    
                    column.Item().AlignCenter().Image(barChartBytes);
                    Console.WriteLine($"   ✓ Horizontal subdimension bar chart rendered ({sortedSubDims.Count} dimensions)");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ⚠ Bar chart rendering failed: {ex.Message}");
                    column.Item().AlignCenter().PaddingVertical(20)
                        .Text("[خطأ في رسم المخطط الشريطي]")
                        .Style(TextStyle.Default.FontSize(11).FontColor("#ef4444"));
                }

                column.Item().PaddingTop(24);

                // SUB-SECTION 2: Heptagon Radar Chart for 7 Patterns (fixed Arabic labels with HarfBuzz)
                column.Item().AlignRight().Text("التحليل البصري للأنماط الرئيسية")
                    .Style(ReportTheme.ArabicTextStyle(14, true, "#374151"));

                column.Item().PaddingTop(8).AlignCenter()
                    .Text("(الرسم البياني السباعي يُظهر توزيع الأنماط السبعة الرئيسية)")
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
        /// Page 3: Seven Tracks Summary (ملخص الأنماط الرئيسية) - as per requirements
        /// Shows all 7 SDJ tracks with T-scores, bands, and interpretations
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

                column.Item().PaddingTop(8).AlignCenter()
                    .Text("(تحليل شامل للأنماط السبعة بناءً على نتائجك في الاختبار)")
                    .Style(ReportTheme.ArabicTextStyle(10, false, "#6b7280"));

                column.Item().PaddingTop(16);

                // Sort patterns by T-score descending (best first)
                var sortedPatterns = patterns.OrderByDescending(p => p.TScore).ToList();

                // Display all 7 tracks with interpretations
                foreach (var pattern in sortedPatterns)
                {
                    column.Item().PaddingBottom(14)
                        .Border(1)
                        .BorderColor("#e5e7eb")
                        .Background("#fafafa")
                        .Padding(14)
                        .Column(trackCard =>
                        {
                            // Track header: Name + T-score badge + Band
                            trackCard.Item().Row(header =>
                            {
                                header.RelativeItem(3).Text(pattern.PatternNameAr)
                                    .Style(ReportTheme.ArabicTextStyle(14, true, "#111827"));
                                
                                header.AutoItem().PaddingHorizontal(8)
                                    .Background(ReportTheme.GetBandColor(pattern.TScore))
                                    .PaddingVertical(4)
                                    .PaddingHorizontal(8)
                                    .Text($"T = {ReportTheme.FormatNum(pattern.TScore, 1)}")
                                    .Style(TextStyle.Default.FontSize(11).FontColor("#ffffff").Bold());
                                
                                var bandLabel = GetBandLabel(pattern.TScore);
                                header.AutoItem().AlignMiddle().Text(bandLabel)
                                    .Style(ReportTheme.ArabicTextStyle(11, true, GetBandColor(pattern.TScore)));
                            });

                            // Interpretation paragraph
                            trackCard.Item().PaddingTop(8).Text(GetTrackInterpretation(pattern, subDimensions))
                                .Style(ReportTheme.ArabicTextStyle(10, false, "#374151")).LineHeight(1.5f);
                        });
                }
            });

            // Footer
            page.Footer().AlignCenter().Text("صفحة 3")
                .Style(ReportTheme.ArabicTextStyle(9, false, "#9ca3af"));
        }

        /// <summary>
        /// Page 4: Detailed track analyses organized by track (not by weakness) + course recommendations
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
                column.Item().AlignCenter().Text("الدورات المقترحة لكل مسار")
                    .Style(ReportTheme.ArabicTextStyle(20, true, "#111827"));

                column.Item().PaddingTop(8).AlignCenter()
                    .Text("(توصيات تطويرية مخصصة لكل نمط من الأنماط السبعة)")
                    .Style(ReportTheme.ArabicTextStyle(10, false, "#6b7280"));

                column.Item().PaddingTop(16);

                // Organize by tracks (show subdimensions within each track)
                var sortedPatterns = patterns.OrderByDescending(p => p.TScore).ToList();
                
                // Show top 3-4 tracks for space management
                foreach (var pattern in sortedPatterns.Take(4))
                {
                    column.Item().PaddingBottom(16)
                        .Column(trackSection =>
                        {
                            // Track heading
                            trackSection.Item().Background("#f3f4f6")
                                .Padding(10)
                                .Row(header =>
                                {
                                    header.RelativeItem().Text(pattern.PatternNameAr)
                                        .Style(ReportTheme.ArabicTextStyle(14, true, "#1f2937"));
                                    header.AutoItem().Text($"T = {ReportTheme.FormatNum(pattern.TScore, 1)}")
                                        .Style(ReportTheme.ArabicTextStyle(12, true, GetBandColor(pattern.TScore)));
                                });

                            // Subdimensions table within this track
                            var trackSubDims = pattern.SubDimensions.OrderBy(s => s.T).ToList();
                            if (trackSubDims.Any())
                            {
                                trackSection.Item().PaddingTop(8).Column(subDimTable =>
                                {
                                    foreach (var subDim in trackSubDims.Take(3)) // Max 3 per track for space
                                    {
                                        subDimTable.Item().PaddingVertical(4).Row(row =>
                                        {
                                            row.RelativeItem(2).Text(subDim.SubDimension)
                                                .Style(ReportTheme.ArabicTextStyle(10, false, "#4b5563"));
                                            row.AutoItem().Text($"T={ReportTheme.FormatNum(subDim.T, 1)}")
                                                .Style(TextStyle.Default.FontSize(9).FontColor("#6b7280"));
                                            row.RelativeItem(1).Text(GetMiniAssessment(subDim.T))
                                                .Style(ReportTheme.ArabicTextStyle(9, false, "#6b7280"));
                                        });
                                    }
                                });
                            }

                            // Course recommendations for this track's weak subdimensions
                            var trackCourses = courseRecommendations
                                .Where(c => pattern.SubDimensions.Any(s => s.SubDimension == c.SubDimensionAr))
                                .SelectMany(c => c.RecommendedCourses)
                                .Distinct()
                                .Take(3)
                                .ToList();

                            if (trackCourses.Any())
                            {
                                trackSection.Item().PaddingTop(6).Column(courses =>
                                {
                                    courses.Item().Text("دورات مقترحة:")
                                        .Style(ReportTheme.ArabicTextStyle(10, true, "#374151"));
                                    foreach (var course in trackCourses)
                                    {
                                        courses.Item().PaddingVertical(2).Row(courseRow =>
                                        {
                                            courseRow.AutoItem().Width(12).Text("•")
                                                .Style(TextStyle.Default.FontSize(10).FontColor("#2563eb"));
                                            courseRow.RelativeItem().Text(course)
                                                .Style(ReportTheme.ArabicTextStyle(9, false, "#4b5563"));
                                        });
                                    }
                                });
                            }
                        });
                }

                column.Item().PaddingTop(20);

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
        }

        /// <summary>
        /// Get Arabic band label based on T-score
        /// </summary>
        private string GetBandLabel(double tScore)
        {
            if (tScore >= 55) return "متقدّم";
            if (tScore >= 45) return "متوسط";
            return "بحاجة لتنمية";
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
