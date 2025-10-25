# SDJ Migration Plan - Sustainable Development Journey Framework

**Version:** 1.0  
**Date:** 2025-10-24  
**Status:** Planning Phase  
**Target:** Zero-regression migration from legacy psychometric framework to SDJ

---

## Executive Summary

This document outlines the comprehensive migration plan from the current psychometric question bank (200 items, mixed types) to a new **Sustainable Development Journey (SDJ)** framework. The SDJ framework is designed for Arabic-only offline psychometric assessment aligned with sustainable development competencies.

### Key Changes
- **Content:** Convert all TEXT items → LikertAgreement (1-5 scale)
- **Schema:** Add SubDimension and Reverse flags to Item model
- **Scoring:** Implement SDJ dimension aggregation, reverse scoring, T-score banding, track mapping
- **Reporting:** Add SDJ profile sections to PDF (tracks, action plans, methodology)
- **UI:** Update question rendering and results visualization
- **Stability:** Maintain API contracts, no breaking changes for existing clients

---

## 1. SDJ Dimension Tree & Arabic Names

### Parent Dimensions (أبعاد رئيسية)

1. **التميز الذاتي** (Self-Excellence)
   - الوعي الذاتي (Self-Awareness)
   - الثقة بالنفس (Self-Confidence)
   - التنظيم الذاتي (Self-Regulation)
   - التعلم المستمر (Continuous Learning)
   - المرونة النفسية (Psychological Flexibility)

2. **التواصل والعلاقات** (Communication & Relationships)
   - الذكاء العاطفي (Emotional Intelligence)
   - التواصل الفعال (Effective Communication)
   - التعاون (Collaboration)
   - حل النزاعات (Conflict Resolution)
   - بناء العلاقات (Relationship Building)

3. **النجاح المهني** (Career Success)
   - القيادة (Leadership)
   - حل المشكلات (Problem Solving)
   - الإبداع والابتكار (Creativity & Innovation)
   - إدارة الوقت (Time Management)
   - التخطيط الاستراتيجي (Strategic Planning)

4. **المسؤولية الاجتماعية** (Social Responsibility)
   - الوعي المجتمعي (Community Awareness)
   - الأخلاق المهنية (Professional Ethics)
   - الاستدامة (Sustainability)
   - العمل التطوعي (Volunteer Work)
   - المواطنة الفاعلة (Active Citizenship)

5. **الصحة والتوازن** (Health & Balance)
   - الصحة النفسية (Mental Health)
   - الصحة الجسدية (Physical Health)
   - إدارة الضغوط (Stress Management)
   - التوازن بين العمل والحياة (Work-Life Balance)
   - الرفاهية الشاملة (Holistic Well-being)

---

## 2. Likert Scale Specification

### Arabic Anchors (1-5 Scale)

| Value | Arabic Label | English Translation |
|-------|-------------|---------------------|
| 1 | لا أوافق بشدة | Strongly Disagree |
| 2 | لا أوافق | Disagree |
| 3 | محايد | Neutral |
| 4 | أوافق | Agree |
| 5 | أوافق بشدة | Strongly Agree |

### Reverse Scoring Rules

- Items marked with `reverse=1` will be scored as: `score = 6 - raw_value`
- Example: User selects "أوافق بشدة" (5) → Reverse scored as 1
- Used for negatively-worded items (e.g., "أشعر بالتوتر المستمر")

### Item Time Limits

- Default: 45 seconds per item (can be overridden per item)
- Range: 20-120 seconds
- Session cap: 1 hour total

---

## 3. Field-Level Mapping

### Old CSV Schema (questions_fixed_extended_plus_personality.csv)

```csv
item_id,text_ar,type,dimension_tags,difficulty,time_limit_seconds,max_score,correct_answer,options
```

### New CSV Schema (questions_sdj_ar.csv)

```csv
item_code,text_ar,type,dimension,sub_dimension,anchors_ar,reverse,time_limit_seconds,max_score,difficulty
```

### Field Mappings

