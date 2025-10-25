
## Phase E: Admin UI Results Visualization

### Objective
Extend admin-ui to display SDJ dimension hierarchies, horizontal bar charts, enlarged donuts, and track fit cards.

### Files to Create

#### 1. `frontend/admin-ui/src/components/charts/HorizontalBarChart.tsx`
```tsx
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Cell } from 'recharts';

interface HorizontalBarChartProps {
  data: Array<{
    name: string;
    value: number;
    band: 'Weak' | 'Average' | 'Excellent';
  }>;
}

export function HorizontalBarChart({ data }: HorizontalBarChartProps) {
  const BAND_COLORS = {
    Weak: '#ef4444',      // red-500
    Average: '#f59e0b',   // amber-500
    Excellent: '#10b981'  // green-500
  };

  // Sort ascending by T-score
  const sortedData = [...data].sort((a, b) => a.value - b.value);

  return (
    <ResponsiveContainer width="100%" height={400}>
      <BarChart 
        data={sortedData} 
        layout="vertical"
        margin={{ top: 5, right: 30, left: 100, bottom: 5 }}
      >
        <CartesianGrid strokeDasharray="3 3" />
        <XAxis type="number" domain={[0, 80]} />
        <YAxis type="category" dataKey="name" width={90} style={{ fontSize: 12 }} />
        <Tooltip />
        <Bar dataKey="value" name="T-Score">
          {sortedData.map((entry, index) => (
            <Cell key={`cell-${index}`} fill={BAND_COLORS[entry.band]} />
          ))}
        </Bar>
      </BarChart>
    </ResponsiveContainer>
  );
}
```

#### 2. `frontend/admin-ui/src/components/SdjTrackCard.tsx`
```tsx
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { TrendingUp, Target, Users } from 'lucide-react';

interface SdjTrack {
  trackNameAr: string;
  trackNameEn: string;
  fitLevel: 'high' | 'medium' | 'low';
  fitScore: number;
  reasoningAr: string;
  keyCompetencies: string[];
}

interface SdjTrackCardProps {
  track: SdjTrack;
}

const FIT_CONFIG = {
  high: { color: 'bg-green-100 text-green-800 border-green-200', icon: TrendingUp, label: 'ملائمة عالية' },
  medium: { color: 'bg-amber-100 text-amber-800 border-amber-200', icon: Target, label: 'ملائمة متوسطة' },
  low: { color: 'bg-red-100 text-red-800 border-red-200', icon: Users, label: 'تحتاج تطوير' }
};

export function SdjTrackCard({ track }: SdjTrackCardProps) {
  const config = FIT_CONFIG[track.fitLevel];
  const Icon = config.icon;

  return (
    <Card className="hover:shadow-lg transition-shadow">
      <CardHeader className="pb-3">
        <div className="flex items-start justify-between">
          <div>
            <CardTitle className="text-lg mb-1">{track.trackNameAr}</CardTitle>
            <p className="text-sm text-muted-foreground">{track.trackNameEn}</p>
          </div>
          <Badge variant="outline" className={config.color}>
            <Icon className="h-3 w-3 mr-1" />
            {config.label}
          </Badge>
        </div>
      </CardHeader>
      <CardContent className="space-y-3">
        <div className="flex items-center gap-2">
          <span className="text-2xl font-bold text-primary">{track.fitScore.toFixed(1)}</span>
          <span className="text-sm text-muted-foreground">T-Score</span>
        </div>
        
        <p className="text-sm text-gray-700 leading-relaxed" dir="rtl">
          {track.reasoningAr}
        </p>

        <div>
          <p className="text-xs font-semibold text-gray-500 mb-2">الكفاءات الرئيسية:</p>
          <div className="flex flex-wrap gap-1">
            {track.keyCompetencies.map((comp, idx) => (
              <Badge key={idx} variant="secondary" className="text-xs">
                {comp}
              </Badge>
            ))}
          </div>
        </div>
      </CardContent>
    </Card>
  );
}
```

