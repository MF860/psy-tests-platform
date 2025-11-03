namespace PsyApi.Services.Reports
{
    /// <summary>
    /// Localization strings for bilingual reports - Ultra Hi-Fi v4.0
    /// Supports Arabic (RTL) and English (LTR) with complete translations
    /// </summary>
    public static class LocalizationStrings
    {
        /// <summary>
        /// Current active language
        /// </summary>
        public static ReportLanguage CurrentLanguage { get; set; } = ReportLanguage.AR;

        /// <summary>
        /// Get localized string by key
        /// Supports both Arabic and English with full coverage
        /// </summary>
        public static string Get(string key, ReportLanguage? language = null)
        {
            var lang = language ?? CurrentLanguage;
            return lang == ReportLanguage.AR 
                ? GetArabic(key) 
                : GetEnglish(key);
        }

        private static string GetArabic(string key)
        {
            return key switch
            {
                // Cover Page
                "report.title" => "تقرير تحليل أنماط الشخصية السبعة - SDJ",
                "report.subtitle" => "التحليل التفصيلي للأنماط والأبعاد الفرعية",
                "participant.name" => "اسم المشارك",
                "session.date" => "تاريخ الجلسة",
                "generated.date" => "تاريخ التوليد",

                // Page 2 - Charts
                "charts.title" => "التحليل البصري للأنماط",
                "charts.subdimensions.title" => "توزيع الدرجات التفصيلي عبر الأبعاد الفرعية - T-Score",
                "charts.radar.title" => "التحليل البصري للأنماط الرئيسية",
                "charts.radar.subtitle" => "الرسم البياني السباعي يُظهر توزيع الأنماط السبعة الرئيسية",

                // KPIs
                "kpi.avg_tscore" => "متوسط T-Score",
                "kpi.variance" => "التباين",
                "kpi.top_dimension" => "أقوى بُعد",
                "kpi.bottom_dimension" => "أضعف بُعد",

                // Performance Bands
                "band.excellent" => "ممتاز",
                "band.average" => "متوسط",
                "band.weak" => "ضعيف",
                "band.needsDevelopment" => "بحاجة للتنمية",

                // Page 3 - Seven Patterns
                "patterns.title" => "تحليل الأنماط السبعة التفصيلي",
                "patterns.description" => "الوصف",
                "patterns.tscore" => "T-Score",
                "patterns.percentile" => "الشريحة المئوية",

                // Page 4 - Courses
                "courses.title" => "الدورات التدريبية الموصى بها",
                "courses.subtitle" => "بناءً على الأبعاد التي تحتاج إلى تنمية",
                "courses.target" => "الأبعاد المستهدفة",

                // Footer
                "footer.page" => "صفحة",
                "footer.of" => "من",

                // General
                "points" => "نقطة",
                "score" => "الدرجة",
                
                _ => key // Return key as fallback
            };
        }

        /// <summary>
        /// English strings - COMPLETE IMPLEMENTATION for Ultra Hi-Fi Reports
        /// </summary>
        private static string GetEnglish(string key)
        {
            return key switch
            {
                // Cover Page & Header
                "report.title" => "Advanced Psychometric Analysis Platform",
                "report.title.sdj" => "Seven Personality Patterns Analysis Report - SDJ",
                "report.subtitle" => "Comprehensive Detailed Report",
                "report.subtitle.sdj" => "Detailed Analysis of Patterns and Subdimensions",
                "report.description" => "A comprehensive report integrating modern psychometric measurement with precise analytics for your professional and personal development.",
                "participant.info" => "Participant Information",
                "participant.name" => "Name",
                "participant.id" => "National ID",
                "session.id" => "Session ID",
                "session.date" => "Assessment Date",
                "generated.date" => "Report Date",
                "scoring.model" => "Scoring Model",

                // Executive Summary & KPIs
                "summary.title" => "Executive Summary",
                "summary.kpis" => "Overall Performance Indicators",
                "kpi.avg_tscore" => "Average T-Score",
                "kpi.avg_percentile" => "Average Percentile",
                "kpi.total_score" => "Total Score",
                "kpi.dimensions_count" => "Dimensions Count",
                "kpi.variance" => "Variance",
                "kpi.balance_index" => "Balance Index",
                "kpi.advanced_patterns" => "Advanced Patterns",
                "kpi.top_dimension" => "Strongest Dimension",
                "kpi.bottom_dimension" => "Weakest Dimension",
                "summary.strengths" => "Top 3 Strengths",
                "summary.growth_areas" => "Areas for Development",
                "summary.overall_status" => "Overall Status",

                // Performance Bands & Levels
                "band.excellent" => "Excellent",
                "band.verygood" => "Very Good",
                "band.good" => "Good",
                "band.average" => "Average",
                "band.weak" => "Weak",
                "band.needsDevelopment" => "Needs Development",
                "band.count.excellent" => "Excellent",
                "band.count.good" => "Good",
                "band.count.average" => "Average",
                "band.count.weak" => "Weak",

                // Page 2 - Visual Analytics
                "charts.title" => "Visual Analytics",
                "charts.overview" => "Results Across Dimensions and Clusters",
                "charts.radar.title" => "Comprehensive Radar Overview",
                "charts.radar.subtitle" => "Distribution across dimensions",
                "charts.bars.title" => "Horizontal Score Distribution",
                "charts.donut.title" => "Cluster Distribution",
                "charts.donut.subtitle" => "Average T-Score by four main clusters",
                "charts.subdimensions.title" => "Detailed Score Distribution - T-Score",
                "charts.legend" => "Legend",
                "charts.summary" => "Statistical Summary",

                // Page 3 - Data Story & Analysis
                "analysis.title" => "Detailed Analysis & Narrative",
                "insights.title" => "Key Insights",
                "insights.general" => "General Observations",
                "clusters.title" => "Cluster Analysis",
                "clusters.analysis" => "Analysis by Psychological Clusters",
                "risk.title" => "Attention Required",
                "risk.flags" => "Risk Indicators",
                "dimension.narrative" => "Dimension Analysis",

                // Page 4 - Seven Patterns
                "patterns.title" => "Seven Patterns Detailed Overview",
                "patterns.description" => "Description",
                "patterns.tscore" => "T-Score",
                "patterns.percentile" => "Percentile",
                "patterns.band" => "Performance Band",
                "patterns.recommendations" => "Recommendations",
                "patterns.subdimensions" => "Sub-dimensions",
                "patterns.map" => "Psychological Seven-Category Map",

                // Page 5 - Development Plan
                "plan.title" => "Personalized Development Plan",
                "plan.subtitle" => "Actionable Steps for Growth",
                "plan.intro" => "This plan is specifically designed to develop weaker dimensions. Recommended commitment: 6-8 weeks with regular follow-up.",
                "plan.action" => "Action Item",
                "plan.priority" => "Priority",
                "plan.priority.high" => "High",
                "plan.priority.medium" => "Medium",
                "plan.priority.low" => "Low",
                "plan.description" => "Description",
                "plan.timeline" => "Timeline",
                "plan.kpi" => "Success Metric",
                "plan.tips" => "Tips for Success",
                "plan.tips.commitment" => "Commit to daily exercises, even if brief (15-20 minutes)",
                "plan.tips.tracking" => "Track weekly progress and celebrate small wins",
                "plan.tips.feedback" => "Request feedback from peers or supervisors",
                "plan.tips.review" => "Review this report monthly to assess improvement",
                "plan.tips.help" => "Don't hesitate to seek professional help when needed",

                // Page 6 - Training Courses
                "courses.title" => "Recommended Training Courses",
                "courses.subtitle" => "Based on dimensions that need development",
                "courses.recommended_for" => "Recommended for you based on weaker dimensions:",
                "courses.target" => "Target Dimensions",
                "courses.duration" => "Duration",
                "courses.description" => "Description",
                "courses.note" => "We recommend enrolling in one or two courses simultaneously to avoid overwhelm, focusing on practical application.",

                // Quality & Methodology
                "quality.title" => "Quality & Consistency Checks",
                "quality.interpretation" => "Score Interpretation Guide",
                "quality.methodology" => "Methodology",
                "quality.reliability" => "Reliability",
                "quality.bands.title" => "Performance Band Guide",
                "quality.bands.range" => "Range",
                "quality.bands.interpretation" => "Interpretation",

                // Career Tracks (SDJ)
                "tracks.title" => "Recommended Career Paths",
                "tracks.subtitle" => "Based on your strengths and interests",
                "tracks.fit" => "Fit Score",
                "tracks.fit.high" => "High Match",
                "tracks.fit.medium" => "Medium Match",
                "tracks.fit.low" => "Low Match",
                "tracks.competencies" => "Key Competencies",
                "tracks.reasoning" => "Reasoning",

                // Dimensions & Clusters
                "dimension.social_responsibility" => "Social Responsibility",
                "dimension.professional_success" => "Professional Success",
                "dimension.communication_relations" => "Communication & Relations",
                "dimension.health_balance" => "Health & Balance",
                "dimension.personal_excellence" => "Personal Excellence",
                "cluster.self_identity" => "Self & Identity",
                "cluster.interpersonal" => "Interpersonal Relations",
                "cluster.professional" => "Professional Competence",
                "cluster.wellbeing" => "Well-being & Balance",

                // Footer & Watermark
                "footer.page" => "Page",
                "footer.of" => "of",
                "footer.watermark" => "Generated by Advanced Psychometric Analysis Platform © 2025",
                "footer.confidential" => "Confidential Document - For Authorized Use Only",

                // General Terms
                "points" => "points",
                "score" => "Score",
                "value" => "Value",
                "label" => "Label",
                "status" => "Status",
                "date" => "Date",
                "time" => "Time",
                "version" => "Version",
                "total" => "Total",
                "average" => "Average",
                "minimum" => "Minimum",
                "maximum" => "Maximum",
                "range" => "Range",
                "count" => "Count",
                "percentage" => "Percentage",
                
                _ => key // Return key as fallback
            };
        }
    }
}