| Old Field | New Field | Transformation |
|-----------|-----------|----------------|
| `item_id` | `item_code` | Format as I001, I002, etc. |
| `text_ar` | `text_ar` | Convert TEXT prompts to Likert statements |
| `type` | `type` | TEXT → LikertAgreement; others unchanged |
| `dimension_tags` | `dimension` + `sub_dimension` | Split into parent + child |
| `difficulty` | `difficulty` | Keep 1-5 scale |
| `time_limit_seconds` | `time_limit_seconds` | Default 45s for Likert |
| `max_score` | `max_score` | 5 for all Likert items |
| `correct_answer` | (removed) | Not applicable for Likert |
| `options` | `anchors_ar` | Standard Likert anchors |
| (new) | `reverse` | 0 or 1 flag |

### Example Transformations

**Before (TEXT item):**
```csv
I003,صف شخصيتك في ثلاث كلمات.,Text,التعبير الذاتي,3,60,5,,
```

**After (LikertAgreement item):**
```csv
I003,أستطيع وصف شخصيتي بوضوح.,LikertAgreement,التميز الذاتي,الوعي الذاتي,لا أوافق بشدة|لا أوافق|محايد|أوافق|أوافق بشدة,0,45,5,2
```

---

## 4. Database Schema Changes

### 4.1 Item Model Extension

**C# Model Changes (Item.cs):**

```csharp
public class Item
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(10)]
    public string ItemCode { get; set; } = string.Empty;
    
    [Required]
    public string TextAr { get; set; } = string.Empty;
    
    [Required]
    [StringLength(30)]
    public string Type { get; set; } = string.Empty;
    
    [Required]
    public string DimensionTags { get; set; } = string.Empty; // DEPRECATED: Use Dimension + SubDimension
    
    // NEW SDJ FIELDS
    [StringLength(100)]
    public string? Dimension { get; set; } // Parent dimension (e.g., "التميز الذاتي")
    
    [StringLength(100)]
    public string? SubDimension { get; set; } // Sub-dimension (e.g., "الوعي الذاتي")
    
    public bool Reverse { get; set; } = false; // Reverse scoring flag
    
    [Required]
    [Range(1, 5)]
    public int Difficulty { get; set; }
    
    [Required]
    [Range(1, 300)]
    public int TimeLimitSeconds { get; set; }
    
    [Required]
    [Range(1, 10)]
    public int MaxScore { get; set; }
    
    public string? CorrectAnswer { get; set; }
    public string? Options { get; set; }
    
    // Navigation properties
    public ICollection<SessionItem> SessionItems { get; set; } = new List<SessionItem>();
}
```

### 4.2 EF Migration

**Migration Name:** `AddSdjFieldsToItems`

```csharp
public partial class AddSdjFieldsToItems : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Dimension",
            table: "Items",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SubDimension",
            table: "Items",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "Reverse",
            table: "Items",
            type: "bit",
            nullable: false,
            defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Dimension", table: "Items");
        migrationBuilder.DropColumn(name: "SubDimension", table: "Items");
        migrationBuilder.DropColumn(name: "Reverse", table: "Items");
    }
}
```

### 4.3 Seeder Updates

**File:** `backend/PsyApi/Services/DataSeeder.cs`

**Changes:**
1. Add CSV column mapping for `dimension`, `sub_dimension`, `reverse`
2. Parse reverse flag (default 0)
3. Populate new fields during Item creation
4. Add validation for SDJ dimension tree

**Feature Flag:** `USE_SDJ` (environment variable)
- `USE_SDJ=0` → Load legacy CSV (default for rollback safety)
- `USE_SDJ=1` → Load new SDJ CSV

---

## 5. API Contract Stability

### 5.1 Maintained Endpoints (No Breaking Changes)

**All existing endpoints maintain current response shapes:**

- `POST /api/sessions/start`
- `GET /api/sessions/{sessionId}`
- `GET /api/sessions/{sessionId}/next`
- `POST /api/sessions/{sessionId}/answer`
- `POST /api/sessions/{sessionId}/finish`
- `POST /api/sessions/{sessionId}/submit`
- `GET /api/results`
- `GET /api/results/{id}/birkman`
- `GET /api/results/{id}/pdf`

### 5.2 Extended Fields (Additive Only)

**QuestionResponse (GET /sessions/{sessionId}/next):**

```json
{
  "id": 123,
  "item_id": "I001",
  "text_ar": "أستطيع وصف شخصيتي بوضوح.",
  "type": "LikertAgreement",
  "dimension_tags": "التعبير الذاتي",  // DEPRECATED but maintained
  "dimension": "التميز الذاتي",           // NEW
  "sub_dimension": "الوعي الذاتي",         // NEW
  "difficulty": 2,
  "time_limit_seconds": 45,
  "max_score": 5,
  "options": "لا أوافق بشدة|لا أوافق|محايد|أوافق|أوافق بشدة"
}
```

