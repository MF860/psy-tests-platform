# SDJ PDF Implementation Plan - Phase F Complete Specification

**Target**: 5-page Arabic SDJ PDF report with QuestPDF + SkiaSharp  
**Effort**: 12-15 hours  
**Priority**: HIGH (production blocker)

---

## Current State

### Existing PDF Service
**File**: `backend/PsyApi/Services/Reports/ModernPdfReportService.cs` (617 lines)

**Current Structure** (3 pages):
- Page 1: Cover & KPIs (user info, average T-score, top 3 strengths/weaknesses)
- Page 2: Dimension donuts grid (180px, vector-based, HarfBuzz enabled)
- Page 3: Actions & recommended courses

**Technologies**:
- QuestPDF: Layout engine (Community License)
- SkiaSharp: Chart rendering
- HarfBuzz: Arabic text shaping (ENABLED ✅)
- Fonts: Amiri (registered ✅)

---

## SDJ Requirements (5 Pages)

### Page 1: Cover & Summary (SDJ)
**Changes from current**:
- Add centered logo: `SITES-ICON.png` (150x150px)
- Title: "منصة التحليل النفسي المتقدم – إطار التنمية المستدامة"
- Keep: User info 2x2 grid, KPI chips
- Update: Top 3 strengths from SDJ sub-dimensions (not legacy dimensions)
- Update: Top 3 development areas from SDJ sub-dimensions

### Page 2: Charts & Visualizations (SDJ)
**NEW: Horizontal Bar Chart** (major addition):
- **Renderer**: SkiaSharp canvas drawing (not Recharts)
- **Data**: All 24 SDJ sub-dimensions sorted ascending by T-score
- **Layout**: Horizontal bars (category axis vertical, value axis horizontal)
- **Dimensions**:
  - Canvas: 500x (24 * 30) = 720px height
  - Bar height: 20px
  - Gap: 10px
  - Label width: 150px (Arabic sub-dimension name)
  - Bar max width: 300px (domain 0-80)
- **Styling**:
  - Band colors: Red (T<40), Amber (40-55), Green (≥55)
  - T-score label at end of bar (white text inside bar if space, black outside)
  - Axis: 0, 20, 40, 60, 80 markers
  - Arabic dimension names (12pt, right-aligned)

**Updated: Vector Donuts** (keep existing implementation):
- Grid: 2 rows × 3 cols = 6 donuts
- Size: 180px (already implemented ✅)
- Show: Top 3 strongest + Top 3 weakest sub-dimensions (not parent dimensions)
- Band colors as usual

### Page 3: SDJ Dimension Tree (NEW)
**Accordion-style hierarchy**:
- **Parent Dimensions** (5 total):
  - Font: 16pt bold, color: ReportTheme.Colors.Primary
  - Show T-score and band badge next to name
  - Separator line below

- **Sub-Dimensions** (24 total, 5 per parent):
  - Indented 20px
  - Font: 13pt regular
  - Show: Name (Arabic), T-score, Band badge
  - Row format: `→ {SubDimension}   T={T}   [{Band}]`
  - Band badge: Colored circle (10px) + text

**Example**:
```
█ التميز الذاتي   T=62.5   [ممتاز]
  → الوعي الذاتي   T=58.2   [ممتاز]
  → الثقة بالنفس   T=65.1   [ممتاز]
  → التنظيم الذاتي   T=60.8   [ممتاز]
  → التعلم المستمر   T=64.3   [ممتاز]
  → المرونة النفسية   T=63.7   [ممتاز]
```

**SDJ Track Analysis** (bottom of page):
- **3 Track Cards** (one per track):
  - Track name: Arabic + English
  - Fit level badge: high/medium/low with icon
  - Fit score: T-score (large, 18pt)
  - Reasoning: 2-3 sentence Arabic text
  - Key competencies: Bullet list (3-4 items)

