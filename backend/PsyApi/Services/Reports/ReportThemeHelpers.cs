using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System;

namespace PsyApi.Services.Reports
{
    /// <summary>
    /// HiFi UI Component Helpers for professional PDF reports v3.0
    /// Enhanced with trend indicators, progress bars, and advanced layouts
    /// </summary>
    public static class ReportThemeHelpers
    {
        /// <summary>
        /// Create a large KPI card with value, label, and optional trend
        /// HiFi version with enhanced visual hierarchy
        /// </summary>
        public static void KpiCard(this IContainer container, string value, string label, 
            string? valueColor = null, string? trendIcon = null, string? trendText = null)
        {
            container
                .Background(ReportTheme.Colors.Surface)
                .Border(1)
                .BorderColor(ReportTheme.Colors.Border)
                .Padding(HiFiSettings.Spacing.LG)
                .Column(col =>
                {
                    // Main value (large and prominent)
                    col.Item()
                        .AlignCenter()
                        .Text(UnicodeTextHelper.NormalizeNfc(value))
                        .Style(ReportTheme.ArabicTextStyle(ReportTheme.Typography.KPI, true, 
                            valueColor ?? ReportTheme.Colors.Primary));
                    
                    // Label
                    col.Item()
                        .PaddingTop(HiFiSettings.Spacing.XS)
                        .AlignCenter()
                        .Text(UnicodeTextHelper.NormalizeNfc(label))
                        .Style(ReportTheme.ArabicTextStyle(ReportTheme.Typography.KPILabel, false, 
                            ReportTheme.Colors.TextSecondary));

                    // Optional trend indicator
                    if (!string.IsNullOrEmpty(trendIcon) || !string.IsNullOrEmpty(trendText))
                    {
                        col.Item()
                            .PaddingTop(HiFiSettings.Spacing.SM)
                            .AlignCenter()
                            .Row(row =>
                            {
                                if (!string.IsNullOrEmpty(trendIcon))
                                {
                                    row.AutoItem()
                                        .Text(trendIcon)
                                        .FontSize(ReportTheme.Typography.Icon)
                                        .FontColor(ReportTheme.Colors.Success);
                                }
                                if (!string.IsNullOrEmpty(trendText))
                                {
                                    row.AutoItem()
                                        .PaddingLeft(4)
                                        .Text(UnicodeTextHelper.NormalizeNfc(trendText))
                                        .Style(ReportTheme.ArabicTextStyle(ReportTheme.Typography.BodySmall, 
                                            false, ReportTheme.Colors.TextMuted));
                                }
                            });
                    }
                });
        }

        /// <summary>
        /// Enhanced badge with optional icon
        /// </summary>
        public static void BadgeWithIcon(this IContainer container, string text, string bgColor, 
            string? icon = null, string textColor = "#FFFFFF", float fontSize = 0)
        {
            if (fontSize == 0) fontSize = ReportTheme.Typography.Badge;

            container
                .Background(bgColor)
                .PaddingVertical(6)
                .PaddingHorizontal(12)
                .AlignCenter()
                .Row(row =>
                {
                    if (!string.IsNullOrEmpty(icon))
                    {
                        row.AutoItem()
                            .PaddingRight(4)
                            .Text(icon)
                            .FontSize(ReportTheme.Typography.Icon)
                            .FontColor(textColor);
                    }
                    row.AutoItem()
                        .Text(UnicodeTextHelper.NormalizeNfc(text))
                        .Style(ReportTheme.ArabicTextStyle(fontSize, true, textColor));
                });
        }
        /// <summary>
        /// Create a colored badge/chip with text
        /// </summary>
        public static void Badge(this IContainer container, string text, string bgColor, string textColor = "#FFFFFF", float fontSize = 10f, bool bold = false)
        {
            container
                .Background(bgColor)
                .PaddingVertical(4)
                .PaddingHorizontal(8)
                .AlignCenter()
                .Text(text)
                .Style(ReportTheme.ArabicTextStyle(fontSize, bold, textColor));
        }