**DimensionScore (GET /results/{id}/birkman):**

```json
{
  "dimension": "التميز الذاتي",
  "subDimensions": [                      // NEW
    {
      "name": "الوعي الذاتي",
      "raw": 4.2,
      "t": 52.3,
      "percentile": 0.58,
      "band": "Average"
    }
  ],
  "raw": 4.5,
  "z": 0.45,
  "t": 54.5,
  "percentile": 0.67,
  "band": "Average"                       // NEW
}
```

**Result Detail (GET /results/{id}/birkman):**

```json
{
  "session": { ... },
  "user": { ... },
  "totals": { ... },
  "dimensions": [ ... ],
  "byType": [ ... ],
  "timing": { ... },
  "birkmanAxes": [ ... ],
  "recommendations": [ ... ],
  "courses": [ ... ],
  "sdjProfile": {                         // NEW
    "topTracks": [
      {
        "name": "مسار التميز الذاتي",
        "fit": "high",
        "reasoning": "التركيز على تطوير الوعي الذاتي والثقة"
      }
    ]
  }
}
```

### 5.3 Answer Validation Changes

**LikertAgreement Validation:**

- Accept **only** Arabic text labels (not numeric 1-5)
- Valid answers: `["لا أوافق بشدة", "لا أوافق", "محايد", "أوافق", "أوافق بشدة"]`
- Server maps to 1-5 internally
- Reject numeric input with error: `NUMERIC_NOT_ALLOWED`

**Unchanged Types:**
- MCQ, ORDERING, TIMED_NUMERIC, Frequency → No validation changes

---

## 6. Scoring Engine Changes

### 6.1 Current Scoring (Legacy)

**File:** `Services/Scoring/ScoringService.cs`

**Current Flow:**
1. Score each item by type (MCQ, Likert, Text, etc.)
2. Aggregate by `DimensionTags` (simple grouping)
3. Compute raw scores, Z-scores (placeholder), T-scores (50 + 10*Z)
4. No banding, no reverse scoring

### 6.2 New SDJ Scoring Engine

**File:** `Services/Scoring/SdjScoringService.cs` (new)

**SDJ Flow:**
1. **Item Scoring:**
   - LikertAgreement: `raw = value` (1-5)
   - If `Reverse == true`: `score = 6 - raw`
   - Other types: unchanged logic

2. **Sub-Dimension Aggregation:**
   - Group items by `SubDimension`
   - Compute mean raw score per sub-dimension

3. **Dimension Aggregation:**
   - Group sub-dimensions by parent `Dimension`
   - Compute weighted mean (equal weighting for MVP)

4. **T-Score Transformation:**
   - **Constants:** Mean = 50, SD = 10 (population norms)
   - **Formula:** `T = 50 + 10 * ((raw - mean) / sd)`
   - **Clamp:** [20, 80] range for display safety

5. **Banding:**
   - **Weak:** T < 40 (Red)
   - **Average:** 40 ≤ T < 55 (Orange)
   - **Excellent:** T ≥ 55 (Green)

6. **Track Mapping:**
   - Analyze dimension profile
   - Map to 3 SDJ tracks with fit scores:
     - **مسار التميز الذاتي** (Self-Excellence Track)
     - **مسار العلاقات المهنية** (Professional Relationships Track)
     - **مسار النجاح الوظيفي** (Career Success Track)
   - Based on strongest dimensions and gaps

### 6.3 Scoring Interface