### Page 4: Action Plan (NEW)
**Weakness-driven recommendations**:
- **Header**: "خطة التطوير الموجهة"
- **Identify weak sub-dimensions**: Filter SubDimensions where T < 40
- **For each weak sub-dimension**:
  - Sub-heading: Sub-dimension name + T-score
  - **2-3 Recommendations** from hardcoded template dictionary
  - Format: `→ {recommendation_text}`
  - Spacing: 12pt between recommendations

**Template Dictionary** (hardcoded, no AI):
```csharp
private static readonly Dictionary<string, List<string>> ActionPlanTemplates = new()
{
    ["الوعي الذاتي"] = new List<string>
    {
        "ممارسة التأمل الذاتي اليومي لمدة 10 دقائق لتعزيز الوعي بالأفكار والمشاعر",
        "كتابة يومية تحليلية تُركز على نقاط القوة والضعف الشخصية",
        "طلب تغذية راجعة منتظمة من الزملاء والمشرفين لفهم التأثير الخارجي"
    },
    ["الثقة بالنفس"] = new List<string>
    {
        "وضع أهداف صغيرة قابلة للتحقيق وتوثيق النجاحات المحققة",
        "ممارسة التحدث أمام مجموعات صغيرة بشكل تدريجي",
        "تحدي الأفكار السلبية عن الذات باستخدام تقنيات إعادة الهيكلة المعرفية"
    },
    // ... 22 more sub-dimensions
};
```

**Fallback**: If no weak dimensions (all T ≥ 40), show:
> "لا توجد مجالات ضعيفة تحتاج إلى تطوير. جميع الأبعاد في النطاق المتوسط أو الممتاز. يُنصح بالتركيز على تعزيز نقاط القوة الحالية."

### Page 5: Methodology (NEW)
**Sections**:

1. **SDJ Framework Overview**
   - "إطار التنمية المستدامة (SDJ): نموذج قياس نفسي متقدم"
   - 5 parent dimensions, 24 sub-dimensions, 120 Likert items
   - Paragraph: Purpose, scientific basis, competency focus

2. **Likert Scale Explanation**
   - Table: Value (1-5), Arabic label, Interpretation
   - Example:
     ```
     1 - لا أوافق بشدة - رفض تام للعبارة
     2 - لا أوافق - ميل إلى عدم الموافقة
     3 - محايد - موقف محايد أو غير متأكد
     4 - أوافق - ميل إلى الموافقة
     5 - أوافق بشدة - موافقة تامة على العبارة
     ```

3. **Reverse Scoring**
   - Explanation: `score = 6 - raw_value` for negatively-worded items
   - Example: "أشعر بالتوتر المستمر" → Higher agreement = Lower competence

4. **T-Score Transformation**
   - Formula: `T = 50 + 10 × ((raw - μ) / σ)`
   - Population norms: μ=3.0, σ=0.8
   - Interpretation: Mean=50, SD=10, Range=[20, 80]

5. **Banding System**
   - Table:
     ```
     T < 40        - ضعيف (Weak)     - يحتاج إلى تطوير مكثف
     40 ≤ T < 55   - متوسط (Average)  - أداء مقبول مع إمكانية التحسين
     T ≥ 55        - ممتاز (Excellent) - أداء متفوق
     ```