#### 3. `frontend/admin-ui/src/pages/ResultDetailSDJ.tsx`
```tsx
import { useParams } from 'react-router-dom';
import { useEffect, useState } from 'react';
import { AdminApi } from '@/lib/apiAdmin';
import { HorizontalBarChart } from '@/components/charts/HorizontalBarChart';
import { SdjTrackCard } from '@/components/SdjTrackCard';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Accordion, AccordionContent, AccordionItem, AccordionTrigger } from '@/components/ui/accordion';

export default function ResultDetailSDJ() {
  const { id } = useParams();
  const [data, setData] = useState<any>(null);

  useEffect(() => {
    const load = async () => {
      const result = await AdminApi.resultDetail(Number(id));
      setData(result);
    };
    load();
  }, [id]);

  if (!data?.sdjData) {
    return <div>Loading or no SDJ data...</div>;
  }

  const { sdjData } = data;

  // Prepare chart data
  const chartData = sdjData.Dimensions.map((d: any) => ({
    name: d.Dimension,
    value: d.T,
    band: d.Band
  }));

  return (
    <div className="container mx-auto px-4 py-6 space-y-6" dir="rtl">
      {/* SDJ Dimension Tree */}
      <Card>
        <CardHeader>
          <CardTitle>شجرة الأبعاد - التنمية المستدامة</CardTitle>
        </CardHeader>
        <CardContent>
          <Accordion type="multiple" className="w-full">
            {sdjData.Dimensions.map((dim: any, idx: number) => {
              const subDims = sdjData.SubDimensions.filter(
                (sub: any) => sub.Dimension === dim.Dimension
              );
              
              return (
                <AccordionItem key={idx} value={`dim-${idx}`}>
                  <AccordionTrigger className="text-lg font-semibold">
                    <div className="flex items-center gap-3 w-full">
                      <span>{dim.Dimension}</span>
                      <span className="text-sm text-muted-foreground">
                        (T: {dim.T.toFixed(1)} - {dim.Band})
                      </span>
                    </div>
                  </AccordionTrigger>
                  <AccordionContent>
                    <div className="space-y-2 pr-6">
                      {subDims.map((sub: any, subIdx: number) => (
                        <div key={subIdx} className="flex items-center justify-between p-3 bg-muted rounded-lg">
                          <span className="font-medium">{sub.SubDimension}</span>
                          <div className="flex items-center gap-3">
                            <span className="text-sm">T: {sub.T.toFixed(1)}</span>
                            <Badge variant={sub.Band === 'Excellent' ? 'default' : sub.Band === 'Weak' ? 'destructive' : 'secondary'}>
                              {sub.Band}
                            </Badge>
                          </div>
                        </div>
                      ))}
                    </div>
                  </AccordionContent>
                </AccordionItem>
              );
            })}
          </Accordion>
        </CardContent>
      </Card>

      {/* Horizontal Bar Chart */}
      <Card>
        <CardHeader>
          <CardTitle>توزيع الدرجات (T-Scores)</CardTitle>
        </CardHeader>
        <CardContent>
          <HorizontalBarChart data={chartData} />
        </CardContent>
      </Card>

      {/* SDJ Tracks */}
      <div className="grid md:grid-cols-3 gap-6">
        {sdjData.TrackFits.map((track: any, idx: number) => (
          <SdjTrackCard key={idx} track={track} />
        ))}
      </div>
    </div>
  );
}
```

#### 4. Update `frontend/admin-ui/src/components/charts/ApexCharts.tsx`
Change donut chart size from 120px to 180px:
```tsx
// Line ~50
options={{
  ...baseOptions,
  chart: {
    ...baseOptions.chart,
    width: 180,  // Changed from 120
    height: 180  // Changed from 120
  }
}}
```

#### 5. Update `frontend/admin-ui/src/pages/ResultsList.tsx`
Add SDJ profile badge to results list:
```tsx
{result.sdjProfile && (
  <Badge variant="outline" className="bg-blue-50 text-blue-700 border-blue-200">
    SDJ: {result.topStrength || 'N/A'}
  </Badge>
)}
```

### Dependencies to Install
```json
{
  "recharts": "^2.10.0"
}
```