```csharp
public interface ISdjScoringService
{
    Task<SdjScoreSummary> ComputeSdjScores(int sessionId);
}

public class SdjScoreSummary
{
    public List<SdjDimensionScore> Dimensions { get; set; }
    public List<SdjSubDimensionScore> SubDimensions { get; set; }
    public SdjTotalScore TotalScore { get; set; }
    public List<SdjTrackFit> TrackFits { get; set; }
    public string Version { get; set; } = "SDJ_v1.0";
}

public class SdjDimensionScore
{
    public string Dimension { get; set; }
    public double Raw { get; set; }
    public double T { get; set; }
    public double Percentile { get; set; }
    public string Band { get; set; } // "Weak", "Average", "Excellent"
    public List<SdjSubDimensionScore> SubDimensions { get; set; }
}

public class SdjSubDimensionScore
{
    public string SubDimension { get; set; }
    public double Raw { get; set; }
    public double T { get; set; }
    public double Percentile { get; set; }
    public string Band { get; set; }
}

public class SdjTrackFit
{
    public string TrackNameAr { get; set; }
    public string FitLevel { get; set; } // "high", "medium", "low"
    public string ReasoningAr { get; set; }
    public List<string> KeyCompetencies { get; set; }
}
```

---

## 7. Frontend Changes

### 7.1 User UI (frontend/user-ui)

**Affected Components:**

1. **Likert.tsx (existing)**
   - Already renders 1-5 Arabic scale correctly
   - **No changes needed** (validates against current logic)

2. **ExamNew.tsx**
   - Question fetching and answer submission
   - **No changes needed** (contract-compatible)

3. **Instructions.tsx**
   - Update instructions to mention SDJ framework
   - Add note about Likert scale usage

### 7.2 Admin UI (frontend/admin-ui)

**Affected Components:**

1. **Results List Page**
   - Add SDJ badge/indicator for SDJ-scored results
   - Show top track fit in list view

2. **Result Detail Page**
   - **SDJ Dimension Tree View:**
     - Expandable parent dimensions
     - Sub-dimension breakdown with T-scores
     - Color-coded bands (Red/Orange/Green)
   
   - **Horizontal Bar Chart:**
     - Replace basic bar → modern horizontal bar
     - Sort ascending by T-score
     - Show dimension names (Arabic) + T-score labels
     - Color by band

   - **Larger Vector Donuts:**
     - Increase size: 120px → 180px diameter
     - One donut per dimension
     - Band-colored fill
     - Arabic dimension name below

   - **SDJ Track Fit Card:**
     - Top 3 tracks with fit level
     - Reasoning in Arabic
     - Key competencies list

### 7.3 Chart Specifications

**Horizontal Bar Chart (Ascending T-Score):**
- Library: Recharts (existing)
- Axis: Y-axis (dimension names), X-axis (T-scores 0-100)
- Colors: Green (≥55), Orange (40-54.9), Red (<40)
- Labels: Arabic names, Western numerals for T-scores
- Sort: Ascending by T-score (weakest at top)

**Vector Donuts:**
- Library: Recharts PieChart with custom rendering
- Size: 180px diameter
- Segments: 3 (Weak, Average, Excellent bands)
- Center label: T-score (large, bold)
- Below: Arabic dimension name (14pt)
- No PNG exports (pure SVG)

---

## 8. PDF Report Changes

### 8.1 Current Structure (3 pages)

**Page 1:** Cover + Summary  
**Page 2:** Charts Grid  
**Page 3:** Actions & Courses

### 8.2 New SDJ Structure (3+ pages)

**Page 1: Cover & Summary**
- Centered logo: `SITES-ICON.png`
- Title: "منصة التحليل النفسي المتقدم - إطار التنمية المستدامة"
- User info grid (2×2): Name, National ID, Session ID, Date
- KPI chips: Avg T-Score, Percentile, Band distribution
- **Top 3 Strengths (SDJ dimensions)**
- **Top 3 Development Areas (SDJ dimensions)**

**Page 2: Dimension Analysis**
- **Horizontal Bar Chart (Ascending T-Score):**
  - All dimensions sorted by T-score
  - Color-coded by band
  - Larger font sizes (14pt Arabic, 12pt numbers)
  - RTL layout

- **Vector Donut Grid (2×3 or 3×2):**
  - Top 6 dimensions (3 strongest + 3 weakest)
  - Large donuts (180px)
  - Band-colored segments
  - Arabic labels

**Page 3: SDJ Profile**
- **Dimension Tree Breakdown:**
  - Parent dimension → Sub-dimensions with T-scores
  - Table format with band colors

- **Track Fit Analysis:**
  - Top 3 SDJ tracks with fit level
  - Arabic reasoning for each
  - Key competencies list

**Page 4: Action Plan** (if needed)
- **Development Recommendations:**
  - 2-3 recommendations per weakest sub-dimension
  - Arabic text, bullet format
  - No AI-generated content (templated rules)

