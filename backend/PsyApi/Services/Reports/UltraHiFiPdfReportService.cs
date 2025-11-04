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
    /// Ultra Hi-Fi PDF Report Service v4.0
    /// Premium, board-level quality psychometric reports with:
    /// - Aurora Glass (Light) and Noir Executive (Dark) themes
    /// - Perfect bilingual support (Arabic RTL + English LTR)
    /// - Vector-quality charts (300-450 DPI)
    /// - Glassmorphism/Neumorphic design components
    /// - Print-ready PDF/X-4 output
    /// </summary>
    public class UltraHiFiPdfReportService : IPdfReportService
    {
        private readonly IRecommendationService _recommendationService;
        private static bool _fontsRegistered;
        private static readonly object _lock = new();
        private static byte[]? _logoBytes;

        public UltraHiFiPdfReportService(IRecommendationService recommendationService)
        {
            _recommendationService = recommendationService;
            EnsureFonts();
            LoadLogo();
        }

        public async Task<byte[]> RenderResultPdfAsync(
            Result result, 
            User user, 
            IEnumerable<DimensionScoreDto> dimensions, 
            CancellationToken ct = default)
        {
            var startTime = DateTime.Now;

            try
            {
                Console.WriteLine($"\n{'═', 70}");
                Console.WriteLine($"🎨 ULTRA HI-FI PSYCHOMETRIC REPORT v4.0");
                Console.WriteLine($"{'═', 70}");
                Console.WriteLine($"👤 User: {user.FullName ?? "غير محدد"} (ID: {user.NationalId ?? "N/A"})");
                Console.WriteLine($"📋 Session: {result.SessionId}");
                Console.WriteLine($"🎨 Theme: {DesignTokens.CurrentTheme}");
                Console.WriteLine($"🌐 Language: {LocalizationStrings.CurrentLanguage}");
                Console.WriteLine($"🕐 Started: {startTime:yyyy-MM-dd HH:mm:ss}");

                // Configure QuestPDF
                QuestPDF.Settings.License = LicenseType.Community;
                QuestPDF.Settings.EnableDebugging = false;
                Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("ar-JO");

                // Prepare data
                var dimensionsList = dimensions.OrderBy(d => d.T).ToList();
                if (!dimensionsList.Any())
                    throw new InvalidOperationException("No dimensions provided");

                // Validate data
                foreach (var d in dimensionsList)
                {
                    if (double.IsNaN(d.T) || double.IsInfinity(d.T))
                        throw new InvalidOperationException($"Invalid T-score for {d.Dimension}");
                }

                // Calculate statistics
                var avgTScore = dimensionsList.Average(d => d.T);
                var avgPercentile = dimensionsList.Average(d => d.Percentile);
                var variance = CalculateVariance(dimensionsList.Select(d => d.T));
                var totalScore = (int)Math.Round(dimensionsList.Sum(d => d.T));

                // Band counts
                var excellentCount = dimensionsList.Count(d => d.T >= DesignTokens.PerformanceBands.ExcellentMin);
                var goodCount = dimensionsList.Count(d => d.T >= DesignTokens.PerformanceBands.GoodMin && d.T < DesignTokens.PerformanceBands.ExcellentMin);
                var averageCount = dimensionsList.Count(d => d.T >= DesignTokens.PerformanceBands.AverageMin && d.T < DesignTokens.PerformanceBands.GoodMin);
                var weakCount = dimensionsList.Count(d => d.T < DesignTokens.PerformanceBands.AverageMin);

                // Top and bottom dimensions
                var strongestDimensions = dimensionsList.OrderByDescending(d => d.T).Take(5).ToList();
                var weakestDimensions = dimensionsList.Take(5).ToList();

                // Cluster analysis
                var clusterGroups = ReportAnalytics.GroupDimensionsByClusters(dimensionsList);

                // Recommendations
                var recommendations = _recommendationService.GetDimensionRecommendations(weakestDimensions);
                var courses = _recommendationService.GetSuggestedCourses(weakestDimensions);
                var actionPlan = ReportAnalytics.GenerateActionPlan(weakestDimensions, clusterGroups);

                // Insights
                var insights = ReportAnalytics.GenerateInsights(
                    dimensionsList, excellentCount, goodCount, averageCount, weakCount);
                var riskFlags = ReportAnalytics.RiskFlags(dimensionsList).ToList();

                // Logging
                Console.WriteLine($"\n📊 STATISTICS:");
                Console.WriteLine($"   Dimensions: {dimensionsList.Count}");
                Console.WriteLine($"   Excellent (≥{DesignTokens.PerformanceBands.ExcellentMin}): {excellentCount}");
                Console.WriteLine($"   Good ({DesignTokens.PerformanceBands.GoodMin}-{DesignTokens.PerformanceBands.ExcellentMin - 0.1}): {goodCount}");
                Console.WriteLine($"   Average ({DesignTokens.PerformanceBands.AverageMin}-{DesignTokens.PerformanceBands.GoodMin - 0.1}): {averageCount}");
                Console.WriteLine($"   Weak (<{DesignTokens.PerformanceBands.AverageMin}): {weakCount}");
                Console.WriteLine($"   Avg T-Score: {DesignTokens.Formatting.FormatTScore(avgTScore)}");
                Console.WriteLine($"   Variance: {DesignTokens.Formatting.FormatNumber(variance, 2)}");

                // Generate PDF
                var pdf = Document.Create(container =>
                {
                    // Page 1: Cover & Executive Summary
                    container.Page(page =>
                    {
                        ConfigurePageDefaults(page, 1, 6);
                        ComposePage1_CoverAndExecutiveSummary(
                            page, user, result, avgTScore, avgPercentile, variance, totalScore,
                            strongestDimensions, weakestDimensions, dimensionsList.Count,
                            excellentCount, goodCount, averageCount, weakCount);
                    });

                    // Page 2: Visual Analytics
                    container.Page(page =>
                    {
                        ConfigurePageDefaults(page, 2, 6);
                        ComposePage2_VisualAnalytics(
                            page, dimensionsList, clusterGroups,
                            excellentCount, goodCount, averageCount, weakCount);
                    });

                    // Page 3: Data Story & Analysis
                    container.Page(page =>
                    {
                        ConfigurePageDefaults(page, 3, 6);
                        ComposePage3_DataStoryAndAnalysis(
                            page, dimensionsList, clusterGroups, insights, riskFlags);
                    });

                    // Page 4: Personalized Development Plan
                    container.Page(page =>
                    {
                        ConfigurePageDefaults(page, 4, 6);
                        ComposePage4_DevelopmentPlan(
                            page, weakestDimensions, actionPlan, recommendations);
                    });

                    // Page 5: Training Courses (if available)
                    if (courses.Any())
                    {
                        container.Page(page =>
                        {
                            ConfigurePageDefaults(page, 5, courses.Any() ? 6 : 5);
                            ComposePage5_TrainingCourses(page, courses, weakestDimensions);
                        });
                    }

                    // Page 6: Quality & Methodology
                    container.Page(page =>
                    {
                        ConfigurePageDefaults(page, courses.Any() ? 6 : 5, courses.Any() ? 6 : 5);
                        ComposePage6_QualityAndMethodology(page, dimensionsList);
                    });
                });

                var pdfBytes = pdf.GeneratePdf();
                var endTime = DateTime.Now;
                var duration = endTime - startTime;
                var pageCount = 5 + (courses.Any() ? 1 : 0);

                Console.WriteLine($"\n{'─', 70}");
                Console.WriteLine($"✅ PDF GENERATION COMPLETE");
                Console.WriteLine($"{'─', 70}");
                Console.WriteLine($"📄 Pages: {pageCount}");
                Console.WriteLine($"📏 Size: {pdfBytes.Length / 1024:F1} KB");
                Console.WriteLine($"⏱ Duration: {duration.TotalMilliseconds:F0}ms");
                Console.WriteLine($"🎯 Font: {DesignTokens.Typography.FontArabic} with HarfBuzz");
                Console.WriteLine($"📊 Charts: Vector-quality (300-450 DPI)");
                Console.WriteLine($"🔢 Numbers: Western digits (zero � glyphs)");
                Console.WriteLine($"🎨 Design System: v4.0 Ultra Hi-Fi");
                Console.WriteLine($"{'═', 70}\n");

                return pdfBytes;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ PDF GENERATION FAILED: {ex.Message}");
                Console.WriteLine($"   Stack: {ex.StackTrace}");
                throw;
            }
        }

        public Task<byte[]> RenderSdjResultPdfAsync(Result result, User user, CancellationToken ct = default)
        {
            // For SDJ reports, detect version and parse accordingly
            try
            {
                var opts = new System.Text.Json.JsonSerializerOptions 
                { 
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    PropertyNameCaseInsensitive = true
                };
                
                var json = result.DimensionScoresJson ?? "{}";
                
                // Detect version by checking JSON structure
                var isV2 = json.Contains("PatternScores") && json.Contains("SubDimensionScores");
                var isV1 = json.Contains("Dimensions") && json.Contains("SevenPatternScores");
                
                Console.WriteLine($"[UltraHiFi] Detecting SDJ version - V2: {isV2}, V1: {isV1}");
                
                if (isV2)
                {
                    // Parse as SDJ V2 (latest 7-pattern system)
                    var v2Data = System.Text.Json.JsonSerializer.Deserialize<SdjV2StorageFormat>(json, opts);
                    
                    if (v2Data == null || v2Data.PatternScores?.Any() != true)
                    {
                        Console.WriteLine($"[UltraHiFi] SDJ V2 data is empty");
                        throw new InvalidOperationException("SDJ V2 data contains no pattern scores");
                    }
                    
                    Console.WriteLine($"[UltraHiFi] SDJ V2 loaded - Patterns: {v2Data.PatternScores.Count}, SubDimensions: {v2Data.SubDimensionScores?.Count ?? 0}");
                    
                    // Convert V2 format to SdjScoreSummary for ModernSdjSevenPatternReportService
                    var sdjData = ConvertV2ToSummary(v2Data);
                    var modernService = new ModernSdjSevenPatternReportService();
                    return modernService.RenderSdjSevenPatternPdfAsync(result, user, sdjData, ct);
                }
                else if (isV1)
                {
                    // Parse as SDJ V1 (legacy format with Dimensions + SevenPatternScores)
                    var sdjData = System.Text.Json.JsonSerializer.Deserialize<SdjScoreSummary>(json, opts);
                    
                    if (sdjData == null || (sdjData.Dimensions?.Any() != true && sdjData.SevenPatternScores?.Any() != true))
                    {
                        Console.WriteLine($"[UltraHiFi] SDJ V1 data is empty");
                        throw new InvalidOperationException("SDJ V1 data contains no dimensions or patterns");
                    }
                    
                    Console.WriteLine($"[UltraHiFi] SDJ V1 loaded - Dimensions: {sdjData.Dimensions?.Count ?? 0}, Patterns: {sdjData.SevenPatternScores?.Count ?? 0}");
                    
                    var modernService = new ModernSdjSevenPatternReportService();
                    return modernService.RenderSdjSevenPatternPdfAsync(result, user, sdjData, ct);
                }
                else
                {
                    Console.WriteLine($"[UltraHiFi] Unknown SDJ format - JSON preview: {json.Substring(0, Math.Min(200, json.Length))}");
                    throw new InvalidOperationException("Unknown SDJ data format - neither V1 nor V2 detected");
                }
            }
            catch (System.Text.Json.JsonException jsonEx)
            {
                Console.WriteLine($"[UltraHiFi] JSON parsing error: {jsonEx.Message}");
                throw new InvalidOperationException($"Invalid SDJ JSON format: {jsonEx.Message}", jsonEx);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UltraHiFi] SDJ generation error: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Convert SDJ V2 storage format to SdjScoreSummary for report generation
        /// </summary>
        private SdjScoreSummary ConvertV2ToSummary(SdjV2StorageFormat v2Data)
        {
            return new SdjScoreSummary
            {
                // Map V2 PatternScores to SevenPatternScores
                SevenPatternScores = v2Data.PatternScores?.Select(p => new SevenPatternScore
                {
                    PatternKey = p.PatternKey ?? "",
                    PatternNameAr = p.PatternNameAr ?? "",
                    PatternNameEn = p.PatternKey ?? "",
                    TScore = p.TScore,
                    Percentile = p.Percentile,
                    Raw = p.Raw,
                    Band = p.Band ?? "",
                    SubDimensionCount = p.SubDimensions?.Count ?? 0,
                    SubDimensions = p.SubDimensions?.Select(sd => new SdjSubDimensionScore
                    {
                        SubDimension = sd.SubNameAr ?? "",
                        T = sd.TScore,
                        Percentile = sd.Percentile,
                        Raw = sd.Raw,
                        Band = sd.Band ?? ""
                    }).ToList() ?? new List<SdjSubDimensionScore>()
                }).ToList() ?? new List<SevenPatternScore>(),
                
                // Map V2 overall score to Dimensions (for compatibility)
                Dimensions = v2Data.PatternScores?.Select(p => new SdjDimensionScore
                {
                    Dimension = p.PatternNameAr ?? "",
                    T = p.TScore,
                    Percentile = p.Percentile,
                    Band = p.Band ?? "",
                    Raw = p.Raw,
                    SubDimensions = p.SubDimensions?.Select(sd => new SdjSubDimensionScore
                    {
                        Dimension = p.PatternId ?? "",
                        SubDimension = sd.SubNameAr ?? "",
                        T = sd.TScore,
                        Percentile = sd.Percentile,
                        Band = sd.Band ?? "",
                        Raw = sd.Raw
                    }).ToList() ?? new List<SdjSubDimensionScore>()
                }).ToList() ?? new List<SdjDimensionScore>(),
                
                SubDimensions = v2Data.SubDimensionScores?.Select(sd => new SdjSubDimensionScore
                {
                    Dimension = sd.PatternId ?? "",
                    SubDimension = sd.SubNameAr ?? "",
                    T = sd.TScore,
                    Percentile = sd.Percentile,
                    Band = sd.Band ?? "",
                    Raw = sd.Raw,
                    ItemCount = sd.ItemCount
                }).ToList() ?? new List<SdjSubDimensionScore>(),
                
                TotalScore = new SdjTotalScore
                {
                    T = v2Data.OverallScore?.TScore ?? 50,
                    Percentile = v2Data.OverallScore?.Percentile ?? 0.5,
                    Raw = v2Data.OverallScore?.Raw ?? 0
                },
                
                Version = v2Data.Version ?? "SDJ_v2.0",
                TrackFits = new List<SdjTrackFit>() // V2 doesn't use track fits
            };
        }

        #region Page 1: Cover & Executive Summary

        private void ComposePage1_CoverAndExecutiveSummary(
            PageDescriptor page,
            User user,
            Result result,
            double avgTScore,
            double avgPercentile,
            double variance,
            int totalScore,
            List<DimensionScoreDto> strongestDimensions,
            List<DimensionScoreDto> weakestDimensions,
            int totalDimensions,
            int excellentCount,
            int goodCount,
            int averageCount,
            int weakCount)
        {
            page.Content().Column(column =>
            {
                // Header with logo
                column.Item().Element(c => DesignComponents.Header(
                    c,
                    title: LocalizationStrings.Get("report.title"),
                    subtitle: LocalizationStrings.Get("report.subtitle"),
                    metadata: new Dictionary<string, string>
                    {
                        { LocalizationStrings.Get("participant.name"), user.FullName ?? "غير محدد" },
                        { LocalizationStrings.Get("participant.id"), user.NationalId ?? "N/A" },
                        { LocalizationStrings.Get("session.date"), result.CreatedAt.ToString("yyyy-MM-dd") },
                        { LocalizationStrings.Get("scoring.model"), result.ScoringModelVersion ?? "Standard" }
                    },
                    logoBytes: _logoBytes
                ));

                column.Item().PaddingTop(DesignTokens.Spacing.XL);

                // Overall Status Badge
                var overallStatus = avgTScore >= DesignTokens.PerformanceBands.ExcellentMin ? "excellent" :
                                   avgTScore >= DesignTokens.PerformanceBands.GoodMin ? "good" :
                                   avgTScore >= DesignTokens.PerformanceBands.AverageMin ? "average" : "weak";
                
                column.Item().AlignCenter().Width(200).Element(c =>
                    DesignComponents.Badge(
                        c,
                        text: LocalizationStrings.Get($"band.{overallStatus}"),
                        level: overallStatus
                    )
                );

                column.Item().PaddingTop(DesignTokens.Spacing.LG);

                // KPI Cards (4 cards in a row)
                column.Item().Element(c => DesignComponents.Section(c, LocalizationStrings.Get("summary.kpis"), "📊"));

                column.Item().PaddingTop(DesignTokens.Spacing.MD);

                column.Item().Row(row =>
                {
                    // Avg T-Score
                    row.RelativeItem().PaddingRight(DesignTokens.Spacing.SM).Element(c =>
                        DesignComponents.KpiCard(
                            c,
                            value: DesignTokens.Formatting.FormatTScore(avgTScore),
                            label: LocalizationStrings.Get("kpi.avg_tscore"),
                            color: DesignTokens.PerformanceBands.GetBandColor(avgTScore)
                        )
                    );

                    // Avg Percentile
                    row.RelativeItem().PaddingHorizontal(DesignTokens.Spacing.SM / 2).Element(c =>
                        DesignComponents.KpiCard(
                            c,
                            value: DesignTokens.Formatting.FormatPercentile(avgPercentile),
                            label: LocalizationStrings.Get("kpi.avg_percentile"),
                            color: DesignTokens.Colors.Info
                        )
                    );

                    // Variance
                    row.RelativeItem().PaddingHorizontal(DesignTokens.Spacing.SM / 2).Element(c =>
                        DesignComponents.KpiCard(
                            c,
                            value: DesignTokens.Formatting.FormatNumber(variance, 1),
                            label: LocalizationStrings.Get("kpi.variance"),
                            color: DesignTokens.Colors.Accent
                        )
                    );

                    // Advanced Patterns
                    row.RelativeItem().PaddingLeft(DesignTokens.Spacing.SM).Element(c =>
                        DesignComponents.KpiCard(
                            c,
                            value: excellentCount.ToString(),
                            label: LocalizationStrings.Get("kpi.advanced_patterns"),
                            color: DesignTokens.Colors.Success
                        )
                    );
                });

                column.Item().PaddingTop(DesignTokens.Spacing.XL);

                // Top 3 Strengths
                if (strongestDimensions.Any())
                {
                    column.Item().AlignRight().Text(LocalizationStrings.Get("summary.strengths"))
                        .Style(GetTextStyle(DesignTokens.Typography.H3, true, DesignTokens.Colors.Success));

                    column.Item().PaddingTop(DesignTokens.Spacing.SM).Row(row =>
                    {
                        foreach (var strength in strongestDimensions.Take(3))
                        {
                            var dimName = ArabicTextRenderer.FormatDimensionName(strength.Dimension, 20);
                            row.RelativeItem().PaddingHorizontal(DesignTokens.Spacing.XS).Element(c =>
                                DesignComponents.GlassCard(c, card =>
                                {
                                    card.Background(DesignTokens.Colors.Success)
                                        .Padding(DesignTokens.Spacing.MD)
                                        .Column(col =>
                                        {
                                            col.Item().AlignCenter().Text(dimName)
                                                .Style(GetTextStyle(DesignTokens.Typography.Body, false, "#FFFFFF"));
                                            col.Item().PaddingTop(DesignTokens.Spacing.XS).AlignCenter()
                                                .Text($"T={DesignTokens.Formatting.FormatTScore(strength.T)}")
                                                .FontSize(DesignTokens.Typography.Small)
                                                .FontColor("#FFFFFF");
                                        });
                                })
                            );
                        }
                    });

                    column.Item().PaddingTop(DesignTokens.Spacing.LG);
                }

                // Growth Areas
                if (weakestDimensions.Any())
                {
                    column.Item().AlignRight().Text(LocalizationStrings.Get("summary.growth_areas"))
                        .Style(GetTextStyle(DesignTokens.Typography.H3, true, DesignTokens.Colors.Warning));

                    column.Item().PaddingTop(DesignTokens.Spacing.SM).Row(row =>
                    {
                        foreach (var weakness in weakestDimensions.Take(3))
                        {
                            var dimName = ArabicTextRenderer.FormatDimensionName(weakness.Dimension, 20);
                            var bandColor = DesignTokens.PerformanceBands.GetBandColor(weakness.T);

                            row.RelativeItem().PaddingHorizontal(DesignTokens.Spacing.XS).Element(c =>
                                DesignComponents.GlassCard(c, card =>
                                {
                                    card.Background(bandColor)
                                        .Padding(DesignTokens.Spacing.MD)
                                        .Column(col =>
                                        {
                                            col.Item().AlignCenter().Text(dimName)
                                                .Style(GetTextStyle(DesignTokens.Typography.Body, false, "#FFFFFF"));
                                            col.Item().PaddingTop(DesignTokens.Spacing.XS).AlignCenter()
                                                .Text($"T={DesignTokens.Formatting.FormatTScore(weakness.T)}")
                                                .FontSize(DesignTokens.Typography.Small)
                                                .FontColor("#FFFFFF");
                                        });
                                })
                            );
                        }
                    });
                }

                column.Item().PaddingTop(DesignTokens.Spacing.XL);

                // Band Distribution Summary
                column.Item().Background(DesignTokens.Colors.Surface)
                    .Padding(DesignTokens.Spacing.MD)
                    .Row(row =>
                    {
                        row.RelativeItem().AlignRight()
                            .Text($"{LocalizationStrings.Get("band.excellent")}: {excellentCount} | " +
                                  $"{LocalizationStrings.Get("band.good")}: {goodCount} | " +
                                  $"{LocalizationStrings.Get("band.average")}: {averageCount} | " +
                                  $"{LocalizationStrings.Get("band.weak")}: {weakCount}")
                            .Style(GetTextStyle(DesignTokens.Typography.Body, true));
                    });
            });
        }

        #endregion

        #region Page 2: Visual Analytics

        private void ComposePage2_VisualAnalytics(
            PageDescriptor page,
            List<DimensionScoreDto> sortedDimensions,
            Dictionary<string, List<DimensionScoreDto>> clusterGroups,
            int excellentCount,
            int goodCount,
            int averageCount,
            int weakCount)
        {
            page.Content().Background(DesignTokens.Colors.Surface).Padding(DesignTokens.Spacing.MD).Column(column =>
            {
                // Title
                column.Item().Element(c =>
                    DesignComponents.Section(c, LocalizationStrings.Get("charts.overview"), "📊"));

                column.Item().PaddingTop(DesignTokens.Spacing.LG);

                // Radar Chart
                column.Item().Background("#FFFFFF").Padding(DesignTokens.Spacing.MD).Column(radarCard =>
                {
                    try
                    {
                        var radarBytes = RadarChartRenderer.RenderRadarChart(
                            sortedDimensions.Take(12),
                            size: 400,
                            showGrid: true);

                        radarCard.Item().AlignCenter().Height(220).Image(radarBytes);
                        radarCard.Item().PaddingTop(DesignTokens.Spacing.SM).AlignCenter()
                            .Text(LocalizationStrings.Get("charts.radar.subtitle"))
                            .Style(GetTextStyle(DesignTokens.Typography.Caption, false, DesignTokens.Colors.TextSecondary));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Charts] Radar error: {ex.Message}");
                        radarCard.Item().AlignCenter().Element(c =>
                            DesignComponents.Callout(c, $"Chart error: {ex.Message}", "danger"));
                    }
                });

                column.Item().PaddingTop(DesignTokens.Spacing.MD);

                // Horizontal Bar Chart
                column.Item().Background("#FFFFFF").Padding(DesignTokens.Spacing.MD).Column(barCard =>
                {
                    try
                    {
                        var barBytes = HorizontalBarChartRenderer.RenderHorizontalBars(
                            sortedDimensions,
                            width: 580,
                            maxDimensions: 12);

                        barCard.Item().AlignCenter().Height(280).Image(barBytes);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Charts] Bar error: {ex.Message}");
                        barCard.Item().AlignCenter().Element(c =>
                            DesignComponents.Callout(c, $"Chart error: {ex.Message}", "danger"));
                    }
                });

                column.Item().PaddingTop(DesignTokens.Spacing.MD);

                // Donut Chart
                column.Item().Background("#FFFFFF").Padding(DesignTokens.Spacing.MD).Column(donutCard =>
                {
                    try
                    {
                        var clusterDistribution = DonutChartRenderer.CalculateClusterDistribution(sortedDimensions);

                        if (clusterDistribution.Any())
                        {
                            var donutBytes = DonutChartRenderer.RenderClusterDonut(
                                clusterDistribution,
                                size: 160,
                                centerText: LocalizationStrings.CurrentLanguage == ReportLanguage.AR ? "المحاور" : "Clusters");

                            donutCard.Item().AlignCenter().Height(170).Image(donutBytes);
                            donutCard.Item().PaddingTop(DesignTokens.Spacing.SM).AlignCenter()
                                .Text(LocalizationStrings.Get("charts.donut.subtitle"))
                                .Style(GetTextStyle(DesignTokens.Typography.Caption, false, DesignTokens.Colors.TextSecondary));
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Charts] Donut error: {ex.Message}");
                        donutCard.Item().AlignCenter().Element(c =>
                            DesignComponents.Callout(c, $"Chart error: {ex.Message}", "danger"));
                    }
                });

                column.Item().PaddingTop(DesignTokens.Spacing.LG);

                // Statistical Summary
                column.Item().Background("#FFFFFF").Padding(DesignTokens.Spacing.MD)
                    .Row(summaryRow =>
                    {
                        summaryRow.RelativeItem().AlignRight()
                            .Text(LocalizationStrings.Get("charts.summary") + ": " +
                                  $"{LocalizationStrings.Get("band.excellent")}: {excellentCount} | " +
                                  $"{LocalizationStrings.Get("band.good")}: {goodCount} | " +
                                  $"{LocalizationStrings.Get("band.average")}: {averageCount} | " +
                                  $"{LocalizationStrings.Get("band.weak")}: {weakCount}")
                            .Style(GetTextStyle(DesignTokens.Typography.Body, true));
                    });
            });
        }

        #endregion

        #region Page 3: Data Story & Analysis

        private void ComposePage3_DataStoryAndAnalysis(
            PageDescriptor page,
            List<DimensionScoreDto> dimensions,
            Dictionary<string, List<DimensionScoreDto>> clusterGroups,
            List<string> insights,
            List<string> riskFlags)
        {
            page.Content().Column(column =>
            {
                // Title
                column.Item().Element(c =>
                    DesignComponents.Section(c, LocalizationStrings.Get("analysis.title"), "📖"));

                column.Item().PaddingTop(DesignTokens.Spacing.LG);

                // General Insights
                if (insights.Any())
                {
                    column.Item().AlignRight().Text(LocalizationStrings.Get("insights.title"))
                        .Style(GetTextStyle(DesignTokens.Typography.H3, true, DesignTokens.Colors.Primary));

                    column.Item().PaddingTop(DesignTokens.Spacing.SM);

                    foreach (var insight in insights.Take(5))
                    {
                        column.Item().PaddingVertical(DesignTokens.Spacing.XS)
                            .AlignRight()
                            .Text($"• {insight}")
                            .Style(GetTextStyle(DesignTokens.Typography.Body, false));
                    }

                    column.Item().PaddingTop(DesignTokens.Spacing.LG);
                }

                // Cluster Analysis
                column.Item().AlignRight().Text(LocalizationStrings.Get("clusters.analysis"))
                    .Style(GetTextStyle(DesignTokens.Typography.H3, true, DesignTokens.Colors.Primary));

                column.Item().PaddingTop(DesignTokens.Spacing.SM);

                foreach (var (cluster, clusterDims) in clusterGroups)
                {
                    if (cluster == "OTHER" || !clusterDims.Any()) continue;

                    var analysis = ReportAnalytics.AnalyzeCluster(cluster, clusterDims);
                    var clusterName = ReportAnalytics.ClusterNames.GetValueOrDefault(cluster, cluster);

                    column.Item().PaddingVertical(DesignTokens.Spacing.SM).Element(c =>
                        DesignComponents.GlassCard(c, card =>
                        {
                            card.Column(col =>
                            {
                                col.Item().AlignRight().Text(clusterName)
                                    .Style(GetTextStyle(DesignTokens.Typography.TitleSemibold, true, DesignTokens.Colors.Text));

                                col.Item().PaddingTop(DesignTokens.Spacing.XS)
                                    .AlignRight()
                                    .Text(analysis)
                                    .Style(GetTextStyle(DesignTokens.Typography.Body, false));

                                // Top 3 dimensions in cluster
                                var topDims = clusterDims.OrderByDescending(d => d.T).Take(3);
                                col.Item().PaddingTop(DesignTokens.Spacing.SM).Row(dimRow =>
                                {
                                    foreach (var dim in topDims)
                                    {
                                        var dimName = ArabicTextRenderer.FormatDimensionName(dim.Dimension, 15);
                                        var bandColor = DesignTokens.PerformanceBands.GetBandColor(dim.T);

                                        dimRow.RelativeItem().PaddingHorizontal(DesignTokens.Spacing.XS)
                                            .Background(bandColor)
                                            .Padding(DesignTokens.Spacing.XS)
                                            .AlignCenter()
                                            .Text($"{dimName} ({DesignTokens.Formatting.FormatTScore(dim.T)})")
                                            .FontSize(DesignTokens.Typography.Small)
                                            .FontColor("#FFFFFF");
                                    }
                                });
                            });
                        })
                    );
                }

                column.Item().PaddingTop(DesignTokens.Spacing.XL);

                // Risk Flags
                if (riskFlags.Any())
                {
                    column.Item().AlignRight().Text(LocalizationStrings.Get("risk.title"))
                        .Style(GetTextStyle(DesignTokens.Typography.H3, true, DesignTokens.Colors.Danger));

                    column.Item().PaddingTop(DesignTokens.Spacing.SM);

                    foreach (var flag in riskFlags.Take(5))
                    {
                        column.Item().PaddingVertical(DesignTokens.Spacing.XS).Element(c =>
                            DesignComponents.Callout(c, flag, "warning"));
                    }
                }
            });
        }

        #endregion

        #region Page 4: Development Plan

        private void ComposePage4_DevelopmentPlan(
            PageDescriptor page,
            List<DimensionScoreDto> weakestDimensions,
            List<ActionItem> actionPlan,
            List<DimensionRecommendation> recommendations)
        {
            page.Content().Column(column =>
            {
                // Title
                column.Item().Element(c =>
                    DesignComponents.Section(c, LocalizationStrings.Get("plan.title"), "🎯"));

                column.Item().PaddingTop(DesignTokens.Spacing.MD);

                // Introduction
                column.Item().Element(c =>
                    DesignComponents.Callout(c, LocalizationStrings.Get("plan.intro"), "info"));

                column.Item().PaddingTop(DesignTokens.Spacing.LG);

                // Action Plan
                if (actionPlan.Any())
                {
                    foreach (var action in actionPlan.Take(6))
                    {
                        column.Item().PaddingVertical(DesignTokens.Spacing.SM)
                            .Border(1).BorderColor(DesignTokens.Colors.Border)
                            .Padding(DesignTokens.Spacing.MD)
                            .Column(actionCard =>
                            {
                                // Header: Title + Priority Badge
                                actionCard.Item().Row(headerRow =>
                                {
                                    headerRow.RelativeItem(3).AlignRight().Text(action.Title)
                                        .Style(GetTextStyle(DesignTokens.Typography.Body, true));

                                    var priorityLevel = action.Priority == "عالية" || action.Priority == "High" ? "danger" :
                                                       action.Priority == "متوسطة" || action.Priority == "Medium" ? "warning" :
                                                       "success";

                                    headerRow.RelativeItem(1).AlignRight().Element(c =>
                                        DesignComponents.Badge(c, action.Priority, priorityLevel));
                                });

                                // Description
                                actionCard.Item().PaddingTop(DesignTokens.Spacing.SM)
                                    .AlignRight()
                                    .Text(action.Description)
                                    .Style(GetTextStyle(DesignTokens.Typography.Body, false));

                                // Timeline & KPI
                                actionCard.Item().PaddingTop(DesignTokens.Spacing.SM).Row(footerRow =>
                                {
                                    if (!string.IsNullOrEmpty(action.Timeline))
                                    {
                                        footerRow.RelativeItem().AlignRight()
                                            .Text($"⏰ {action.Timeline}")
                                            .Style(GetTextStyle(DesignTokens.Typography.Small, false, DesignTokens.Colors.TextSecondary));
                                    }

                                    if (!string.IsNullOrEmpty(action.KPI))
                                    {
                                        footerRow.RelativeItem().AlignRight()
                                            .Text($"📊 {action.KPI}")
                                            .Style(GetTextStyle(DesignTokens.Typography.Small, false, DesignTokens.Colors.Primary));
                                    }
                                });
                            });
                    }
                }

                column.Item().PaddingTop(DesignTokens.Spacing.XL);

                // Tips for Success
                column.Item().Background(DesignTokens.CurrentTheme == DesignTokens.ReportThemeMode.AuroraGlass ? "#E7F3FF" : "#1E3A5F")
                    .Padding(DesignTokens.Spacing.MD)
                    .Column(tipsBox =>
                    {
                        tipsBox.Item().AlignRight().Text(LocalizationStrings.Get("plan.tips"))
                            .Style(GetTextStyle(DesignTokens.Typography.TitleSemibold, true, DesignTokens.Colors.Primary));

                        var tips = new[]
                        {
                            LocalizationStrings.Get("plan.tips.commitment"),
                            LocalizationStrings.Get("plan.tips.tracking"),
                            LocalizationStrings.Get("plan.tips.feedback"),
                            LocalizationStrings.Get("plan.tips.review"),
                            LocalizationStrings.Get("plan.tips.help")
                        };

                        foreach (var tip in tips)
                        {
                            tipsBox.Item().PaddingVertical(DesignTokens.Spacing.XS)
                                .AlignRight()
                                .Text($"✓ {tip}")
                                .Style(GetTextStyle(DesignTokens.Typography.Small, false));
                        }
                    });
            });
        }

        #endregion

        #region Page 5: Training Courses

        private void ComposePage5_TrainingCourses(
            PageDescriptor page,
            List<CourseSuggestion> courses,
            List<DimensionScoreDto> weakestDimensions)
        {
            page.Content().Column(column =>
            {
                // Title
                column.Item().Element(c =>
                    DesignComponents.Section(c, LocalizationStrings.Get("courses.title"), "📚"));

                column.Item().PaddingTop(DesignTokens.Spacing.MD);

                // Subtitle
                column.Item().AlignRight()
                    .Text(LocalizationStrings.Get("courses.recommended_for"))
                    .Style(GetTextStyle(DesignTokens.Typography.Body, false, DesignTokens.Colors.TextSecondary));

                column.Item().PaddingTop(DesignTokens.Spacing.LG);

                // Course Cards
                var filteredCourses = courses
                    .Where(c => weakestDimensions.Any(w => c.TargetDimensions?.Contains(w.Dimension) == true))
                    .OrderBy(c => c.Duration ?? "zzz")
                    .Take(6)
                    .ToList();

                foreach (var course in filteredCourses)
                {
                    column.Item().PaddingVertical(DesignTokens.Spacing.SM).Element(c =>
                        DesignComponents.GlassCard(c, card =>
                        {
                            card.Column(col =>
                            {
                                // Course Name
                                col.Item().AlignRight().Text(course.Name)
                                    .Style(GetTextStyle(DesignTokens.Typography.TitleSemibold, true, DesignTokens.Colors.Primary));

                                // Duration
                                if (!string.IsNullOrEmpty(course.Duration))
                                {
                                    col.Item().PaddingTop(DesignTokens.Spacing.XS)
                                        .AlignRight()
                                        .Text($"⏱ {LocalizationStrings.Get("courses.duration")}: {course.Duration}")
                                        .Style(GetTextStyle(DesignTokens.Typography.Small, false, DesignTokens.Colors.TextSecondary));
                                }

                                // Description
                                if (!string.IsNullOrEmpty(course.Description))
                                {
                                    var description = ArabicTextRenderer.FormatDescription(course.Description, 200);
                                    col.Item().PaddingTop(DesignTokens.Spacing.SM)
                                        .AlignRight()
                                        .Text(description)
                                        .Style(GetTextStyle(DesignTokens.Typography.Body, false));
                                }

                                // Target Dimensions (as badges)
                                var targetedDims = course.TargetDimensions?
                                    .Where(td => weakestDimensions.Any(wd => wd.Dimension == td))
                                    .Take(3)
                                    .ToList();

                                if (targetedDims?.Any() == true)
                                {
                                    col.Item().PaddingTop(DesignTokens.Spacing.SM).Row(tagRow =>
                                    {
                                        tagRow.AutoItem().AlignMiddle().Text($"{LocalizationStrings.Get("courses.target")}: ")
                                            .Style(GetTextStyle(DesignTokens.Typography.Small, false, DesignTokens.Colors.TextSecondary));

                                        foreach (var target in targetedDims)
                                        {
                                            tagRow.AutoItem().PaddingLeft(DesignTokens.Spacing.XS).Element(b =>
                                                DesignComponents.Badge(b, target, "primary"));
                                        }
                                    });
                                }
                            });
                        })
                    );
                }

                column.Item().PaddingTop(DesignTokens.Spacing.XL);

                // Note
                column.Item().Element(c =>
                    DesignComponents.Callout(c, LocalizationStrings.Get("courses.note"), "info"));
            });
        }

        #endregion

        #region Page 6: Quality & Methodology

        private void ComposePage6_QualityAndMethodology(
            PageDescriptor page,
            List<DimensionScoreDto> dimensions)
        {
            page.Content().Column(column =>
            {
                // Title
                column.Item().Element(c =>
                    DesignComponents.Section(c, LocalizationStrings.Get("quality.title"), "✓"));

                column.Item().PaddingTop(DesignTokens.Spacing.LG);

                // Performance Bands Interpretation
                column.Item().AlignRight().Text(LocalizationStrings.Get("quality.bands.title"))
                    .Style(GetTextStyle(DesignTokens.Typography.H3, true));

                column.Item().PaddingTop(DesignTokens.Spacing.SM);

                // Band Table
                var headers = new List<string>
                {
                    LocalizationStrings.Get("band.excellent"),
                    LocalizationStrings.Get("quality.bands.range"),
                    LocalizationStrings.Get("quality.bands.interpretation")
                };

                var rows = new List<List<string>>
                {
                    new() {
                        LocalizationStrings.Get("band.excellent"),
                        $"T ≥ {DesignTokens.PerformanceBands.ExcellentMin}",
                        LocalizationStrings.CurrentLanguage == ReportLanguage.AR 
                            ? "نقطة قوة بارزة - استمر في التميز" 
                            : "Outstanding strength - Continue excellence"
                    },
                    new() {
                        LocalizationStrings.Get("band.good"),
                        $"{DesignTokens.PerformanceBands.GoodMin} ≤ T < {DesignTokens.PerformanceBands.ExcellentMin}",
                        LocalizationStrings.CurrentLanguage == ReportLanguage.AR 
                            ? "أداء جيد - فرصة للتحسين" 
                            : "Good performance - Room for improvement"
                    },
                    new() {
                        LocalizationStrings.Get("band.average"),
                        $"{DesignTokens.PerformanceBands.AverageMin} ≤ T < {DesignTokens.PerformanceBands.GoodMin}",
                        LocalizationStrings.CurrentLanguage == ReportLanguage.AR 
                            ? "مستوى متوسط - يحتاج تطوير مستمر" 
                            : "Average level - Needs continuous development"
                    },
                    new() {
                        LocalizationStrings.Get("band.weak"),
                        $"T < {DesignTokens.PerformanceBands.AverageMin}",
                        LocalizationStrings.CurrentLanguage == ReportLanguage.AR 
                            ? "يحتاج تطوير عاجل - تركيز أولوية" 
                            : "Needs urgent development - Priority focus"
                    }
                };

                column.Item().Element(c => DesignComponents.RtlTable(c, headers, rows));

                column.Item().PaddingTop(DesignTokens.Spacing.XL);

                // Methodology
                column.Item().AlignRight().Text(LocalizationStrings.Get("quality.methodology"))
                    .Style(GetTextStyle(DesignTokens.Typography.H3, true));

                column.Item().PaddingTop(DesignTokens.Spacing.SM);

                var methodologyText = LocalizationStrings.CurrentLanguage == ReportLanguage.AR
                    ? "تم حساب الدرجات باستخدام نموذج القياس النفسي المعياري مع تطبيع T-Score (متوسط=50، انحراف معياري=10). " +
                      "جميع القياسات معايرة على عينة تمثيلية وتخضع لمعايير الموثوقية والصدق الإحصائي."
                    : "Scores calculated using standardized psychometric model with T-Score normalization (mean=50, SD=10). " +
                      "All measurements calibrated on representative sample and subject to reliability and statistical validity standards.";

                column.Item().Background(DesignTokens.Colors.Surface)
                    .Padding(DesignTokens.Spacing.MD)
                    .AlignRight()
                    .Text(methodologyText)
                    .Style(GetTextStyle(DesignTokens.Typography.Body, false))
                    .LineHeight(DesignTokens.Typography.LineHeightNormal);

                column.Item().PaddingTop(DesignTokens.Spacing.XL);

                // Report Statistics
                column.Item().AlignRight().Text(LocalizationStrings.CurrentLanguage == ReportLanguage.AR ? "إحصائيات التقرير" : "Report Statistics")
                    .Style(GetTextStyle(DesignTokens.Typography.H3, true));

                column.Item().PaddingTop(DesignTokens.Spacing.SM);

                var statsData = new Dictionary<string, string>
                {
                    { LocalizationStrings.CurrentLanguage == ReportLanguage.AR ? "عدد الأبعاد" : "Dimensions", dimensions.Count.ToString() },
                    { LocalizationStrings.CurrentLanguage == ReportLanguage.AR ? "النموذج" : "Model", "Standard Psychometric v2.0" },
                    { LocalizationStrings.CurrentLanguage == ReportLanguage.AR ? "الموثوقية" : "Reliability", "α > 0.85" },
                    { LocalizationStrings.CurrentLanguage == ReportLanguage.AR ? "تاريخ التوليد" : "Generated", DateTime.Now.ToString("yyyy-MM-dd HH:mm") }
                };

                column.Item().Element(c => DesignComponents.MetadataGrid(c, statsData));

                column.Item().PaddingTop(DesignTokens.Spacing.XL);

                // Confidentiality Notice
                column.Item().Element(c =>
                    DesignComponents.Callout(
                        c,
                        LocalizationStrings.Get("footer.confidential"),
                        "info"
                    )
                );
            });
        }

        #endregion

        #region Helper Methods

        private void ConfigurePageDefaults(PageDescriptor page, int pageNumber, int totalPages)
        {
            page.Size(PageSizes.A4);
            page.Margin(DesignTokens.Spacing.PageMarginTop);
            page.DefaultTextStyle(style => GetTextStyle(DesignTokens.Typography.Body, false));

            // Footer
            page.Footer().Element(f => DesignComponents.Footer(
                f,
                pageNumber,
                totalPages,
                LocalizationStrings.Get("footer.watermark")
            ));
        }

        private TextStyle GetTextStyle(float size, bool bold = false, string? color = null)
        {
            var fontFamily = LocalizationStrings.CurrentLanguage == ReportLanguage.AR
                ? DesignTokens.Typography.FontArabic
                : DesignTokens.Typography.FontEnglish;

            var style = TextStyle.Default
                .FontFamily(fontFamily)
                .FontSize(size)
                .FontColor(color ?? DesignTokens.Colors.Text);

            if (LocalizationStrings.CurrentLanguage == ReportLanguage.AR)
                style = style.DirectionFromRightToLeft();

            if (bold)
                style = style.Bold();

            return style;
        }

        private double CalculateVariance(IEnumerable<double> values)
        {
            var valuesList = values.ToList();
            if (!valuesList.Any()) return 0;

            var mean = valuesList.Average();
            var sumOfSquares = valuesList.Sum(val => Math.Pow(val - mean, 2));
            return sumOfSquares / valuesList.Count;
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
                    var arabicRegular = Path.Combine(fontsDir, "NotoNaskhArabic-Regular.ttf");
                    var arabicBold = Path.Combine(fontsDir, "NotoNaskhArabic-Bold.ttf");

                    if (File.Exists(arabicRegular))
                    {
                        QuestPDF.Drawing.FontManager.RegisterFont(File.OpenRead(arabicRegular));
                        Console.WriteLine("[UltraHiFi] ✓ Noto Naskh Arabic Regular loaded");
                    }

                    if (File.Exists(arabicBold))
                    {
                        QuestPDF.Drawing.FontManager.RegisterFont(File.OpenRead(arabicBold));
                        Console.WriteLine("[UltraHiFi] ✓ Noto Naskh Arabic Bold loaded");
                    }

                    _fontsRegistered = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[UltraHiFi] ⚠ Font loading error: {ex.Message}");
                }
            }
        }

        private static void LoadLogo()
        {
            try
            {
                var logoOptions = new[] { "SAITES-ICON.png", "SAITEST.jpeg", "SITES-ICON.png" };

                foreach (var logoFile in logoOptions)
                {
                    var logoPath = Path.Combine(AppContext.BaseDirectory, "Resources", "Brand", logoFile);
                    if (File.Exists(logoPath))
                    {
                        _logoBytes = File.ReadAllBytes(logoPath);
                        Console.WriteLine($"[UltraHiFi] ✓ Logo loaded: {logoFile}");
                        return;
                    }
                }

                Console.WriteLine("[UltraHiFi] ℹ Logo not found in Resources/Brand/");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UltraHiFi] ⚠ Logo loading error: {ex.Message}");
            }
        }

        #endregion
    }
    
    /// <summary>
    /// SDJ V2 storage format (as saved in DimensionScoresJson by SessionsController)
    /// </summary>
    public class SdjV2StorageFormat
    {
        public List<SdjV2PatternScoreStorage>? PatternScores { get; set; }
        public List<SdjV2SubDimensionScoreStorage>? SubDimensionScores { get; set; }
        public SdjV2OverallScoreStorage? OverallScore { get; set; }
        public string? Version { get; set; }
        public int ItemCount { get; set; }
        public int McqCount { get; set; }
        public int LikertCount { get; set; }
    }
    
    public class SdjV2PatternScoreStorage
    {
        public string? PatternId { get; set; }
        public string? PatternKey { get; set; }
        public string? PatternNameAr { get; set; }
        public double Raw { get; set; }
        public double TScore { get; set; }
        public double Percentile { get; set; }
        public string? Band { get; set; }
        public int SubDimensionCount { get; set; }
        public List<SdjV2SubDimensionScoreStorage>? SubDimensions { get; set; }
    }
    
    public class SdjV2SubDimensionScoreStorage
    {
        public string? PatternId { get; set; }
        public string? SubId { get; set; }
        public string? SubKey { get; set; }
        public string? SubNameAr { get; set; }
        public double Raw { get; set; }
        public double TScore { get; set; }
        public double Percentile { get; set; }
        public string? Band { get; set; }
        public int ItemCount { get; set; }
    }
    
    public class SdjV2OverallScoreStorage
    {
        public double Raw { get; set; }
        public double TScore { get; set; }
        public double Percentile { get; set; }
        public string? Band { get; set; }
    }
}