### Acceptance Criteria
- [ ] Horizontal bar chart displays SDJ dimensions sorted ascending by T-score
- [ ] Bar colors match band (Red=Weak, Amber=Average, Green=Excellent)
- [ ] Accordion shows dimension → sub-dimensions hierarchy
- [ ] Each sub-dimension displays T-score and band badge
- [ ] Track cards show fit level, reasoning in Arabic, and key competencies
- [ ] Donut charts enlarged to 180px diameter
- [ ] Results list shows SDJ badge when available

---

## Phase F: PDF Report Redesign

### Objective
Redesign QuestPDF report service to include SDJ sections, enlarged charts, track fit, action plan, and methodology.

### Files to Modify

#### 1. `backend/PsyApi/Services/Reports/ModernPdfReportService.cs`

**Page 1: Cover & Summary**
```csharp
private void RenderCoverPage(IContainer container, User user, SdjScoreSummary sdjScores)
{
    container.Page(page =>
    {
        page.Size(PageSizes.A4);
        page.Margin(40);
        page.DefaultTextStyle(x => x.FontFamily("Amiri").FontSize(12));

        page.Header().AlignCenter().Column(column =>
        {
            // Logo (SITES-ICON.png centered)
            column.Item().Image("assets/SITES-ICON.png").FitWidth();
            
            column.Item().PaddingVertical(10).Text("منصة التحليل النفسي المتقدم")
                .FontSize(20).Bold().FontColor("#1e40af");
            
            column.Item().Text("إطار التنمية المستدامة (SDJ)")
                .FontSize(16).SemiBold().FontColor("#3b82f6");
        });

        page.Content().PaddingTop(30).Column(column =>
        {
            // User Info
            column.Item().Background("#f8fafc").Padding(15).Column(c =>
            {
                c.Item().Text($"المشارك: {user.FullName}").FontSize(14).Bold();
                c.Item().Text($"الرقم الوطني: {user.NationalId}").FontSize(12);
                c.Item().Text($"تاريخ الإنشاء: {DateTime.Now:yyyy-MM-dd}").FontSize(12);
            });

            // Top 3 Strengths
            column.Item().PaddingTop(20).Text("أهم 3 نقاط قوة").FontSize(14).Bold();
            var strengths = sdjScores.SubDimensions.OrderByDescending(s => s.T).Take(3);
            foreach (var s in strengths)
            {
                column.Item().Row(row =>
                {
                    row.Spacing(10);
                    row.AutoItem().Text("✓").FontColor("#10b981");
                    row.RelativeItem().Text($"{s.SubDimension} (T: {s.T:F1})");
                });
            }

            // Top 3 Development Areas
            column.Item().PaddingTop(15).Text("أهم 3 مجالات للتطوير").FontSize(14).Bold();
            var weaknesses = sdjScores.SubDimensions.OrderBy(s => s.T).Take(3);
            foreach (var w in weaknesses)
            {
                column.Item().Row(row =>
                {
                    row.Spacing(10);
                    row.AutoItem().Text("⚠").FontColor("#ef4444");
                    row.RelativeItem().Text($"{w.SubDimension} (T: {w.T:F1})");
                });
            }
        });
    });
}
```

**Page 2: Charts**
```csharp
private void RenderChartsPage(IContainer container, SdjScoreSummary sdjScores)
{
    container.Page(page =>
    {
        page.Size(PageSizes.A4);
        page.Margin(40);

        page.Content().Column(column =>
        {
            // Horizontal Bar Chart (SVG or SkiaSharp rendering)
            column.Item().Height(300).Canvas((canvas, size) =>
            {
                RenderHorizontalBarChart(canvas, size, sdjScores.Dimensions);
            });

            // Enlarged Donut Charts (180px each)
            column.Item().PaddingTop(20).Row(row =>
            {
                row.Spacing(15);
                foreach (var dim in sdjScores.Dimensions.Take(5))
                {
                    row.AutoItem().Width(180).Height(180).Canvas((canvas, size) =>
                    {
                        RenderDonutChart(canvas, size, dim);
                    });
                }
            });
        });
    });
}

private void RenderHorizontalBarChart(SKCanvas canvas, Size size, List<SdjDimensionScore> dimensions)
{
    var sorted = dimensions.OrderBy(d => d.T).ToList();
    var barHeight = size.Height / sorted.Count;

    for (int i = 0; i < sorted.Count; i++)
    {
        var dim = sorted[i];
        var y = i * barHeight;
        var barWidth = (dim.T / 80.0f) * size.Width;

        // Draw bar
        var barColor = dim.Band == "Excellent" ? SKColors.Green : 
                       dim.Band == "Weak" ? SKColors.Red : SKColors.Orange;
        
        var paint = new SKPaint { Color = barColor, Style = SKPaintStyle.Fill };
        canvas.DrawRect(0, y, barWidth, barHeight - 5, paint);

        // Draw label
        var textPaint = new SKPaint { Color = SKColors.Black, TextSize = 12, IsAntialias = true };
        canvas.DrawText(dim.Dimension, 5, y + 15, textPaint);
    }
}
```