- **Suggested Courses:**
  - Course name (Arabic)
  - Description
  - Target sub-dimension

**Page 5: Methodology** (if needed)
- **Likert Scale Explanation**
- **T-Score Banding**
- **Reverse Scoring Concept**
- **SDJ Framework Overview**
- Arabic only, Fus'ha

### 8.3 Technical Requirements

- **HarfBuzz Arabic Shaping:** Enabled (already working)
- **Font:** Amiri or Noto Naskh Arabic
- **Numbers:** Western digits (0-9)
- **Charts:** Pure vector (QuestPDF SkiaSharp), no PNG
- **Page Count:** Minimum 3, expandable to 5 with appendices
- **File Size:** Target <500KB

---

## 9. Testing & Acceptance Criteria

### 9.1 Unit Tests

**Test File:** `backend/PsyApi.Tests/SdjScoringServiceTests.cs`

**Test Cases:**
1. ✅ Reverse scoring: Input 5 → Output 1
2. ✅ Sub-dimension aggregation: Mean of 3 items
3. ✅ T-score calculation: Correct formula
4. ✅ Banding: T=39 → Weak, T=40 → Average, T=55 → Excellent
5. ✅ Track mapping: Profile → 3 tracks with fit

### 9.2 API Integration Tests

**Test File:** `backend/PsyApi.Tests/SdjApiTests.cs`

**Test Cases:**
1. ✅ `/sessions/{id}/next` returns SDJ fields
2. ✅ Answer submission rejects numeric Likert input
3. ✅ `/results/{id}/birkman` includes `sdjProfile`
4. ✅ Legacy clients still work (no 400/500 errors)

### 9.3 Visual Tests (Playwright/Cypress)

**Test File:** `frontend/user-ui/tests/exam-sdj.spec.ts`

**Test Cases:**
1. ✅ Likert item renders with 5 Arabic options
2. ✅ Timer countdown visible and accurate
3. ✅ Session cap at 1 hour enforced
4. ✅ Admin result page shows SDJ tree
5. ✅ Horizontal bar chart displays correctly (RTL)
6. ✅ Vector donuts render without PNG artifacts

### 9.4 PDF Smoke Tests

**Test File:** `backend/PsyApi.Tests/SdjPdfTests.cs`

**Test Cases:**
1. ✅ PDF generates 3+ pages
2. ✅ Logo centered on page 1
3. ✅ No � glyph errors (HarfBuzz validation)
4. ✅ Charts are vector (SkiaSharp SVG paths)
5. ✅ Arabic text renders correctly (visual inspection)

### 9.5 Acceptance Checklist

- [ ] **CSV Validation:** questions_sdj_ar.csv passes linter (no empty fields, correct anchors)
- [ ] **DB Migration:** EF migration applies without errors
- [ ] **Seeding:** 100+ SDJ items load successfully
- [ ] **Scoring:** Reverse scoring works, T-scores computed correctly
- [ ] **API:** All endpoints return SDJ fields, legacy clients unaffected
- [ ] **User Exam:** Likert renders correctly, timer works, 1-hour cap enforced
- [ ] **Admin UI:** SDJ tree visible, charts modern, no console errors
- [ ] **PDF:** 3+ pages, logo centered, no glyphs errors, vector charts, Arabic shaping OK
- [ ] **Tests:** All unit/API/visual tests pass (green)
- [ ] **Rollback:** Feature flag `USE_SDJ=0` restores legacy behavior

---

## 10. Risk Mitigation & Rollback

### 10.1 Feature Flag Strategy

**Environment Variable:** `USE_SDJ` (boolean)

**Behavior:**
- `USE_SDJ=0` (default): Load legacy CSV, use legacy scoring
- `USE_SDJ=1`: Load SDJ CSV, use SDJ scoring

**Implementation:**
```csharp
// In DataSeeder.cs
var useSdj = Environment.GetEnvironmentVariable("USE_SDJ") == "1";
var csvPath = useSdj 
    ? "Resources/Questions/questions_sdj_ar.csv"
    : "Resources/Questions/questions_fixed_extended_plus_personality.csv";
```

### 10.2 Rollback Plan

**If SDJ causes issues in production:**

1. **Stop Services:**
   ```bash
   docker-compose down
   ```