6. **Privacy & Quality**
   - Confidentiality statement
   - Data anonymization (no PII in reports)
   - Measurement reliability (Cronbach's alpha mention)
   - Validity: Construct, criterion, predictive

---

## Implementation Steps

### Step 1: Detect SDJ Mode
**Location**: `RenderResultPdfAsync` method (Line ~30)

```csharp
// Detect SDJ data presence
var hasSdjData = !string.IsNullOrEmpty(result.DimensionScoresJson) && 
                 result.DimensionScoresJson.Contains("\"SubDimensions\"");

if (hasSdjData)
{
    return await RenderSdjReportAsync(result, user, dimensions, ct);
}
else
{
    // Existing 3-page legacy report
    return await RenderLegacyReportAsync(result, user, dimensions, ct);
}
```

### Step 2: Parse SDJ Data
**New method**: `ParseSdjData`

```csharp
private (List<SdjDimension>, List<SdjSubDimension>, List<SdjTrack>) ParseSdjData(string dimensionScoresJson)
{
    var sdjData = JsonSerializer.Deserialize<SdjScoreSummary>(dimensionScoresJson);
    
    // Extract sub-dimensions sorted by T
    var subDims = sdjData.SubDimensions.OrderBy(s => s.T).ToList();
    
    // Extract parent dimensions
    var dims = sdjData.Dimensions.OrderBy(d => d.T).ToList();
    
    // Extract tracks
    var tracks = sdjData.TrackFits.ToList();
    
    return (dims, subDims, tracks);
}
```

### Step 3: Implement SkiaSharp Horizontal Bar Chart
**New method**: `RenderHorizontalBarChart`

```csharp
private byte[] RenderHorizontalBarChart(List<SdjSubDimension> subDimensions, int width = 500, int height = 720)
{
    using var surface = SKSurface.Create(new SKImageInfo(width, height));
    var canvas = surface.Canvas;
    canvas.Clear(SKColors.White);
    
    const int labelWidth = 150;
    const int barMaxWidth = 300;
    const int barHeight = 20;
    const int gap = 10;
    const int startX = labelWidth + 10;
    const int startY = 20;
    
    var arabicTypeface = SKTypeface.FromFamilyName("Amiri");
    var labelPaint = new SKPaint
    {
        Color = SKColors.Black,
        TextSize = 12,
        Typeface = arabicTypeface,
        IsAntialias = true,
        TextAlign = SKTextAlign.Right
    };
    
    var sortedSubs = subDimensions.OrderBy(s => s.T).ToList();
    
    for (int i = 0; i < sortedSubs.Count; i++)
    {
        var sub = sortedSubs[i];
        var y = startY + i * (barHeight + gap);
        
        // Draw label (Arabic, right-aligned)
        canvas.DrawText(sub.SubDimension, labelWidth, y + 15, labelPaint);
        
        // Calculate bar width (T-score domain 0-80)
        var barWidth = (int)((sub.T / 80.0) * barMaxWidth);
        
        // Band color
        var barColor = sub.T < 40 ? SKColors.Red :
                       sub.T < 55 ? SKColors.Orange :
                       SKColors.Green;
        
        var barPaint = new SKPaint { Color = barColor, IsAntialias = true };
        var rect = new SKRect(startX, y, startX + barWidth, y + barHeight);
        canvas.DrawRect(rect, barPaint);
        
        // Draw T-score at end of bar
        var scorePaint = new SKPaint
        {
            Color = barWidth > 40 ? SKColors.White : SKColors.Black,
            TextSize = 11,
            Typeface = arabicTypeface,
            IsAntialias = true
        };
        canvas.DrawText($"T={sub.T:F1}", startX + barWidth - 5, y + 15, scorePaint);
    }
    
    // Draw axis
    var axisPaint = new SKPaint { Color = SKColors.Gray, StrokeWidth = 1 };
    canvas.DrawLine(startX, startY - 10, startX, startY + sortedSubs.Count * (barHeight + gap), axisPaint);
    
    // Draw axis markers (0, 20, 40, 60, 80)
    for (int t = 0; t <= 80; t += 20)
    {
        var x = startX + (int)((t / 80.0) * barMaxWidth);
        canvas.DrawText(t.ToString(), x, startY - 5, labelPaint);
    }
    
    using var image = surface.Snapshot();
    using var data = image.Encode(SKEncodedImageFormat.Png, 100);
    return data.ToArray();
}
```

### Step 4: Page 1 SDJ Cover
**Modify**: `CreatePage1_CoverAndSummary`

```csharp
// Add logo at top center
column.Item().AlignCenter().Image("Resources/Images/SITES-ICON.png").FitWidth(150);

// Add SDJ title
column.Item().PaddingTop(12).AlignCenter().Text("منصة التحليل النفسي المتقدم – إطار التنمية المستدامة")
    .FontSize(18).Bold().FontColor(ReportTheme.Colors.Primary);

// Change strengths/weaknesses to use sub-dimensions
var topSubDimensions = subDimensions.OrderByDescending(s => s.T).Take(3).ToList();
var weakSubDimensions = subDimensions.OrderBy(s => s.T).Take(3).ToList();
```

### Step 5: Page 2 SDJ Charts
**New section**: Horizontal bar chart

```csharp
// Horizontal bar chart
column.Item().PaddingTop(16).Column(col =>
{
    col.Item().Text("الأبعاد الفرعية حسب الأداء").FontSize(16).Bold();
    col.Item().PaddingTop(8).Image(RenderHorizontalBarChart(subDimensions));
});

// Vector donuts (update to use sub-dimensions)
var donutSubDims = topSubDimensions.Take(3).Concat(weakSubDimensions.Take(3)).ToList();
// ... render donuts for sub-dimensions
```

### Step 6: Page 3 SDJ Tree
**New method**: `CreatePage3_SdjTree`

```csharp
private void CreatePage3_SdjTree(PageDescriptor page, List<SdjDimension> dimensions, List<SdjTrack> tracks)
{
    page.Content().Column(column =>
    {
        column.Item().Text("تفاصيل الأبعاد").FontSize(20).Bold();
        
        foreach (var dim in dimensions)
        {
            // Parent dimension header
            column.Item().PaddingTop(12).Row(row =>
            {
                row.AutoItem().Text("█").FontColor(GetBandColor(dim.Band));
                row.AutoItem().PaddingLeft(8).Text(dim.Dimension).FontSize(16).Bold();
                row.AutoItem().PaddingLeft(8).Text($"T={dim.T:F1}").FontSize(14);
                row.AutoItem().PaddingLeft(4).Text($"[{dim.Band}]").FontColor(GetBandColor(dim.Band));
            });
            
            // Sub-dimensions
            foreach (var sub in dim.SubDimensions.OrderBy(s => s.SubDimension))
            {
                column.Item().PaddingTop(4).PaddingLeft(20).Row(row =>
                {
                    row.AutoItem().Text("→").FontColor(ReportTheme.Colors.TextSecondary);
                    row.AutoItem().PaddingLeft(8).Text(sub.SubDimension).FontSize(13);
                    row.AutoItem().PaddingLeft(8).Text($"T={sub.T:F1}").FontSize(12);
                    row.AutoItem().PaddingLeft(4).Text($"[{sub.Band}]").FontColor(GetBandColor(sub.Band)).FontSize(11);
                });
            }
            
            // Separator
            column.Item().PaddingTop(8).LineHorizontal(1).LineColor(ReportTheme.Colors.Border);
        }
        
        // Track cards
        column.Item().PaddingTop(24).Text("تحليل المسارات المهنية").FontSize(18).Bold();
        column.Item().PaddingTop(12).Row(row =>
        {
            foreach (var track in tracks)
            {
                row.RelativeItem().PaddingRight(8).Border(1).BorderColor(ReportTheme.Colors.Border).Padding(12).Column(col =>
                {
                    col.Item().Text(track.TrackNameAr).FontSize(14).Bold();
                    col.Item().PaddingTop(4).Text(track.FitLevel.ToUpper()).FontSize(11).FontColor(GetFitLevelColor(track.FitLevel));
                    col.Item().PaddingTop(4).Text($"T={track.FitScore:F1}").FontSize(18).Bold();
                    col.Item().PaddingTop(8).Text(track.ReasoningAr).FontSize(10);
                });
            }
        });
    });
}
```

### Step 7: Page 4 Action Plan
**New method**: `CreatePage4_ActionPlan`

```csharp
private void CreatePage4_ActionPlan(PageDescriptor page, List<SdjSubDimension> subDimensions)
{
    page.Content().Column(column =>
    {
        column.Item().Text("خطة التطوير الموجهة").FontSize(20).Bold();
        
        var weakSubs = subDimensions.Where(s => s.T < 40).OrderBy(s => s.T).ToList();
        
        if (weakSubs.Any())
        {
            foreach (var sub in weakSubs)
            {
                column.Item().PaddingTop(16).Column(col =>
                {
                    col.Item().Text($"{sub.SubDimension}   (T={sub.T:F1})").FontSize(14).Bold().FontColor(ReportTheme.Colors.Danger);
                    
                    if (ActionPlanTemplates.TryGetValue(sub.SubDimension, out var recommendations))
                    {
                        foreach (var rec in recommendations)
                        {
                            col.Item().PaddingTop(8).PaddingLeft(12).Row(row =>
                            {
                                row.AutoItem().Text("→").FontColor(ReportTheme.Colors.Primary);
                                row.AutoItem().PaddingLeft(8).Text(rec).FontSize(11);
                            });
                        }
                    }
                });
            }
        }
        else
        {
            column.Item().PaddingTop(16).Text("لا توجد مجالات ضعيفة...").FontSize(12);
        }
    });
}
```

### Step 8: Page 5 Methodology
**New method**: `CreatePage5_Methodology`

```csharp
private void CreatePage5_Methodology(PageDescriptor page)
{
    page.Content().Column(column =>
    {
        column.Item().Text("المنهجية والمعايير").FontSize(20).Bold();
        
        // SDJ Framework
        column.Item().PaddingTop(16).Column(col =>
        {
            col.Item().Text("إطار التنمية المستدامة (SDJ)").FontSize(16).Bold();
            col.Item().PaddingTop(8).Text("نموذج قياس نفسي متقدم...").FontSize(11);
        });
        
        // Likert Scale
        column.Item().PaddingTop(16).Column(col =>
        {
            col.Item().Text("مقياس ليكرت الخماسي").FontSize(16).Bold();
            col.Item().PaddingTop(8).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(40);
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });
                
                table.Cell().Text("القيمة").FontSize(11).Bold();
                table.Cell().Text("التسمية").FontSize(11).Bold();
                table.Cell().Text("التفسير").FontSize(11).Bold();
                
                // ... add rows
            });
        });
        
        // T-Score formula
        column.Item().PaddingTop(16).Column(col =>
        {
            col.Item().Text("التحويل إلى درجة T").FontSize(16).Bold();
            col.Item().PaddingTop(8).Text("T = 50 + 10 × ((raw - μ) / σ)").FontSize(12).FontFamily("Courier New");
            col.Item().PaddingTop(4).Text("μ=3.0, σ=0.8").FontSize(10);
        });
        
        // Banding
        column.Item().PaddingTop(16).Column(col =>
        {
            col.Item().Text("نظام النطاقات").FontSize(16).Bold();
            // ... table
        });
        
        // Privacy
        column.Item().PaddingTop(16).Column(col =>
        {
            col.Item().Text("الخصوصية والجودة").FontSize(16).Bold();
            col.Item().PaddingTop(8).Text("جميع البيانات...").FontSize(10);
        });
    });
}
```

### Step 9: Update Main Method
```csharp
private async Task<byte[]> RenderSdjReportAsync(Result result, User user, IEnumerable<DimensionScoreDto> dimensions, CancellationToken ct)
{
    var (dimList, subList, trackList) = ParseSdjData(result.DimensionScoresJson);
    
    var pdf = Document.Create(container =>
    {
        container.Page(page => { ConfigurePageDefaults(page); CreatePage1_SdjCover(page, user, result, subList); });
        container.Page(page => { ConfigurePageDefaults(page); CreatePage2_SdjCharts(page, subList); });
        container.Page(page => { ConfigurePageDefaults(page); CreatePage3_SdjTree(page, dimList, trackList); });
        container.Page(page => { ConfigurePageDefaults(page); CreatePage4_ActionPlan(page, subList); });
        container.Page(page => { ConfigurePageDefaults(page); CreatePage5_Methodology(page); });
    });
    
    return pdf.GeneratePdf();
}
```

---

## Testing Plan

### Unit Tests (Manual)
1. **Logo rendering**: Verify SITES-ICON.png loads and centers
2. **Horizontal bar chart**: Check ascending sort, band colors, T-score labels
3. **Dimension tree**: Verify 5 parents → 24 subs hierarchy
4. **Action plan**: Confirm weak sub-dimensions get recommendations
5. **Methodology**: Verify all sections present with correct formulas

### Integration Tests
1. **SDJ session → PDF**: Complete SDJ session, download PDF, verify 5 pages
2. **Legacy session → PDF**: Complete legacy session, verify 3 pages (no SDJ)
3. **Mixed data**: Ensure USE_SDJ=0 doesn't break existing PDFs

### Visual QA
1. **Arabic shaping**: No � glyphs, proper ligatures
2. **RTL layout**: All Arabic text right-aligned
3. **Colors**: Band colors consistent (Red/Amber/Green)
4. **Spacing**: 8/12/16pt system maintained
5. **Page breaks**: No orphaned content

---

## File Checklist

### New Files (0)
- None (all changes in ModernPdfReportService.cs)

### Modified Files (1)
- `backend/PsyApi/Services/Reports/ModernPdfReportService.cs`:
  - Add `RenderSdjReportAsync` method (~400 lines)
  - Add `RenderHorizontalBarChart` method (~80 lines)
  - Add `CreatePage1_SdjCover` method (~60 lines)
  - Add `CreatePage2_SdjCharts` method (~80 lines)
  - Add `CreatePage3_SdjTree` method (~100 lines)
  - Add `CreatePage4_ActionPlan` method (~70 lines)
  - Add `CreatePage5_Methodology` method (~120 lines)
  - Add `ActionPlanTemplates` dictionary (~200 lines)
  - Add `ParseSdjData` helper method (~30 lines)
  - **Total additions**: ~1,140 lines

### Resources Required
- `Resources/Images/SITES-ICON.png` (logo file, 150x150px recommended)

---

## Acceptance Criteria

- [ ] Page 1: Logo centered, Arabic title, SDJ-specific KPIs
- [ ] Page 2: Horizontal bar chart renders (SkiaSharp), 24 sub-dimensions ascending, band colors correct
- [ ] Page 2: 6 vector donuts (180px) for top/bottom sub-dimensions
- [ ] Page 3: Dimension tree (5 parents → 24 subs) with T-scores and bands
- [ ] Page 3: 3 track cards with fit levels and reasoning
- [ ] Page 4: Action plan with 2-3 recommendations per weak sub-dimension
- [ ] Page 5: Methodology with Likert table, T-score formula, banding table, privacy statement
- [ ] All Arabic text renders correctly (HarfBuzz, no � glyphs)
- [ ] PDF generates in <3 seconds
- [ ] File size <500KB
- [ ] USE_SDJ flag correctly routes to SDJ vs legacy PDF

---

## Estimated Timeline

- **Step 1-2** (SDJ detection & parsing): 1 hour
- **Step 3** (SkiaSharp horizontal bar): 3 hours
- **Step 4-5** (Pages 1-2): 2 hours
- **Step 6** (Page 3 tree): 2 hours
- **Step 7** (Page 4 action plan + templates): 3 hours
- **Step 8** (Page 5 methodology): 2 hours
- **Step 9** (Integration + testing): 2 hours
- **Total**: ~15 hours

---

## Dependencies

- [x] SdjScoringService implemented
- [x] DimensionScoresJson contains SDJ data
- [ ] SITES-ICON.png logo file added to Resources/Images
- [x] QuestPDF Community License registered
- [x] SkiaSharp and HarfBuzz enabled

---

## Notes

- **No external AI calls**: All recommendations from hardcoded templates
- **Backward compatible**: Legacy PDF unchanged when USE_SDJ=0
- **Performance target**: <3s generation time (achievable with vector charts)
- **Arabic quality**: HarfBuzz already enabled, fonts registered ✅
- **Number format**: Western digits (0-9) as per existing standard

---

**Status**: READY TO IMPLEMENT  
**Priority**: HIGH  
**Blocker**: None (all dependencies met)