**Page 3: SDJ Dimension Tree & Track Fit**
```csharp
private void RenderSdjDetailsPage(IContainer container, SdjScoreSummary sdjScores)
{
    container.Page(page =>
    {
        page.Size(PageSizes.A4);
        page.Margin(40);

        page.Content().Column(column =>
        {
            column.Item().Text("التحليل التفصيلي - الأبعاد والمسارات")
                .FontSize(16).Bold().FontColor("#1e40af");

            // Dimension Tree
            foreach (var dim in sdjScores.Dimensions)
            {
                column.Item().PaddingTop(10).Background("#f1f5f9").Padding(10).Column(c =>
                {
                    c.Item().Text($"{dim.Dimension} (T: {dim.T:F1} - {dim.Band})")
                        .FontSize(13).Bold();

                    var subDims = sdjScores.SubDimensions.Where(s => s.Dimension == dim.Dimension);
                    foreach (var sub in subDims)
                    {
                        c.Item().PaddingLeft(15).Row(row =>
                        {
                            row.Spacing(5);
                            row.AutoItem().Text("•");
                            row.RelativeItem().Text($"{sub.SubDimension}: {sub.T:F1} ({sub.Band})");
                        });
                    }
                });
            }

            // Track Fit Analysis
            column.Item().PageBreak();
            column.Item().Text("تحليل المسارات المهنية").FontSize(16).Bold();
            foreach (var track in sdjScores.TrackFits)
            {
                column.Item().PaddingTop(10).Border(1).BorderColor("#e2e8f0").Padding(10).Column(c =>
                {
                    c.Item().Text(track.TrackNameAr).FontSize(14).Bold();
                    c.Item().Text($"مستوى الملائمة: {track.FitLevel} (T: {track.FitScore:F1})");
                    c.Item().Text(track.ReasoningAr).FontSize(11).LineHeight(1.5f);
                });
            }
        });
    });
}
```

**Page 4: Action Plan**
```csharp
private void RenderActionPlanPage(IContainer container, SdjScoreSummary sdjScores)
{
    container.Page(page =>
    {
        page.Size(PageSizes.A4);
        page.Margin(40);

        page.Content().Column(column =>
        {
            column.Item().Text("خطة التطوير الموصى بها")
                .FontSize(16).Bold().FontColor("#1e40af");

            var weakSubDims = sdjScores.SubDimensions.Where(s => s.Band == "Weak").Take(5);
            
            foreach (var sub in weakSubDims)
            {
                column.Item().PaddingTop(15).Column(c =>
                {
                    c.Item().Text($"تطوير: {sub.SubDimension}").FontSize(13).Bold();
                    
                    // Template-based recommendations (not AI)
                    var recommendations = GetTemplateRecommendations(sub.SubDimension);
                    foreach (var rec in recommendations)
                    {
                        c.Item().PaddingLeft(10).Row(row =>
                        {
                            row.Spacing(5);
                            row.AutoItem().Text("→");
                            row.RelativeItem().Text(rec).FontSize(11);
                        });
                    }
                });
            }
        });
    });
}

private List<string> GetTemplateRecommendations(string subDimension)
{
    // Hardcoded template recommendations for each sub-dimension
    var templates = new Dictionary<string, List<string>>
    {
        ["الثقة بالنفس"] = new List<string>
        {
            "مارس تقنيات التأكيدات الإيجابية يومياً",
            "احتفظ بدفتر إنجازات لتوثيق نجاحاتك",
            "شارك في مواقف تتحدى منطقة راحتك تدريجياً"
        },
        ["التخطيط الاستراتيجي"] = new List<string>
        {
            "استخدم أدوات التخطيط مثل جداول جانت",
            "حدد أهداف SMART (محددة، قابلة للقياس، قابلة للتحقيق، ذات صلة، محددة زمنياً)",
            "راجع خططك أسبوعياً وعدّلها حسب الحاجة"
        }
        // Add templates for all 24 sub-dimensions...
    };

    return templates.ContainsKey(subDimension) 
        ? templates[subDimension] 
        : new List<string> { "استشر مدرب تطوير مهني متخصص", "ابحث عن دورات تدريبية في هذا المجال" };
}
```