2. **Set Rollback Flag:**
   ```bash
   export USE_SDJ=0
   ```

3. **Restart Services:**
   ```bash
   docker-compose up -d
   ```

4. **Verify Legacy Behavior:**
   - Check question types (should see TEXT items)
   - Check scoring (should use legacy dimensions)
   - Check PDF (should use old format)

### 10.3 Data Safety

**Database:**
- New fields (`Dimension`, `SubDimension`, `Reverse`) are nullable
- Legacy data remains intact
- No destructive migrations (additive only)

**CSV Files:**
- Keep both CSVs in repo:
  - `questions_fixed_extended_plus_personality.csv` (legacy)
  - `questions_sdj_ar.csv` (new)
- Git-tracked for version control

---

## 11. Implementation Phases

### Phase A: Discovery & Planning ✅ (Current)
- [x] Map current architecture
- [x] Define SDJ dimension tree
- [x] Document API contracts
- [x] Create migration plan

### Phase B: Content & Schema (Week 1)
- [ ] Transform CSV (TEXT → Likert)
- [ ] Validate SDJ question bank
- [ ] Generate EF migration
- [ ] Update seeder with feature flag

### Phase C: Backend Scoring & Validation (Week 1-2)
- [ ] Implement `SdjScoringService`
- [ ] Add reverse scoring logic
- [ ] Implement T-score banding
- [ ] Add track mapping rules
- [ ] Update answer validation
- [ ] Extend API responses (non-breaking)

### Phase D: User UI (Week 2)
- [ ] Update instructions page
- [ ] Verify Likert rendering
- [ ] Test timer and session cap

### Phase E: Admin UI (Week 2-3)
- [ ] Add SDJ dimension tree view
- [ ] Implement horizontal bar chart
- [ ] Enlarge vector donuts
- [ ] Add track fit card

### Phase F: PDF Report (Week 3)
- [ ] Redesign page 1 (cover)
- [ ] Update page 2 (charts)
- [ ] Add page 3 (SDJ profile)
- [ ] Add page 4-5 (action plan + methodology)
- [ ] Test HarfBuzz rendering

### Phase G: Testing & QA (Week 4)
- [ ] Write unit tests
- [ ] Write API integration tests
- [ ] Write visual tests (Playwright)
- [ ] PDF smoke tests
- [ ] Manual QA (Arabic correctness)

### Phase H: Documentation & Handover (Week 4)
- [ ] Create CHANGELOG_SDJ.md
- [ ] Write operator guide
- [ ] Generate sample PDFs
- [ ] Record demo video

---

## 12. Operational Guide

### 12.1 Switching to SDJ

**Prerequisites:**
1. Backup database: `pg_dump` or SQLite backup
2. Deploy code with SDJ feature
3. Verify CSV file exists: `questions_sdj_ar.csv`

**Steps:**
```bash
# 1. Set environment variable
export USE_SDJ=1

# 2. Clear old items (optional, if force re-seed)
dotnet run --reseed

# 3. Restart backend
dotnet run

# 4. Test with one session
curl -X POST http://localhost:5000/api/sessions/start \
  -H "Content-Type: application/json" \
  -d '{"nationalId":"1000000001"}'

# 5. Verify SDJ fields in response
curl http://localhost:5000/api/sessions/{sessionId}/next
```

### 12.2 Monitoring

**Log Indicators:**
- `[SDJ] Seeding 100+ items from questions_sdj_ar.csv`
- `[SDJ] Scoring session {id} with reverse scoring enabled`
- `[SDJ] Track mapping: User profile → 3 tracks`
- `[SDJ] PDF report: 3+ pages with SDJ sections`

**Metrics:**
- Average session duration (should be ~50-60 min for 80 items)
- Likert answer distribution (should not have numeric values)
- PDF generation time (<5 seconds)

### 12.3 Troubleshooting

**Issue:** Questions not loading  
**Fix:** Check `USE_SDJ` flag, verify CSV path

**Issue:** Arabic text rendering incorrectly  
**Fix:** Ensure HarfBuzz enabled, check font registration

**Issue:** T-scores showing as NaN  
**Fix:** Verify dimension grouping, check for empty sub-dimensions

**Issue:** PDF generation fails  
**Fix:** Check QuestPDF license, verify SkiaSharp version

---

## 13. Dependencies & Versions