        /// <summary>
        /// Create a KPI card component with large number and label
        /// </summary>
        public static void Kpi(this IContainer container, string value, string label, string? valueColor = null, string? labelColor = null)
        {
            container.Column(col =>
            {
                col.Item()
                    .AlignCenter()
                    .Text(value)
                    .Style(ReportTheme.ArabicTextStyle(ReportTheme.Typography.KPI, true, valueColor ?? ReportTheme.Colors.Primary));
                
                col.Item()
                    .PaddingTop(ReportTheme.Spacing.XS)
                    .AlignCenter()
                    .Text(label)
                    .Style(ReportTheme.ArabicTextStyle(ReportTheme.Typography.BodySmall, false, labelColor ?? ReportTheme.Colors.TextSecondary));
            });
        }

        /// <summary>
        /// Create a section title with optional icon/emoji
        /// </summary>
        public static void SectionTitle(this IContainer container, string title, string? icon = null)
        {
            container.Row(row =>
            {
                if (!string.IsNullOrEmpty(icon))
                {
                    row.AutoItem()
                        .PaddingLeft(ReportTheme.Spacing.SM)
                        .Text(icon)
                        .FontSize(ReportTheme.Typography.H2);
                }
                
                row.RelativeItem()
                    .Text(title)
                    .Style(ReportTheme.ArabicTextStyle(ReportTheme.Typography.H2, true, ReportTheme.Colors.Text));
            });
        }

        /// <summary>
        /// Create a card container with border, background, and padding
        /// </summary>
        public static void Card(this IContainer container, Action<ColumnDescriptor> content)
        {
            container
                .Background(ReportTheme.Colors.Surface)
                .Border(1)
                .BorderColor(ReportTheme.Colors.Border)
                .Padding(ReportTheme.Spacing.MD)
                .Column(content);
        }

        /// <summary>
        /// Create a performance band badge based on T-Score
        /// </summary>
        public static void PerformanceBadge(this IContainer container, double tScore)
        {
            var label = ReportTheme.GetPerformanceLabel(tScore);
            var color = ReportTheme.GetBandColor(tScore);
            container.Badge(label, color, "#FFFFFF", 11f, true);
        }

        /// <summary>
        /// Create a divider line (horizontal separator)
        /// </summary>
        public static void Divider(this IContainer container, float thickness = 1f, string? color = null)
        {
            container
                .Height(thickness)
                .Background(color ?? ReportTheme.Colors.Divider);
        }

        /// <summary>
        /// Create a simple info row (label: value) with RTL support
        /// </summary>
        public static void InfoRow(this IContainer container, string label, string? value)
        {
            container.Row(row =>
            {
                row.RelativeItem()
                    .AlignRight()
                    .PaddingVertical(4)
                    .Text(label + ":")
                    .Style(ReportTheme.ArabicTextStyle(ReportTheme.Typography.Body, true, ReportTheme.Colors.TextSecondary));
                
                row.RelativeItem()
                    .AlignRight()
                    .PaddingVertical(4)
                    .Text(value ?? "غير محدد")
                    .Style(ReportTheme.ArabicTextStyle(ReportTheme.Typography.Body, false, ReportTheme.Colors.Text));
            });
        }

        /// <summary>
        /// Create a progress bar with percentage
        /// </summary>
        public static void ProgressBar(this IContainer container, double percentage, string? label = null, string? color = null)
        {
            percentage = Math.Clamp(percentage, 0, 100);
            var barColor = color ?? ReportTheme.Colors.Primary;

            container.Column(col =>
            {
                if (!string.IsNullOrEmpty(label))
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem()
                            .Text(label)
                            .Style(ReportTheme.ArabicTextStyle(ReportTheme.Typography.BodySmall, false, ReportTheme.Colors.TextSecondary));
                        
                        row.AutoItem()
                            .Text(ReportTheme.FormatPercent(percentage))
                            .Style(ReportTheme.ArabicTextStyle(ReportTheme.Typography.BodySmall, true, ReportTheme.Colors.Text));
                    });
                }

