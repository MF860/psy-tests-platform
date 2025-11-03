using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Helpers;

namespace PsyApi.Services.Reports
{
    /// <summary>
    /// Reusable Design Components for Ultra Hi-Fi Reports
    /// All components support both Aurora Glass and Noir Executive themes
    /// Build once, reuse everywhere principle
    /// </summary>
    public static class DesignComponents
    {
        /// <summary>
        /// Render Header component with logo, title, subtitle, and metadata
        /// Usage: column.Item().Component(c => DesignComponents.Header(c, "Title", "Subtitle", metadata));
        /// </summary>
        public static void Header(IContainer container, string title, string? subtitle = null, Dictionary<string, string>? metadata = null, byte[]? logoBytes = null)
        {
            container.Column(column =>
            {
                // Logo (if provided) - 60-72px as per specs
                if (logoBytes != null && logoBytes.Length > 0)
                {
                    column.Item().AlignCenter().Width(66).Image(logoBytes);
                    column.Item().PaddingTop(DesignTokens.Spacing.MD);
                }

                // Title (H1 26pt Bold)
                column.Item().AlignCenter()
                    .Text(title)
                    .Style(GetArabicTextStyle(DesignTokens.Typography.H1, true, DesignTokens.Colors.Text));

                // Subtitle (if provided)
                if (!string.IsNullOrEmpty(subtitle))
                {
                    column.Item().PaddingTop(DesignTokens.Spacing.XS)
                        .AlignCenter()
                        .Text(subtitle)
                        .Style(GetArabicTextStyle(DesignTokens.Typography.Body, false, DesignTokens.Colors.TextSecondary));
                }

                // Metadata (if provided) - displayed as key-value grid
                if (metadata != null && metadata.Any())
                {
                    column.Item().PaddingTop(DesignTokens.Spacing.LG);
                    column.Item().Element(c => MetadataGrid(c, metadata));
                }
            });
        }

        /// <summary>
        /// Render Footer component with page number and watermark
        /// </summary>
        public static void Footer(IContainer container, int pageNumber, int totalPages = 0, string? watermark = null)
        {
            container.Column(footer =>
            {
                // Watermark or copyright
                if (!string.IsNullOrEmpty(watermark))
                {
                    footer.Item().PaddingBottom(DesignTokens.Spacing.SM)
                        .AlignCenter()
                        .Text(watermark)
                        .Style(GetArabicTextStyle(DesignTokens.Typography.Small, false, DesignTokens.Colors.TextMuted));
                }

                // Page number
                var pageText = totalPages > 0 
                    ? $"صفحة {pageNumber} من {totalPages}" 
                    : $"صفحة {pageNumber}";
                
                footer.Item().AlignCenter()
                    .Text(pageText)
                    .Style(GetArabicTextStyle(DesignTokens.Typography.Caption, false, DesignTokens.Colors.TextSecondary));
            });
        }

        /// <summary>
        /// Render Section component with title and optional icon
        /// </summary>
        public static void Section(IContainer container, string title, string? icon = null)
        {
            container.Column(section =>
            {
                section.Item().Row(row =>
                {
                    // Icon (if provided)
                    if (!string.IsNullOrEmpty(icon))
                    {
                        row.ConstantItem(24).Text(icon)
                            .FontSize(DesignTokens.Typography.H3)
                            .FontColor(DesignTokens.Colors.Primary);
                    }

                    // Title
                    row.RelativeItem()
                        .PaddingLeft(icon != null ? DesignTokens.Spacing.SM : 0)
                        .AlignMiddle()
                        .Text(title)
                        .Style(GetArabicTextStyle(DesignTokens.Typography.H2, true, DesignTokens.Colors.Text));
                });

                // Subtle divider line
                section.Item().PaddingTop(DesignTokens.Spacing.SM)
                    .Height(2)
                    .Background(DesignTokens.Colors.Divider);
            });
        }

        /// <summary>
        /// Render KPI Card component with value, label, and optional trend
        /// </summary>
        public static void KpiCard(IContainer container, string value, string label, string? trend = null, string? color = null)
        {
            var bgColor = color ?? DesignTokens.Colors.Primary;
            
            container
                .Border(2).BorderColor(bgColor)
                .Background(DesignTokens.Colors.SurfaceElevated)
                .Padding(DesignTokens.Spacing.MD)
                .Column(card =>
                {
                    // Value (large, bold)
                    card.Item().AlignCenter()
                        .Text(value)
                        .FontSize(DesignTokens.Typography.KPI)
                        .FontColor(bgColor)
                        .Bold();

                    // Label
                    card.Item().PaddingTop(DesignTokens.Spacing.XS)
                        .AlignCenter()
                        .Text(label)
                        .Style(GetArabicTextStyle(DesignTokens.Typography.KPILabel, false, DesignTokens.Colors.TextSecondary));

                    // Trend (if provided)
                    if (!string.IsNullOrEmpty(trend))
                    {
                        card.Item().PaddingTop(DesignTokens.Spacing.XS)
                            .AlignCenter()
                            .Text(trend)
                            .FontSize(DesignTokens.Typography.Small)
                            .FontColor(DesignTokens.Colors.Success);
                    }
                });
        }