**Page 5: Methodology**
```csharp
private void RenderMethodologyPage(IContainer container)
{
    container.Page(page =>
    {
        page.Size(PageSizes.A4);
        page.Margin(40);

        page.Content().Column(column =>
        {
            column.Item().Text("منهجية التقييم").FontSize(16).Bold();

            column.Item().PaddingTop(10).Text(
                "يعتمد إطار التنمية المستدامة (SDJ) على نموذج علمي لقياس 5 أبعاد رئيسية و 24 بُعد فرعي " +
                "من خلال 120 عبارة بمقياس ليكرت (1-5)."
            ).LineHeight(1.5f);

            column.Item().PaddingTop(15).Text("مقياس ليكرت المستخدم:").FontSize(13).Bold();
            column.Item().Row(row =>
            {
                row.Spacing(10);
                row.AutoItem().Text("1 = لا أوافق بشدة");
                row.AutoItem().Text("2 = لا أوافق");
                row.AutoItem().Text("3 = محايد");
                row.AutoItem().Text("4 = أوافق");
                row.AutoItem().Text("5 = أوافق بشدة");
            });

            column.Item().PaddingTop(15).Text("التصحيح العكسي:").FontSize(13).Bold();
            column.Item().Text(
                "بعض العبارات تُصحح عكسياً (الدرجة = 6 - الإجابة الخام) لضمان دقة القياس."
            );

            column.Item().PaddingTop(15).Text("حساب الدرجات المعيارية (T-Scores):").FontSize(13).Bold();
            column.Item().Text(
                "T = 50 + 10 × ((الدرجة الخام - المتوسط) / الانحراف المعياري)\n" +
                "المتوسط = 3.0، الانحراف المعياري = 0.8"
            );

            column.Item().PaddingTop(15).Text("النطاقات:").FontSize(13).Bold();
            column.Item().Text("• ضعيف: T < 40");
            column.Item().Text("• متوسط: 40 ≤ T < 55");
            column.Item().Text("• ممتاز: T ≥ 55");
        });
    });
}
```

### Acceptance Criteria
- [ ] PDF contains 5 pages (Cover, Charts, SDJ Details, Action Plan, Methodology)
- [ ] Cover page shows SITES-ICON.png logo centered
- [ ] Top 3 strengths and development areas displayed on page 1
- [ ] Horizontal bar chart rendered with color-coded bands (page 2)
- [ ] Donut charts enlarged to 180px diameter (page 2)
- [ ] SDJ dimension tree with expandable sub-dimensions (page 3)
- [ ] Track fit analysis with Arabic reasoning (page 3)
- [ ] Action plan with 2-3 template recommendations per weak sub-dimension (page 4)
- [ ] Methodology page explains Likert scale, T-scores, reverse scoring, and banding (page 5)
- [ ] All Arabic text rendered correctly with HarfBuzz shaping

---

## Phase G: Testing & Documentation

### Unit Tests