                col.Item()
                    .PaddingTop(ReportTheme.Spacing.XS)
                    .Height(8)
                    .Background(ReportTheme.Colors.Track)
                    .Row(row =>
                    {
                        row.RelativeItem((float)percentage).Background(barColor);
                        row.RelativeItem((float)(100 - percentage));
                    });
            });
        }

        /// <summary>
        /// Create an icon badge (emoji/unicode symbol with background)
        /// </summary>
        public static void IconBadge(this IContainer container, string icon, string bgColor, string iconColor = "#FFFFFF")
        {
            container
                .Width(32)
                .Height(32)
                .Background(bgColor)
                .AlignCenter()
                .AlignMiddle()
                .Text(icon)
                .FontSize(16)
                .FontColor(iconColor);
        }

        /// <summary>
        /// Create a professional section header with icon and optional subtitle
        /// </summary>
        public static void SectionHeader(this IContainer container, string title, string? icon = null, string? subtitle = null)
        {
            container.Column(col =>
            {
                // Title row
                col.Item().Row(row =>
                {
                    if (!string.IsNullOrEmpty(icon))
                    {
                        row.AutoItem()
                            .PaddingLeft(HiFiSettings.Spacing.SM)
                            .Text(icon)
                            .FontSize(ReportTheme.Typography.H2)
                            .FontColor(ReportTheme.Colors.Primary);
                    }
                    
                    row.RelativeItem()
                        .Text(UnicodeTextHelper.NormalizeNfc(title))
                        .Style(ReportTheme.ArabicTextStyle(ReportTheme.Typography.H2, true, ReportTheme.Colors.Text));
                });

                // Optional subtitle
                if (!string.IsNullOrEmpty(subtitle))
                {
                    col.Item()
                        .PaddingTop(HiFiSettings.Spacing.XS)
                        .Text(UnicodeTextHelper.NormalizeNfc(subtitle))
                        .Style(ReportTheme.ArabicTextStyle(ReportTheme.Typography.Body, false, ReportTheme.Colors.TextSecondary));
                }

                // Underline
                col.Item()
                    .PaddingTop(HiFiSettings.Spacing.SM)
                    .Height(2)
                    .Background(ReportTheme.Colors.Primary);
            });
        }

        /// <summary>
        /// Stat row for key-value pairs with enhanced styling
        /// </summary>
        public static void StatRow(this IContainer container, string label, string value, string? valueColor = null)
        {
            container
                .PaddingVertical(HiFiSettings.Spacing.SM)
                .Row(row =>
                {
                    row.RelativeItem()
                        .AlignRight()
                        .Text(UnicodeTextHelper.NormalizeNfc(label))
                        .Style(ReportTheme.ArabicTextStyle(ReportTheme.Typography.Body, true, ReportTheme.Colors.TextSecondary));
                    
                    row.RelativeItem()
                        .AlignRight()
                        .Text(UnicodeTextHelper.NormalizeNfc(value))
                        .Style(ReportTheme.ArabicTextStyle(ReportTheme.Typography.Body, false, 
                            valueColor ?? ReportTheme.Colors.Text));
                });
        }

        /// <summary>
        /// Progress indicator with percentage and visual bar
        /// </summary>
        public static void ProgressIndicator(this IContainer container, double percentage, string label, 
            string? color = null, bool showPercentage = true)
        {
            percentage = Math.Clamp(percentage, 0, 100);
            var barColor = color ?? ReportTheme.Colors.Primary;

            container.Column(col =>
            {
                // Label row
                col.Item().Row(row =>
                {
                    row.RelativeItem()
                        .Text(UnicodeTextHelper.NormalizeNfc(label))
                        .Style(ReportTheme.ArabicTextStyle(ReportTheme.Typography.Body, false, ReportTheme.Colors.Text));
                    
                    if (showPercentage)
                    {
                        row.AutoItem()
                            .Text(UnicodeTextHelper.FormatPercentage(percentage / 100, 0))
                            .Style(ReportTheme.ArabicTextStyle(ReportTheme.Typography.Body, true, ReportTheme.Colors.Text));
                    }
                });

                // Progress bar
                col.Item()
                    .PaddingTop(HiFiSettings.Spacing.XS)
                    .Height(10)
                    .Background(ReportTheme.Colors.Track)
                    .Row(row =>
                    {
                        row.RelativeItem((float)percentage).Background(barColor);
                        row.RelativeItem((float)(100 - percentage));
                    });
            });
        }

        /// <summary>
        /// Legend item for charts
        /// </summary>
        public static void LegendItem(this IContainer container, string color, string label)
        {
            container.Row(row =>
            {
                // Color indicator
                row.AutoItem()
                    .Width(16)
                    .Height(16)
                    .Background(color);
                
                // Label
                row.RelativeItem()
                    .PaddingLeft(HiFiSettings.Spacing.SM)
                    .Text(UnicodeTextHelper.NormalizeNfc(label))
                    .Style(ReportTheme.ArabicTextStyle(ReportTheme.Typography.BodySmall, false, ReportTheme.Colors.Text));
            });
        }

        /// <summary>
        /// Pattern card for displaying individual pattern analysis
        /// </summary>
        public static void PatternCard(this IContainer container, string title, double tScore, 
            string description, string? recommendation = null)
        {
            var bandColor = ReportTheme.GetBandColor(tScore);
            var bandLabel = ReportTheme.GetPerformanceLabel(tScore);

            container
                .Background(ReportTheme.Colors.Surface)
                .Border(1)
                .BorderColor(ReportTheme.Colors.Border)
                .Padding(HiFiSettings.Spacing.LG)
                .Column(col =>
                {
                    // Header with title and badge
                    col.Item().Row(row =>
                    {
                        row.RelativeItem()
                            .Text(UnicodeTextHelper.NormalizeNfc(title))
                            .Style(ReportTheme.ArabicTextStyle(ReportTheme.Typography.H3, true, ReportTheme.Colors.Text));
                        
                        row.AutoItem()
                            .BadgeWithIcon(
                                $"{bandLabel} ({UnicodeTextHelper.FormatTScore(tScore, 1)})",
                                bandColor,
                                textColor: "#FFFFFF"
                            );
                    });

                    // Description
                    col.Item()
                        .PaddingTop(HiFiSettings.Spacing.MD)
                        .Text(UnicodeTextHelper.NormalizeNfc(description))
                        .Style(ReportTheme.ArabicTextStyle(ReportTheme.Typography.Body, false, ReportTheme.Colors.Text))
                        .LineHeight(1.6f);

                    // Recommendation (if provided)
                    if (!string.IsNullOrEmpty(recommendation))
                    {
                        col.Item()
                            .PaddingTop(HiFiSettings.Spacing.MD)
                            .Background(ReportTheme.Colors.SurfaceHover)
                            .Padding(HiFiSettings.Spacing.MD)
                            .Row(row =>
                            {
                                row.AutoItem()
                                    .PaddingLeft(HiFiSettings.Spacing.SM)
                                    .Text("💡")
                                    .FontSize(ReportTheme.Typography.Icon);
                                
                                row.RelativeItem()
                                    .Text(UnicodeTextHelper.NormalizeNfc(recommendation))
                                    .Style(ReportTheme.ArabicTextStyle(ReportTheme.Typography.BodySmall, false, 
                                        ReportTheme.Colors.TextSecondary))
                                    .LineHeight(1.5f);
                            });
                    }
                });
        }
    }
}