### Backend (.NET 8.0)
- Entity Framework Core 8.x
- QuestPDF 2024.x (Community License)
- SkiaSharp 2.88.x
- Serilog 3.x

### Frontend (React 18)
- Vite 5.x
- Recharts 2.x
- Tailwind CSS 3.x
- TypeScript 5.x

### Database
- SQLite 3.x (dev/offline)
- PostgreSQL 14+ (production, optional)

---

## 14. Success Criteria

### Technical
- ✅ 100% API backward compatibility
- ✅ Zero breaking changes for existing clients
- ✅ All tests pass (unit, integration, visual)
- ✅ PDF renders correctly (no � glyphs)
- ✅ Feature flag rollback works

### Content
- ✅ 100+ SDJ items covering all 5 parent dimensions
- ✅ Each sub-dimension has ≥3 items
- ✅ Reverse-scored items marked correctly
- ✅ Arabic text follows Fus'ha standards

### User Experience
- ✅ Likert items render clearly (RTL, 5 options)
- ✅ Timer visible and accurate
- ✅ Session cap enforced (1 hour)
- ✅ Admin UI shows SDJ tree and charts
- ✅ PDF report professional and readable

---

## 15. Next Steps

1. **Review this plan** with stakeholders
2. **Approve SDJ dimension tree** (confirm translations)
3. **Begin Phase B:** CSV transformation
4. **Set up feature flag** in environment
5. **Create Git branch:** `feature/sdj-migration`

---

## Appendix A: SDJ Track Descriptions

### 1. مسار التميز الذاتي (Self-Excellence Track)
**Target Audience:** Individuals seeking personal growth and self-awareness  
**Focus Areas:** Self-Awareness, Self-Confidence, Continuous Learning  
**Outcomes:** Enhanced self-understanding, improved decision-making, resilience

### 2. مسار العلاقات المهنية (Professional Relationships Track)
**Target Audience:** Professionals in team-based roles  
**Focus Areas:** Emotional Intelligence, Communication, Collaboration  
**Outcomes:** Stronger relationships, effective teamwork, conflict resolution skills

### 3. مسار النجاح الوظيفي (Career Success Track)
**Target Audience:** Aspiring leaders and career-focused individuals  
**Focus Areas:** Leadership, Problem Solving, Strategic Planning  
**Outcomes:** Career advancement, innovation capability, strategic thinking

---

## Appendix B: Sample Questions

### Parent Dimension: التميز الذاتي (Self-Excellence)
**Sub-Dimension:** الوعي الذاتي (Self-Awareness)

1. أستطيع تحديد نقاط قوتي بدقة. (I can accurately identify my strengths)
2. أدرك تأثير سلوكياتي على الآخرين. (I am aware of how my behaviors affect others)
3. أراجع أدائي باستمرار لتحسين نفسي. (I regularly review my performance to improve myself)

**Sub-Dimension:** الثقة بالنفس (Self-Confidence)

1. أثق بقدرتي على تحقيق أهدافي. (I trust my ability to achieve my goals)
2. أشعر بالراحة عند التحدث أمام الجمهور. (Reverse: I feel uncomfortable speaking in public)
3. أتخذ قراراتي بثقة دون تردد مفرط. (I make decisions confidently without excessive hesitation)

---

## Appendix C: Glossary

| Term | Arabic | Definition |
|------|--------|------------|
| Dimension | بُعد | Parent category in SDJ framework |
| Sub-Dimension | بُعد فرعي | Child category under a parent dimension |
| Likert Scale | مقياس ليكرت | 1-5 agreement scale |
| Reverse Scoring | التصحيح العكسي | Inverting scores for negatively-worded items |
| T-Score | درجة تي | Standardized score (mean=50, SD=10) |
| Banding | النطاق | Categorizing T-scores into Weak/Average/Excellent |
| Track | مسار | Development journey aligned with SDJ competencies |
| Fus'ha | الفصحى | Modern Standard Arabic |

---

## Contact & Support

**Technical Lead:** Development Team  
**Content Lead:** Psychology Team  
**QA Lead:** Testing Team  

**Documentation:** This file (`README_SDJO_MIGRATION.md`)  
**Issue Tracker:** GitHub Issues (tag: `sdj-migration`)  
**Status Dashboard:** To be created in Phase B

---

**End of Migration Plan**
