using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System;

namespace PsyApi.Services.Reports
{
    /// <summary>
    /// UI Component Helpers for modern PDF reports v2.0
    /// Simplified helpers without unsupported QuestPDF features
    /// </summary>
    public static class ReportThemeHelpers
    {
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
    }
}
