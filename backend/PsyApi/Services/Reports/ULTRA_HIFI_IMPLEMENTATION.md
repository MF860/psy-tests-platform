# 🎨 Ultra Hi-Fi Report Design System - Implementation Guide

## Overview

This implementation provides a **premium, board-level quality** psychometric report design system with two sophisticated themes:

1. **Aurora Glass** (Light) - Glassmorphism with luxury gradients
2. **Noir Executive** (Dark) - Neumorphic design with high contrast

## 🎯 Key Features

### Design System
- ✅ **Tokenized design** - All colors, typography, spacing are centralized
- ✅ **Theme switching** - Toggle between Aurora Glass and Noir Executive
- ✅ **Reusable components** - Header, Footer, KPI Cards, Badges, Tables, etc.
- ✅ **Perfect Arabic/English** - Full RTL/LTR support with proper shaping
- ✅ **Print-ready quality** - 300 DPI, embedded fonts, PDF/X-4 compatible

### Typography Scale
```
H1: 26pt (Major titles)
H2: 20pt (Section headers)
H3: 16pt (Subsection headers)
Title: 14pt Semibold
Body: 12pt (Perfect readability)
Small: 10pt (Captions, fine print)
KPI: 36pt (Large numbers)
```

### Color Tokens
```csharp
// Primary Brand
Primary: #0B5ED7 (Professional Blue)
Secondary: #4F46E5 (Indigo)

// Semantic Status
Success: #16A34A (Green ≥65)
Warning: #F59E0B (Amber 40-64.9)
Danger: #DC2626 (Red <40)
Info: #0EA5E9 (Sky Blue)

// Theme-aware (auto-switch)
Background: #FFFFFF (Aurora) / #0F172A (Noir)
Surface: #F9FAFB (Aurora) / #1E293B (Noir)
Text: #111827 (Aurora) / #F3F4F6 (Noir)
```

### Spacing System (8pt Grid)
```
XS:  4pt  (Micro spacing)
SM:  8pt  (Small spacing)
MD:  12pt (Medium spacing)
LG:  16pt (Large spacing)
XL:  24pt (Extra large)
XXL: 32pt (Double extra large)
```

## 🚀 Quick Start

### 1. Switch Theme

```csharp
using PsyApi.Services.Reports;

// Set Aurora Glass (Light)
DesignTokens.CurrentTheme = DesignTokens.ReportThemeMode.AuroraGlass;

// Set Noir Executive (Dark)
DesignTokens.CurrentTheme = DesignTokens.ReportThemeMode.NoirExecutive;
```

### 2. Use Design Components

```csharp
// Header with logo
page.Header().Element(header => 
    DesignComponents.Header(
        header, 
        title: "تقرير تحليل أنماط الشخصية السبعة",
        subtitle: "التحليل التفصيلي للأنماط والأبعاد الفرعية",
        metadata: new Dictionary<string, string> {
            {"الاسم", user.FullName},
            {"التاريخ", DateTime.Now.ToString("yyyy-MM-dd")}
        },
        logoBytes: _logoBytes
    )
);

// KPI Cards
column.Item().Row(row => {
    row.RelativeItem().Element(c => 
        DesignComponents.KpiCard(
            c,
            value: "73.5",
            label: "متوسط T-Score",
            trend: "↑ 5.2%",
            color: DesignTokens.Colors.Success
        )
    );
});

// Section with icon
column.Item().Element(c => 
    DesignComponents.Section(c, "📊 التحليل البصري", icon: "📊")
);

// Callout (info/success/warning/danger)
column.Item().Element(c => 
    DesignComponents.Callout(
        c,
        message: "تم تحديد 3 أبعاد تحتاج إلى تطوير عاجل",
        variant: "warning"
    )
);

// Badge
row.AutoItem().Element(c => 
    DesignComponents.Badge(c, text: "ممتاز", level: "excellent")
);

// Glass Card (Aurora Glass theme)
column.Item().Element(c => 
    DesignComponents.GlassCard(c, card => {
        card.Column(col => {
            col.Item().Text("محتوى البطاقة");
        });
    })
);

// Progress Bar
column.Item().Element(c => 
    DesignComponents.ProgressBar(
        c,
        label: "التواصل والعلاقات",
        percentage: 78.5,
        color: DesignTokens.Colors.Success
    )
);
```