#### 1. `backend/PsyApi.Tests/SdjScoringServiceTests.cs`
```csharp
using Xunit;
using PsyApi.Services.Scoring;

public class SdjScoringServiceTests
{
    [Fact]
    public void ScoreLikertItem_ReverseScoring_ReturnsInvertedScore()
    {
        // Arrange
        var item = new Item { Reverse = true };
        var answer = "5"; // User selected "أوافق بشدة"

        // Act
        var service = CreateService();
        var score = service.ScoreLikertItem(item, answer);

        // Assert
        Assert.Equal(1, score); // 6 - 5 = 1
    }

    [Fact]
    public void ComputeTScore_RawScore4_ReturnsApprox62()
    {
        // Arrange
        var service = CreateService();

        // Act
        var tScore = service.ComputeTScore(4.0);

        // Assert
        Assert.InRange(tScore, 62.4, 62.6); // (4.0 - 3.0) / 0.8 * 10 + 50 = 62.5
    }

    [Fact]
    public void GetBand_TScore38_ReturnsWeak()
    {
        // Arrange
        var service = CreateService();

        // Act
        var band = service.GetBand(38);

        // Assert
        Assert.Equal("Weak", band);
    }

    // Add more tests for aggregation, track mapping, etc.
}
```

#### 2. `backend/PsyApi.Tests/SdjApiTests.cs`
```csharp
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;

public class SdjApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task SubmitAnswer_LikertNumericValue_AcceptsSuccessfully()
    {
        // Arrange
        var client = _factory.CreateClient();
        var sessionId = await CreateTestSession(client);

        // Act
        var response = await client.PostAsJsonAsync(
            $"/api/sessions/{sessionId}/submit-answer",
            new { ItemId = "I001", Answer = "4" }
        );

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetBirkmanReport_UseSdj1_ReturnsSdjDataField()
    {
        // Arrange
        Environment.SetEnvironmentVariable("USE_SDJ", "1");
        var client = _factory.CreateClient();
        var resultId = await CreateCompletedSession(client);

        // Act
        var response = await client.GetAsync($"/api/results/{resultId}/birkman");
        var json = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Contains("\"SdjData\":", json);
        Assert.Contains("\"Dimensions\":", json);
        Assert.Contains("\"TrackFits\":", json);
    }
}
```

### E2E Tests

#### 3. `frontend/user-ui/tests/exam-sdj.spec.ts`
```typescript
import { test, expect } from '@playwright/test';

test('Complete SDJ exam with Likert items', async ({ page }) => {
  // 1. Navigate and start exam
  await page.goto('/instructions');
  await page.click('button:has-text("ابدأ الاختبار")');
  
  // 2. Verify Likert question
  await expect(page.locator('[role="radiogroup"]')).toBeVisible();
  await expect(page.locator('label:has-text("أوافق بشدة")')).toBeVisible();
  
  // 3. Select option and verify network payload
  const [request] = await Promise.all([
    page.waitForRequest(req => req.url().includes('/submit-answer')),
    page.click('label:has-text("أوافق بشدة")'),
    page.click('button:has-text("التالي")')
  ]);
  
  const payload = request.postDataJSON();
  expect(payload.Answer).toBe("5"); // Numeric value sent
  
  // 4. Verify timer
  await expect(page.locator('.timer')).toContainText(/\d{2}/);
  
  // 5. Complete session and check results
  for (let i = 0; i < 119; i++) {
    await page.click('label:has-text("محايد")');
    await page.click('button:has-text("التالي")');
  }
  
  await page.click('button:has-text("إنهاء الاختبار")');
  await expect(page).toHaveURL(/\/results/);
});
```

### Documentation

