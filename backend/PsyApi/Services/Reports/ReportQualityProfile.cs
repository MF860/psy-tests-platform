namespace PsyApi.Services.Reports
{
    /// <summary>
    /// Quality profile for PDF report generation
    /// HiFi mode: Maximum quality, vector-first, no size constraints
    /// </summary>
    public enum ReportQualityProfile
    {
        /// <summary>
        /// Standard quality (current implementation)
        /// </summary>
        Standard,

        /// <summary>
        /// High-Fidelity mode: Vector-first, DPI≥300, full antialiasing, no compression
        /// Priority: Visual quality over file size
        /// </summary>
        HiFi
    }

    /// <summary>
    /// Configuration settings for HiFi report generation
    /// </summary>
    public static class HiFiSettings
    {
        /// <summary>
        /// Current quality profile (globally configurable)
        /// </summary>
        public static ReportQualityProfile ActiveProfile { get; set; } = ReportQualityProfile.HiFi;

        /// <summary>
        /// DPI for raster graphics in HiFi mode
        /// </summary>
        public const int HiFiDpi = 300;

        /// <summary>
        /// Scale factor for bitmap rendering (3× = 3600px width for A4)
        /// </summary>
        public const float HiFiScaleFactor = 3f;

        /// <summary>
        /// JPEG quality (100 = no compression loss)
        /// </summary>
        public const int HiFiJpegQuality = 100;

        /// <summary>
        /// Enable subpixel antialiasing
        /// </summary>
        public const bool EnableSubpixelAntialiasing = true;

        /// <summary>
        /// Enable text hinting for sharp rendering
        /// </summary>
        public const bool EnableTextHinting = true;

        /// <summary>
        /// Page margins in points (36-40pt = ~12-14mm)
        /// </summary>
        public const float PageMargin = 40f;

        /// <summary>
        /// Vertical spacing scale (8/12/16/24pt)
        /// </summary>
        public static class Spacing
        {
            public const float XS = 4f;
            public const float SM = 8f;
            public const float MD = 12f;
            public const float LG = 16f;
            public const float XL = 24f;
            public const float XXL = 32f;
        }

        /// <summary>
        /// Check if HiFi mode is active
        /// </summary>
        public static bool IsHiFi => ActiveProfile == ReportQualityProfile.HiFi;

        /// <summary>
        /// Get appropriate scale factor based on active profile
        /// </summary>
        public static float GetScaleFactor() => IsHiFi ? HiFiScaleFactor : 2f;

        /// <summary>
        /// Get appropriate JPEG quality based on active profile
        /// </summary>
        public static int GetJpegQuality() => IsHiFi ? HiFiJpegQuality : 85;

        /// <summary>
        /// Get SkiaSharp paint with HiFi settings (modern API - font properties moved to SKFont)
        /// </summary>
        public static SkiaSharp.SKPaint GetHiFiPaint()
        {
            return new SkiaSharp.SKPaint
            {
                IsAntialias = true
                // Note: FilterQuality, HintingLevel, SubpixelText, LcdRenderText are obsolete
                // Modern approach: Use SKSamplingOptions and configure SKFont directly
            };
        }
    }
}