### 3. Use Design Tokens

```csharp
// Colors
container.Background(DesignTokens.Colors.Surface);
container.BorderColor(DesignTokens.Colors.Border);

// Typography
text.Style(
    TextStyle.Default
        .FontFamily(DesignTokens.Typography.FontArabic)
        .FontSize(DesignTokens.Typography.H2)
        .FontColor(DesignTokens.Colors.Text)
        .DirectionFromRightToLeft()
);

// Spacing
column.Item().PaddingTop(DesignTokens.Spacing.LG);
container.Padding(DesignTokens.Spacing.CardPadding);

// Border Radius
container.BorderRadius(DesignTokens.Radius.XL);

// Number Formatting
var tScoreFormatted = DesignTokens.Formatting.FormatTScore(73.5);  // "73.5"
var percentFormatted = DesignTokens.Formatting.FormatPercent(85);  // "85%"
```

## 📐 Page Architecture (6-8 Pages)

### Page 1: Cover & Executive Summary
- Premium header with logo (60-72px)
- User metadata grid (2×2 layout)
- 4 KPI cards (Avg T, Advanced Patterns, Variance, Balance Index)
- Top 3 strengths + Bottom 3 growth areas

### Page 2: Visual Analytics
- 7-axis radar chart (180px effective radius, vector quality)
- Horizontal bar chart (520-600px, RTL labels)
- Cluster donut chart (150-170px diameter)
- Statistical summary band

### Page 3: Data Story
- General insights (bullet points)
- Cluster analysis (with top 3 dimensions per cluster)
- Risk flags (max 5 alerts)

### Page 4: Seven Patterns Overview
- 7 pattern cards with:
  - Color-coded band badge
  - T-Score + Percentile
  - 2-3 line description
  - Micro-recommendation

### Page 5: Development Plan
- Action items table (Priority, Title, Description, Timeline, KPI)
- Tips for success box

### Page 6 (Optional): Training Courses
- Recommended courses (max 6)
- Target dimensions chips
- Duration and description

## 🎨 Theme Comparison

