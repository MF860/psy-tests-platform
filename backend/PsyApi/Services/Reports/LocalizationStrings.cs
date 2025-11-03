namespace PsyApi.Services.Reports
{
    /// <summary>
    /// Localization strings for bilingual reports (future feature)
    /// Arabic is the primary language, English is prepared but not activated
    /// </summary>
    public static class LocalizationStrings
    {
        /// <summary>
        /// Get localized string by key
        /// Currently returns Arabic only (EN implementation pending)
        /// </summary>
        public static string Get(string key, ReportLanguage language = ReportLanguage.AR)
        {
            return language == ReportLanguage.AR 
                ? GetArabic(key) 
                : GetEnglish(key); // Not yet activated
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
        /// English strings (placeholder - not yet activated)
        /// </summary>
        private static string GetEnglish(string key)
        {
            return key switch
            {
                // Cover Page
                "report.title" => "Seven Personality Patterns Analysis Report - SDJ",
                "report.subtitle" => "Detailed Analysis of Patterns and Subdimensions",
                "participant.name" => "Participant Name",
                "session.date" => "Session Date",
                "generated.date" => "Generated Date",

                // Page 2 - Charts
                "charts.title" => "Visual Pattern Analysis",
                "charts.subdimensions.title" => "Detailed Score Distribution Across Subdimensions - T-Score",
                "charts.radar.title" => "Main Patterns Visual Analysis",
                "charts.radar.subtitle" => "Heptagon chart shows distribution of seven main patterns",

                // KPIs
                "kpi.avg_tscore" => "Avg T-Score",
                "kpi.variance" => "Variance",
                "kpi.top_dimension" => "Top Dimension",
                "kpi.bottom_dimension" => "Bottom Dimension",

                // Performance Bands
                "band.excellent" => "Excellent",
                "band.average" => "Average",
                "band.weak" => "Weak",
                "band.needsDevelopment" => "Needs Development",

                // Page 3 - Seven Patterns
                "patterns.title" => "Detailed Seven Patterns Analysis",
                "patterns.description" => "Description",
                "patterns.tscore" => "T-Score",
                "patterns.percentile" => "Percentile",

                // Page 4 - Courses
                "courses.title" => "Recommended Training Courses",
                "courses.subtitle" => "Based on dimensions that need development",
                "courses.target" => "Target Dimensions",

                // Footer
                "footer.page" => "Page",
                "footer.of" => "of",

                // General
                "points" => "points",
                "score" => "Score",
                
                _ => key // Return key as fallback
            };
        }
    }
}