#### 4. `CHANGELOG_SDJ.md`
```markdown
# SDJ Migration Changelog

## Version 2.0.0 - SDJ Framework (2024-12-XX)

### Added
- **SDJ Question Bank**: 120 Likert items covering 5 dimensions, 24 sub-dimensions
- **Scoring Engine**: `SdjScoringService` with reverse scoring, T-scores, banding
- **API Extensions**: Non-breaking additions to `/results/{id}/birkman` endpoint
- **User UI**: Numeric Likert value submission (1-5)
- **Admin UI**: SDJ dimension tree, horizontal bar charts, track fit cards
- **PDF Report**: 5-page redesign with charts, action plan, methodology

### Changed
- **Likert Component**: Now stores numeric values instead of Arabic labels
- **ResultsController**: Auto-detects SDJ vs legacy format in JSON
- **Timer Instructions**: Updated to mention 45-second SDJ items

### Fixed
- None

### Migration Guide
1. Apply migration: `dotnet ef database update`
2. Set environment variable: `USE_SDJ=1`
3. Seed SDJ questions: `dotnet run --seed`
4. Restart backend and frontend services
5. Test with sample user session

### Rollback
- Set `USE_SDJ=0` to revert to legacy question bank
- No data loss; both formats coexist

### Breaking Changes
- None (backward compatible)
```

#### 5. Sample PDF Generation Script
```powershell
# test_sdj_pdf_generation.ps1

$env:USE_SDJ = "1"

Write-Host "Generating sample SDJ PDF reports..."

# Create test users
$testUsers = @(
    @{ NationalId = "1234567890"; FullName = "أحمد محمد العلي" },
    @{ NationalId = "0987654321"; FullName = "فاطمة سعيد الزهراني" },
    @{ NationalId = "1122334455"; FullName = "خالد عبدالله النمر" }
)

foreach ($user in $testUsers) {
    Write-Host "Creating session for $($user.FullName)..."
    
    # Start session
    $session = Invoke-RestMethod -Uri "http://localhost:5000/api/sessions/start" `
        -Method POST -Body (@{ NationalId = $user.NationalId } | ConvertTo-Json) `
        -ContentType "application/json"
    
    $sessionId = $session.sessionId
    
    # Answer 120 questions with random Likert values
    for ($i = 1; $i -le 120; $i++) {
        $itemId = "I" + $i.ToString("D3")
        $answer = Get-Random -Minimum 1 -Maximum 6
        
        Invoke-RestMethod -Uri "http://localhost:5000/api/sessions/$sessionId/submit-answer" `
            -Method POST -Body (@{ ItemId = $itemId; Answer = $answer.ToString() } | ConvertTo-Json) `
            -ContentType "application/json"
    }
    
    # Submit session
    $result = Invoke-RestMethod -Uri "http://localhost:5000/api/sessions/$sessionId/submit" `
        -Method POST
    
    # Download PDF
    $resultId = $result.resultId
    Invoke-WebRequest -Uri "http://localhost:5000/api/results/$resultId/pdf" `
        -OutFile "sample_pdf_$($user.NationalId).pdf"
    
    Write-Host "✓ PDF generated: sample_pdf_$($user.NationalId).pdf"
}

Write-Host "`n✅ All sample PDFs generated successfully!"
```

### Acceptance Criteria for Phase G
- [ ] All unit tests pass (15+ tests covering scoring, aggregation, T-scores, banding)
- [ ] All API tests pass (10+ tests for Likert validation, SDJ endpoints)
- [ ] E2E test completes full SDJ exam flow without errors
- [ ] 3 sample PDFs generated with diverse score profiles
- [ ] CHANGELOG_SDJ.md documents all changes comprehensively
- [ ] Migration guide tested by external team member
- [ ] Rollback procedure verified (USE_SDJ=0 works correctly)

---

## Summary

**Total Implementation Effort Estimate**:
- Phase E (Admin UI): 8-10 hours
- Phase F (PDF Report): 12-15 hours
- Phase G (Testing & Docs): 6-8 hours
- **Total**: 26-33 hours

**Critical Dependencies**:
- Recharts library for horizontal bar charts
- SkiaSharp for PDF chart rendering
- QuestPDF 2024.x with HarfBuzz support

**Risks**:
- Arabic text rendering in PDFs (mitigated by HarfBuzz + SkiaSharp)
- Chart rendering performance for 24 sub-dimensions (mitigated by pagination)
- Template recommendation quality (mitigated by psychologist review)

**Next Actions**:
1. Install Recharts in admin-ui: `npm install recharts`
2. Create horizontal bar chart component
3. Implement SDJ detail page with accordion
4. Update PDF service with 5-page structure
5. Write comprehensive test suite
6. Generate sample PDFs for validation