| Feature | Aurora Glass | Noir Executive |
|---------|-------------|----------------|
| Background | White (#FFFFFF) | Deep Navy (#0F172A) |
| Surface | Light Gray (#F9FAFB) | Slate 800 (#1E293B) |
| Text | Near Black (#111827) | Off White (#F3F4F6) |
| Components | Glassmorphism cards | Neumorphic panels |
| Dividers | Hairline, gradient bars | Thin accent lines |
| Charts | Translucent polygons | High-contrast colors |
| Best For | Presentations, digital | Print, executive meetings |

## 🔧 Customization

### Add New Color Token
```csharp
// In DesignTokens.Colors class
public static string CustomColor => CurrentTheme == ReportThemeMode.AuroraGlass 
    ? "#YOUR_LIGHT_COLOR" 
    : "#YOUR_DARK_COLOR";
```

### Add New Component
```csharp
// In DesignComponents class
public static void CustomComponent(IContainer container, string param1, string? param2 = null)
{
    container.Column(col => {
        // Your component implementation
        col.Item().Text(param1)
            .Style(GetArabicTextStyle(
                DesignTokens.Typography.Body, 
                false, 
                DesignTokens.Colors.Text
            ));
    });
}
```

### Adjust Performance Bands
```csharp
// In DesignTokens.PerformanceBands class
public const double ExcellentMin = 70.0;  // Change from 65.0
public const double GoodMin = 60.0;       // Change from 55.0
```

## 📊 Chart Integration

### Vector-Quality Charts (300-450 DPI)

```csharp
// Radar Chart
var radarBytes = RadarChartRenderer.RenderRadarChart(
    dimensions: sortedDimensions.Take(12),
    size: 400,  // 200px radius effective
    showGrid: true,
    dpi: 450    // Ultra high quality
);

// Horizontal Bars
var barBytes = HorizontalBarChartRenderer.RenderHorizontalBars(
    dimensions: sortedDimensions,
    width: 580,  // 520-600px as per specs
    maxDimensions: 12,
    dpi: 450
);

// Donut Chart
var donutBytes = DonutChartRenderer.RenderClusterDonut(
    clusterDistribution: clusterData,
    size: 160,  // 150-170px diameter
    centerText: "المحاور",
    dpi: 450
);
```

## 🌐 Bilingual Support (Arabic/English)

### Arabic (RTL)
```csharp
// Set language
LocalizationStrings.CurrentLanguage = ReportLanguage.AR;

// Use localized strings
var title = LocalizationStrings.Get("report.title");  // Arabic

// Arabic text style
text.Style(
    TextStyle.Default
        .FontFamily(DesignTokens.Typography.FontArabic)
        .DirectionFromRightToLeft()
);
```

### English (LTR)
```csharp
// Set language
LocalizationStrings.CurrentLanguage = ReportLanguage.EN;

// Use localized strings
var title = LocalizationStrings.Get("report.title");  // English

// English text style
text.Style(DesignComponents.GetEnglishTextStyle(
    DesignTokens.Typography.H1,
    bold: true,
    color: DesignTokens.Colors.Text
));
```

## 🖨️ Export Configuration

### PDF Settings (Print-Ready)
```csharp
QuestPDF.Settings.License = LicenseType.Community;
QuestPDF.Settings.EnableDebugging = false;

var pdf = Document.Create(document => {
    // Configure pages...
});

var pdfBytes = pdf.GeneratePdf(new PdfGenerationSettings {
    ImageQuality = 100,              // Maximum quality
    RasterDpi = 300,                 // Print quality DPI
    GenerateDocumentOutline = true,  // PDF bookmarks
    ApplyFlateCompression = false    // No aggressive compression
});

// For PDF/X-4 compatibility:
// - Embed all fonts ✓ (handled by QuestPDF)
// - 24-bit color ✓
// - Optional 3mm bleed (add to page margins)
```

## 📦 Dependencies

```xml
<!-- Required NuGet packages -->
<PackageReference Include="QuestPDF" Version="2024.x.x" />
<PackageReference Include="SkiaSharp" Version="2.88.x" />
```

## 📝 Example: Complete Report Generation

```csharp
using PsyApi.Services.Reports;

public class UltraHiFiReportExample
{
    public byte[] GenerateReport(
        Result result, 
        User user, 
        List<DimensionScoreDto> dimensions)
    {
        // Set theme
        DesignTokens.CurrentTheme = DesignTokens.ReportThemeMode.AuroraGlass;
        
        // Configure QuestPDF
        QuestPDF.Settings.License = LicenseType.Community;
        
        var pdf = Document.Create(doc =>
        {
            // Page 1: Cover
            doc.Page(page =>
            {
                ConfigurePage(page);
                
                page.Header().Element(h => 
                    DesignComponents.Header(
                        h, 
                        "منصة التحليل النفسي المتقدم",
                        "تقرير شامل ومفصل",
                        new Dictionary<string, string> {
                            {"الاسم", user.FullName},
                            {"التاريخ", DateTime.Now.ToString("yyyy-MM-dd")}
                        },
                        _logoBytes
                    )
                );
                
                page.Content().Column(col =>
                {
                    // KPI Cards
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Element(c => 
                            DesignComponents.KpiCard(
                                c,
                                DesignTokens.Formatting.FormatTScore(avgTScore),
                                "متوسط T-Score",
                                color: DesignTokens.PerformanceBands.GetBandColor(avgTScore)
                            )
                        );
                        // Add more KPI cards...
                    });
                    
                    // Add more content...
                });
                
                page.Footer().Element(f => 
                    DesignComponents.Footer(f, 1, 6, "© 2025 منصة التحليل النفسي")
                );
            });
            
            // Add more pages...
        });
        
        return pdf.GeneratePdf();
    }
    
    private void ConfigurePage(PageDescriptor page)
    {
        page.Size(PageSizes.A4);
        page.Margin(
            DesignTokens.Spacing.PageMarginTop,
            DesignTokens.Spacing.PageMarginRight,
            DesignTokens.Spacing.PageMarginBottom,
            DesignTokens.Spacing.PageMarginLeft
        );
        page.DefaultTextStyle(style => 
            TextStyle.Default
                .FontFamily(DesignTokens.Typography.FontArabic)
                .FontSize(DesignTokens.Typography.Body)
                .FontColor(DesignTokens.Colors.Text)
        );
    }
}
```

## 🎯 Best Practices

1. **Always use design tokens** - Never hardcode colors, sizes, or spacing
2. **Leverage components** - Reuse Header, Footer, KPI Cards, etc.
3. **Test both themes** - Ensure content is readable in Aurora Glass AND Noir Executive
4. **Embed fonts** - Critical for Arabic rendering (Noto Naskh Arabic)
5. **Vector charts** - Use 300-450 DPI for print quality
6. **RTL/LTR awareness** - Test with both Arabic and English content
7. **Consistent spacing** - Use 8pt grid system religiously
8. **WCAG AA contrast** - All text meets minimum contrast ratios

## 🐛 Troubleshooting

### Issue: Arabic text shows � glyphs
**Solution:** Use `DesignTokens.Formatting.FormatNumber()` for all numeric values

### Issue: Theme not switching
**Solution:** Set `DesignTokens.CurrentTheme` BEFORE creating the document

### Issue: Charts are pixelated
**Solution:** Increase DPI parameter in chart renderers (300-450)

### Issue: Fonts not rendering
**Solution:** Ensure Noto Naskh Arabic fonts are in `Resources/Fonts/` directory

## 📚 Additional Resources

- **QuestPDF Documentation:** https://www.questpdf.com/
- **SkiaSharp Documentation:** https://docs.microsoft.com/en-us/xamarin/xamarin-forms/user-interface/graphics/skiasharp/
- **Arabic Typography Guide:** See `ArabicTextRenderer.cs`
- **Chart Rendering:** See `*ChartRenderer.cs` files

## 📄 Output Files

Generate both themes for client review:

```csharp
// Aurora Glass (Light)
DesignTokens.CurrentTheme = DesignTokens.ReportThemeMode.AuroraGlass;
var auroraAr = GenerateReport(result, user, dimensions);
File.WriteAllBytes("Report_AR_AuroraGlass.pdf", auroraAr);

var auroraEn = GenerateReport(result, user, dimensions, language: ReportLanguage.EN);
File.WriteAllBytes("Report_EN_AuroraGlass.pdf", auroraEn);

// Noir Executive (Dark)
DesignTokens.CurrentTheme = DesignTokens.ReportThemeMode.NoirExecutive;
var noirAr = GenerateReport(result, user, dimensions);
File.WriteAllBytes("Report_AR_NoirExecutive.pdf", noirAr);

var noirEn = GenerateReport(result, user, dimensions, language: ReportLanguage.EN);
File.WriteAllBytes("Report_EN_NoirExecutive.pdf", noirEn);
```

Expected outputs:
- ✅ `Report_AR_AuroraGlass.pdf` (Light theme, Arabic)
- ✅ `Report_EN_AuroraGlass.pdf` (Light theme, English)
- ✅ `Report_AR_NoirExecutive.pdf` (Dark theme, Arabic)
- ✅ `Report_EN_NoirExecutive.pdf` (Dark theme, English)
- ✅ `StyleSheet_Tokens_Components.pdf` (Design system documentation)

## 🎉 What's Next?

1. **Implement UltraHiFiPdfReportService.cs** - Main service using this design system
2. **Enhance chart renderers** - Add 450 DPI support, halos, annotations
3. **Complete English translations** - Full LocalizationStrings.cs
4. **Generate style sheet PDF** - Visual documentation of all tokens and components
5. **Add PNG previews** - 2× resolution for quick review

---

**Version:** 4.0 Ultra Hi-Fi  
**Last Updated:** November 2025  
**License:** MIT  
**Author:** Psy-Tests Platform Team