        /// <summary>
        /// Render Badge component with text and level-based coloring
        /// </summary>
        public static void Badge(IContainer container, string text, string level = "default")
        {
            var (bgColor, textColor) = level.ToLower() switch
            {
                "excellent" => (DesignTokens.Colors.Success, "#FFFFFF"),
                "good" => (DesignTokens.Colors.Info, "#FFFFFF"),
                "average" => (DesignTokens.Colors.Warning, "#FFFFFF"),
                "weak" => (DesignTokens.Colors.Danger, "#FFFFFF"),
                "primary" => (DesignTokens.Colors.Primary, "#FFFFFF"),
                _ => (DesignTokens.Colors.Surface, DesignTokens.Colors.Text)
            };

            container
                .Background(bgColor)
                .PaddingHorizontal(DesignTokens.Spacing.SM)
                .PaddingVertical(DesignTokens.Spacing.XS)
                .AlignCenter()
                .Text(text)
                .Style(GetArabicTextStyle(DesignTokens.Typography.Badge, true, textColor));
        }

        /// <summary>
        /// Render Legend Item for charts
        /// </summary>
        public static void LegendItem(IContainer container, string color, string label)
        {
            container.Row(row =>
            {
                // Color indicator (circle or square)
                row.ConstantItem(16).Height(16)
                    .Background(color)
                    .Border(1).BorderColor(DesignTokens.Colors.Border);

                // Label
                row.RelativeItem()
                    .PaddingLeft(DesignTokens.Spacing.SM)
                    .AlignMiddle()
                    .Text(label)
                    .Style(GetArabicTextStyle(DesignTokens.Typography.Small, false, DesignTokens.Colors.Text));
            });
        }

        /// <summary>
        /// Render Callout component (info/success/warn/danger variants)
        /// </summary>
        public static void Callout(IContainer container, string message, string variant = "info")
        {
            var (bgColor, borderColor, textColor, icon) = variant.ToLower() switch
            {
                "success" => ("#ECFDF5", DesignTokens.Colors.Success, "#065F46", "✓"),
                "warning" => ("#FEF3C7", DesignTokens.Colors.Warning, "#92400E", "⚠"),
                "danger" => ("#FEE2E2", DesignTokens.Colors.Danger, "#991B1B", "✕"),
                _ => ("#EFF6FF", DesignTokens.Colors.Info, "#1E40AF", "ℹ")
            };

            container
                .Background(bgColor)
                .Border(1).BorderColor(borderColor)
                .Padding(DesignTokens.Spacing.MD)
                .Row(row =>
                {
                    // Icon
                    row.ConstantItem(24).Text(icon)
                        .FontSize(DesignTokens.Typography.H4)
                        .FontColor(borderColor)
                        .Bold();

                    // Message
                    row.RelativeItem()
                        .PaddingLeft(DesignTokens.Spacing.SM)
                        .AlignMiddle()
                        .Text(message)
                        .Style(GetArabicTextStyle(DesignTokens.Typography.Body, false, textColor));
                });
        }

        /// <summary>
        /// Render Stat Row (label + value pair)
        /// </summary>
        public static void StatRow(IContainer container, string label, string value)
        {
            container.Row(row =>
            {
                // Label (right-aligned for RTL)
                row.RelativeItem()
                    .AlignRight()
                    .Text(label)
                    .Style(GetArabicTextStyle(DesignTokens.Typography.Body, false, DesignTokens.Colors.TextSecondary));

                // Value (left-aligned, bold)
                row.RelativeItem()
                    .AlignLeft()
                    .Text(value)
                    .Style(GetArabicTextStyle(DesignTokens.Typography.Body, true, DesignTokens.Colors.Text));
            });
        }

        /// <summary>
        /// Render Glass Card (Aurora Glass theme) with frosted blur effect
        /// Note: PDF doesn't support true blur, so we simulate with translucent overlay
        /// </summary>
        public static void GlassCard(IContainer container, Action<IContainer> content)
        {
            container
                .Background(DesignTokens.Colors.SurfaceElevated)
                .Border(1).BorderColor(DesignTokens.Colors.BorderSubtle)
                .Padding(DesignTokens.Spacing.CardPadding)
                .Element(content);
        }

        /// <summary>
        /// Render Neumorphic Card (Noir Executive theme) with subtle embossed effect
        /// </summary>
        public static void NeumorphicCard(IContainer container, Action<IContainer> content)
        {
            container
                .Background(DesignTokens.Colors.Surface)
                .Border(1).BorderColor(DesignTokens.Colors.Divider)
                .Padding(DesignTokens.Spacing.CardPadding)
                .Element(content);
        }

        /// <summary>
        /// Render Metadata Grid (2-column key-value layout)
        /// </summary>
        public static void MetadataGrid(IContainer container, Dictionary<string, string> metadata)
        {
            container
                .Background(DesignTokens.Colors.Surface)
                .Padding(DesignTokens.Spacing.MD)
                .Column(grid =>
                {
                    var items = metadata.ToList();
                    for (int i = 0; i < items.Count; i += 2)
                    {
                        grid.Item().Row(row =>
                        {
                            // Left column
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Element(c => StatRow(c, items[i].Key, items[i].Value));
                            });

                            // Right column (if exists)
                            if (i + 1 < items.Count)
                            {
                                row.RelativeItem().PaddingLeft(DesignTokens.Spacing.LG).Column(col =>
                                {
                                    col.Item().Element(c => StatRow(c, items[i + 1].Key, items[i + 1].Value));
                                });
                            }
                        });

                        if (i + 2 < items.Count)
                        {
                            grid.Item().PaddingTop(DesignTokens.Spacing.SM);
                        }
                    }
                });
        }

        /// <summary>
        /// Render RTL Table with zebra rows and proper alignment
        /// </summary>
        public static void RtlTable(IContainer container, List<string> headers, List<List<string>> rows)
        {
            container.Table(table =>
            {
                // Define columns (equal width)
                table.ColumnsDefinition(cols =>
                {
                    for (int i = 0; i < headers.Count; i++)
                    {
                        cols.RelativeColumn();
                    }
                });

                // Header row
                table.Header(header =>
                {
                    foreach (var headerText in headers)
                    {
                        header.Cell()
                            .Background(DesignTokens.Colors.Primary)
                            .Padding(DesignTokens.Spacing.SM)
                            .AlignRight()
                            .Text(headerText)
                            .Style(GetArabicTextStyle(DesignTokens.Typography.Body, true, "#FFFFFF"));
                    }
                });

                // Data rows with zebra striping
                for (int i = 0; i < rows.Count; i++)
                {
                    var row = rows[i];
                    var bgColor = i % 2 == 0 ? "#FFFFFF" : DesignTokens.Colors.Surface;

                    foreach (var cell in row)
                    {
                        table.Cell()
                            .Background(bgColor)
                            .Padding(DesignTokens.Spacing.SM)
                            .Border(0.5f).BorderColor(DesignTokens.Colors.Border)
                            .AlignRight()
                            .Text(cell)
                            .Style(GetArabicTextStyle(DesignTokens.Typography.Body, false, DesignTokens.Colors.Text));
                    }
                }
            });
        }

        /// <summary>
        /// Render Progress Bar with label and percentage
        /// </summary>
        public static void ProgressBar(IContainer container, string label, double percentage, string? color = null)
        {
            var barColor = color ?? DesignTokens.Colors.Primary;
            var normalizedPercentage = Math.Max(0, Math.Min(100, percentage));

            container.Column(progress =>
            {
                // Label and percentage
                progress.Item().Row(row =>
                {
                    row.RelativeItem()
                        .AlignRight()
                        .Text(label)
                        .Style(GetArabicTextStyle(DesignTokens.Typography.Body, false, DesignTokens.Colors.Text));

                    row.ConstantItem(50)
                        .AlignLeft()
                        .Text(DesignTokens.Formatting.FormatPercent(normalizedPercentage))
                        .FontSize(DesignTokens.Typography.Body)
                        .FontColor(DesignTokens.Colors.TextSecondary);
                });

                // Progress track
                progress.Item().PaddingTop(DesignTokens.Spacing.XS)
                    .Height(8)
                    .Background(DesignTokens.Colors.Surface)
                    .Border(1).BorderColor(DesignTokens.Colors.Border)
                    .Row(track =>
                    {
                        // Filled portion
                        track.RelativeItem((float)normalizedPercentage)
                            .Background(barColor)
                            .Height(8);

                        // Empty portion
                        track.RelativeItem((float)(100 - normalizedPercentage));
                    });
            });
        }

        /// <summary>
        /// Helper: Get Arabic text style with RTL direction
        /// </summary>
        private static TextStyle GetArabicTextStyle(float size, bool bold = false, string? color = null)
        {
            var style = TextStyle.Default
                .FontFamily(DesignTokens.Typography.FontArabic)
                .FontSize(size)
                .FontColor(color ?? DesignTokens.Colors.Text)
                .DirectionFromRightToLeft();

            if (bold)
                style = style.Bold();

            return style;
        }

        /// <summary>
        /// Helper: Get English text style with LTR direction
        /// </summary>
        public static TextStyle GetEnglishTextStyle(float size, bool bold = false, string? color = null)
        {
            var style = TextStyle.Default
                .FontFamily(DesignTokens.Typography.FontEnglish)
                .FontSize(size)
                .FontColor(color ?? DesignTokens.Colors.Text);

            if (bold)
                style = style.Bold();

            return style;
        }
    }
}
