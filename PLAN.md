# SDJ V2 Seven Patterns Migration Plan

## Executive Summary

**Migration**: SDJ v1 (existing dimension/subdimension structure) → SDJ V2 (7 authoritative patterns with specific sub-dimensions)

**Scope**: Complete question bank regeneration, scoring engine refactor, UI updates, and PDF report redesign

**Timeline**: 12 major phases covering backend, frontend, testing, and documentation

**Risk Level**: MEDIUM - Feature flag rollback available, no schema drops, Neon DB compatibility maintained

---

## Current State Analysis

### 1. Existing Pipeline Flow

```
┌─────────────────┐
│  CSV Questions  │ questions_sdj_ar.csv (120 items)
└────────┬────────┘
         │ DataSeeder.cs (USE_SDJ flag)
         ▼
┌─────────────────┐
│   Items Table   │ Columns: Dimension, SubDimension, Reverse
└────────┬────────┘
         │ SessionsController.cs
         ▼
┌─────────────────┐
│ User Takes Test │ 60 min timer, Likert 1-5
└────────┬────────┘
         │ Submit answers
         ▼
┌─────────────────┐
│ SdjScoringService│ T-score calculation, reverse handling
└────────┬────────┘
         │ Generate Result
         ▼
┌─────────────────┐
│  Result JSON    │ DimensionScoresJson + sdjData block
└────────┬────────┘
         │
    ┌────┴────┐
    ▼         ▼
┌────────┐ ┌──────────────────────┐
│Admin UI│ │ModernSdj...Report.cs │
│Charts  │ │(PDF with charts)     │
└────────┘ └──────────────────────┘
```

### 2. Current Dimensions (V1)

**Existing in questions_sdj_ar.csv:**
- التميز الذاتي (6 sub-dimensions: الوعي الذاتي, الثقة بالنفس, التنظيم الذاتي, التعلم المستمر, المرونة النفسية)
- التواصل والعلاقات (5 sub-dimensions: الذكاء العاطفي, التواصل الفعال, التعاون, حل النزاعات, بناء العلاقات)
- النجاح المهني (5 sub-dimensions: القيادة, حل المشكلات, الإبداع والابتكار, إدارة الوقت, التخطيط الاستراتيجي)
- المسؤولية الاجتماعية (5 sub-dimensions: الوعي المجتمعي, الأخلاق المهنية, الاستدامة, العمل التطوعي, المواطنة الفاعلة)
- الصحة والتوازن (4 sub-dimensions: الصحة النفسية, الصحة الجسدية, إدارة الضغوط, التوازن بين العمل والحياة)

**Total:** 5 main dimensions, 25 sub-dimensions, 120 items (4.8 items/subdimension average)

### 3. Target Structure (V2)

**7 Major Patterns (as specified in requirements):**
1. **P1 - الأنماط الشخصية**: 3 subs (MBTI, Big Five, Learning Style) = 30 items
2. **P2 - القدرات المعرفية والعقلية**: 4 subs (Multiple Intelligences, Memory, Attention, Creativity) = 40 items
3. **P3 - الأنماط النفسية**: 4 subs (Stress, Anxiety, Resilience, Emotional Intelligence) = 40 items
4. **P4 - الأنماط السلوكية**: 3 subs (Adaptation, Influence/Leadership, Anger Control) = 30 items
5. **P5 - الأنماط العددية والمنطقية**: 3 subs (Arithmetic, Deduction, Correlation) = 30 items
6. **P6 - الأنماط القيادية والتنظيمية**: 3 subs (Decision Making, Delegation, Reward/Accountability) = 30 items
7. **P7 - الاستعدادات المهنية العامة**: 1 sub (Complex Work Situations) = 10 items

**Total:** 7 patterns, 21 sub-dimensions, **210 items** (10 items/subdimension, 50% reversed)

---

## Migration Strategy

### Phase 0: Schema Definition (Day 1)

**File**: `backend/PsyApi/Domain/SdjV2SevenPatterns.json`

```json
{
  "version": "sdj-v2-seven-patterns",
  "main_patterns": [...7 patterns with IDs, keys, Arabic names...],
  "items_per_sub_dimension": 10,
  "reverse_ratio": 0.5,
  "likert_scale": {"min":1, "max":5, "labels_ar":[...]}
}
```

**Status**: Single source of truth, no code generation yet

---

### Phase 1: Question Generation (Day 1-2)

**Tools to Create:**

1. **tools/generate_sdj_v2_csv.ts** (TypeScript/Node.js)
   - Loads `SdjV2SevenPatterns.json`
   - For each sub-dimension: generates 10 items (5 direct, 5 reverse)
   - Uses Arabic phrasing patterns from existing CSV as style guide
   - Output: `backend/PsyApi/Resources/Questions/questions_sdj_v2_ar.csv`

2. **tools/validate_csv.ts**
   - Asserts: 210 total items
   - Asserts: Exactly 10 items per sub-dimension
   - Asserts: Exactly 50% reverse=true per sub-dimension
   - Asserts: UTF-8 encoding, no invalid glyphs
   - Exit code 1 if validation fails

**CSV Columns:**
```
ItemCode,TextAr,PatternId,PatternKey,PatternNameAr,SubId,SubKey,SubNameAr,Type,Reverse,TimeLimitSeconds,Weight
```

**Risk**: Generated Arabic text quality → Mitigation: Manual review of generated items before commit

---

### Phase 2: Database Migration (Day 2)

**New Migration**: `20251029_SDJ_V2_SevenPatterns.cs`

**Schema Changes:**
- Add columns to `Items` table:
  - `PatternId` (varchar(10), nullable) - e.g., "P1"
  - `PatternKey` (varchar(50), nullable) - e.g., "personality_patterns"
  - `SubId` (varchar(10), nullable) - e.g., "P1_S1"
  - `SubKey` (varchar(50), nullable) - e.g., "mbti"
  - (Existing: `Dimension`, `SubDimension`, `Reverse` - keep for backward compatibility)

**Indexes:**
```sql
CREATE INDEX IX_Items_PatternId ON Items(PatternId);
CREATE INDEX IX_Items_SubId ON Items(SubId);
```

**Backward Compatibility:**
- NO drops of existing columns
- Nullable columns allow legacy data to coexist
- Migration applies cleanly to Neon Postgres

**DataSeeder Update:**
- When `USE_SDJ=1` AND CSV filename matches `questions_sdj_v2_ar.csv` → populate new columns
- Log: "SDJ V2 mode: {PatternCount} patterns, {SubCount} sub-dimensions"

---

### Phase 3: Scoring Engine (Day 2-3)

**New Service**: `backend/PsyApi/Services/Scoring/SdjV2ScoringService.cs`

**Interface:**
```csharp
public interface ISdjV2ScoringService
{
    Task<SdjV2ScoreSummary> ComputeScoresAsync(int sessionId);
}
```

**Logic:**
1. **Fetch answers**: Filter items where `PatternId IS NOT NULL`
2. **Score items**: 
   - Direct: score = likertValue (1-5)
   - Reverse: score = 6 - likertValue
3. **Aggregate sub-dimensions**:
   - Group by `SubId`
   - Compute mean raw score
   - Transform to T-score: `T = 50 + 10 * ((raw - 3.0) / 0.8)`
   - Percentile: approximate from T-score distribution
   - Band: <40 "ضعيف", 40-54.9 "متوسط", ≥55 "ممتاز"
4. **Aggregate patterns**:
   - Group sub-dimensions by `PatternId`
   - Compute weighted mean of T-scores (equal weights)
   - Same banding logic
5. **Top 3 / Need Growth**:
   - Top 3: Highest pattern T-scores
   - Need Growth: Patterns with T < 40 or lowest 2 if all ≥40

**Output DTO:**
```csharp
public class SdjV2ScoreSummary
{
    public List<PatternScore> SevenPatterns { get; set; }
    public List<SubDimensionScore> SubDimensions { get; set; }
    public Summary Summary { get; set; } // top3, needGrowth
    public string Version { get; set; } = "SDJ_v2.1_7Patterns";
}
```

**Controller Integration:**
- `SessionsController.SubmitSession()`: 
  - If `USE_SDJ=1` AND session has items with `PatternId`:
    - Use `SdjV2ScoringService`
  - Else if `USE_SDJ=1`:
    - Use `SdjScoringService` (fallback to v1)
  - Else:
    - Use legacy `ScoringService`

---

### Phase 4: API Contracts (Day 3)

**DTOs to Update:**

1. **ResultDetailResponse** (existing DTO):
   ```csharp
   public class ResultDetailResponse
   {
       // Existing fields...
       public SdjV2ScoreSummary? SevenPatternsData { get; set; } // NEW
       public SdjScoreSummary? SdjData { get; set; } // KEEP for v1 compatibility
   }
   ```

2. **AdminAnalyticsController**:
   - `GET /api/admin/analytics/distribution`:
     - Detect `SevenPatternsData` presence
     - Compute distribution from 7 patterns instead of old dimensions
     - Fix JSON deserialization errors by matching DTO structure

3. **HealthController** (or Program.cs health endpoint):
   ```csharp
   app.MapGet("/api/health", () => new {
       status = "healthy",
       sdjMode = Environment.GetEnvironmentVariable("USE_SDJ") == "1" 
           ? "v2-seven-patterns" 
           : "legacy"
   });
   ```

---

### Phase 5: User UI (Day 3-4)

**Affected Components:**
- `frontend/user-ui/src/pages/TestSession.tsx`
- `frontend/user-ui/src/components/test/LikertQuestion.tsx`

**Changes:**
1. **No major changes needed** - existing Likert component already sends numeric 1-5
2. **Verify**: Timer logic remains 60 minutes total
3. **Verify**: Auto-submit on timeout works
4. **Test**: Load 210 items (vs 120) - pagination/scrolling handles this
5. **Arabic**: Ensure all new question text renders correctly (RTL, Noto Sans Arabic font)

**No Dark Mode**: Already disabled per project standards

---

### Phase 6: Admin UI (Day 4)

**Files to Update:**

1. **frontend/admin-ui/src/lib/adminContract.ts**:
   ```typescript
   export interface ResultDetail {
       // Existing...
       sevenPatternsData?: SevenPatternsData; // NEW
       sdjData?: SdjData; // KEEP
   }
   
   export interface SevenPatternsData {
       sevenPatterns: PatternScore[];
       subDimensions: SubDimensionScore[];
       summary: { top3: string[]; needGrowth: string[] };
       version: string;
   }
   ```

2. **frontend/admin-ui/src/pages/results/ResultDetail.tsx**:
   - Check `sevenPatternsData` presence
   - Display badge: "SDJ v2 — الأنماط السباعية"
   - **Charts**:
     - **Horizontal Bar**: All 21 sub-dimensions sorted by T-score (ascending)
     - **Heptagon Radar**: 7 main patterns with T-scores (0-100 scale)
   - Use existing chart library (recharts or SkiaSharp on backend)
   - Ensure Arabic labels render correctly

3. **frontend/admin-ui/src/pages/analytics/Analytics.tsx**:
   - `GET /api/admin/analytics/distribution`:
     - Parse `sevenPatternsData` structure
     - Show "توزيع الدرجات" by pattern (not old dimensions)
     - Bar chart: 7 patterns with average T-scores across all users

---

### Phase 7: PDF Report (Day 4-5)

**Service**: `backend/PsyApi/Services/Reports/ModernSdjSevenPatternReportService.cs`

**Changes:**

1. **Header**:
   - Replace logo with `STEST.png` (centered, 100x100)
   - Title: "التقرير النفسي الشامل — نتائج القياس والتحليل"
   - Subtitle: "تحليل الأنماط السباعية للتنمية المستدامة"

2. **Participant Section**:
   - Right-aligned labels: "الاسم:", "الهوية:", "تاريخ الاختبار:"
   - Left-aligned values within RTL grid
   - Use QuestPDF Row/Column with RTL direction

3. **Charts** (using SkiaSharp + HarfBuzz):
   - **Horizontal Bar Chart**: 21 sub-dimensions (sorted by T-score)
     - X-axis: 0-100 (T-scores)
     - Y-axis: Arabic sub-dimension names (shaped with HarfBuzz)
     - Color coding: Red (<40), Yellow (40-54.9), Green (≥55)
   - **Heptagon Radar Chart**: 7 main patterns
     - Vertices: 7 pattern names in Arabic
     - Data line: T-scores connected
     - Filled polygon with transparency

4. **New Section: ملخص الأنماط السباعية**:
   ```
   ┌─────────────────────────────────────┐
   │ ملخص الأنماط السباعية               │
   ├─────────────────────────────────────┤
   │ 1. الأنماط الشخصية                 │
   │    • T-Score: 58.2 (ممتاز)          │
   │    • MBTI: 56.1                     │
   │    • Big Five: 59.3                 │
   │    • نمط التعلم: 59.2               │
   │                                     │
   │ 2. القدرات المعرفية والعقلية...    │
   └─────────────────────────────────────┘
   ```

5. **New Section: الدورات المقترحة**:
   - Table format:
   ```
   | النمط | البعد الفرعي | الدورة المقترحة | المدة |
   |------|-------------|-----------------|-------|
   | P1   | MBTI        | فهم الشخصية    | 16 ساعة |
   ```
   - Use `CourseRecommendationsMapper` (existing utility)
   - Filter weak sub-dimensions (T < 40)

6. **Closing Section**:
   - Arabic text:
     ```
     ابدأ رحلتك التدريبية المخصصة عبر منصة استدامة
     
     للمزيد من المعلومات، تواصل معنا:
     📧 info@example.com
     📞 +966 XX XXX XXXX
     ```

7. **Font & Rendering**:
   - All Arabic text uses Noto Naskh Arabic
   - HarfBuzz shaping enabled for all TextBlock calls
   - No `()` parentheses in Arabic context (causes symbol issues)
   - Test: Generate sample PDF, ensure no � glyphs

---

### Phase 8: Testing (Day 5-6)

**Unit Tests** (`backend.Tests/`):

1. **CSV Generator Tests**:
   ```csharp
   [Fact]
   public void GenerateSdjV2Csv_Produces210Items()
   [Fact]
   public void Validate_ExactlyTenItemsPerSubDimension()
   [Fact]
   public void Validate_Exactly50PercentReversed()
   ```

2. **Scoring Tests**:
   ```csharp
   [Fact]
   public void ScoreLikertItem_DirectScoring_ReturnsRawValue()
   [Fact]
   public void ScoreLikertItem_ReverseScoring_Returns6MinusValue()
   [Fact]
   public void ComputeTScore_MeanValue_Returns50()
   [Fact]
   public void AggregateSubDimension_TenItems_ComputesCorrectMean()
   [Fact]
   public void AggregatePattern_ThreeSubs_WeightedAverage()
   [Fact]
   public void BandLogic_Below40_ReturnsWeak()
   ```

3. **Integration Tests**:
   ```csharp
   [Fact]
   public async Task E2E_StartSession_Answer_Submit_GetResult_V2()
   {
       // 1. Seed 210 items
       // 2. Start session
       // 3. Submit 210 answers
       // 4. Fetch result
       // 5. Assert SevenPatternsData present with 7 patterns
   }
   ```

4. **PDF Smoke Test**:
   ```csharp
   [Fact]
   public async Task Pdf_Generate_NoExceptions_ArabicTextPresent()
   {
       var pdf = await service.RenderSdjSevenPatternPdfAsync(...);
       Assert.NotNull(pdf);
       Assert.True(pdf.Length > 50000); // >50KB
       // TODO: OCR check for Arabic text (optional)
   }
   ```

**Frontend Tests** (optional but recommended):
- Cypress E2E: Load test, answer questions, verify result page shows 7 patterns

---

### Phase 9: Rollback & Safety (Day 6)

**scripts/rollback_to_legacy.ps1**:
```powershell
# 1. Set USE_SDJ=0 in .env
# 2. Restart backend
# 3. Force reseed with legacy CSV
# 4. Clear Redis cache (if any)

Write-Host "Rolling back to legacy mode..."
$env:USE_SDJ="0"
dotnet run --project backend/PsyApi -- --force-reseed
Write-Host "Rollback complete. Legacy mode active."
```

**CHANGELOG.md** entry:
```markdown
## [2.1.0] - 2025-10-29 - SDJ V2 Seven Patterns

### Added
- 7-pattern question bank (210 items, 21 sub-dimensions)
- SdjV2ScoringService with enhanced T-score aggregation
- Heptagon radar chart for 7 patterns
- "ملخص الأنماط السباعية" in PDF report
- Course recommendations table

### Changed
- USE_SDJ=1 now activates SDJ V2 (210 items) by default
- PDF logo updated to STEST.png
- Admin analytics uses 7-pattern structure

### Migration Notes
- Run migration: `dotnet ef database update`
- Rollback: Set USE_SDJ=0 and restart
```

---

### Phase 10: Environment & Deployment (Day 6-7)

**Environment Variables:**
- `USE_SDJ=1` (required for V2)
- `DATABASE_URL` (existing - Neon Postgres)
- `VITE_API_BASE_URL` (frontend)

**Deployment Checklist:**

1. **Backend (Render)**:
   - Push to `develop` branch
   - Auto-deploy triggers
   - Set `USE_SDJ=1` in Render dashboard
   - Run migration via Render shell: `dotnet ef database update`
   - Monitor logs for "SDJ V2 mode: 7 patterns"

2. **Frontend (Vercel)**:
   - Push to `develop`
   - Auto-deploy triggers
   - No env changes needed (API-driven)

3. **Database (Neon)**:
   - Migration auto-applies on backend startup
   - No manual SQL needed (EF handles it)

**No Schema Drops**: All changes are additive (nullable columns)

---

## File Touch-Points Summary

### Backend (C#)
| File | Change Type | Description |
|------|-------------|-------------|
| `Domain/SdjV2SevenPatterns.json` | **NEW** | Authoritative schema |
| `Migrations/20251029_SDJ_V2_SevenPatterns.cs` | **NEW** | EF migration |
| `Models/Item.cs` | **MODIFY** | Add PatternId, SubId columns |
| `Services/DataSeeder.cs` | **MODIFY** | Parse new CSV columns |
| `Services/Scoring/SdjV2ScoringService.cs` | **NEW** | V2 scoring logic |
| `Services/Scoring/ISdjV2ScoringService.cs` | **NEW** | Interface |
| `Services/Reports/ModernSdjSevenPatternReportService.cs` | **MODIFY** | Update charts, sections |
| `Controllers/SessionsController.cs` | **MODIFY** | Wire V2 scoring |
| `Controllers/ResultsController.cs` | **MODIFY** | Return SevenPatternsData |
| `Controllers/AdminAnalyticsController.cs` | **MODIFY** | V2 distribution logic |
| `Program.cs` | **MODIFY** | Register ISdjV2ScoringService |

### Frontend (React/TypeScript)
| File | Change Type | Description |
|------|-------------|-------------|
| `admin-ui/src/lib/adminContract.ts` | **MODIFY** | Add SevenPatternsData interface |
| `admin-ui/src/pages/results/ResultDetail.tsx` | **MODIFY** | Display 7 patterns badge, charts |
| `admin-ui/src/pages/analytics/Analytics.tsx` | **MODIFY** | Parse V2 distribution |
| `user-ui/src/pages/TestSession.tsx` | **VERIFY** | No changes needed (test 210 items) |

### Tools (TypeScript/Node.js)
| File | Change Type | Description |
|------|-------------|-------------|
| `tools/generate_sdj_v2_csv.ts` | **NEW** | CSV generator |
| `tools/validate_csv.ts` | **NEW** | CSV validator |

### Data
| File | Change Type | Description |
|------|-------------|-------------|
| `backend/PsyApi/Resources/Questions/questions_sdj_v2_ar.csv` | **NEW** | 210 items (generated) |
| `seed/questions_sdj_v2_ar.csv` | **NEW** | Source copy |

### Scripts
| File | Change Type | Description |
|------|-------------|-------------|
| `scripts/rollback_to_legacy.ps1` | **NEW** | Rollback script |

### Documentation
| File | Change Type | Description |
|------|-------------|-------------|
| `PLAN.md` | **NEW** | This file |
| `CHANGELOG.md` | **MODIFY** | Add v2.1.0 entry |
| `POST_IMPLEMENTATION_REPORT.md` | **NEW** | Final summary |

---

## Risk Assessment

### HIGH RISKS
1. **Generated Arabic Quality**
   - **Mitigation**: Manual review of all 210 items before commit
   - **Fallback**: Edit CSV manually if generator produces poor text

2. **T-Score Drift**
   - **Risk**: New 7-pattern structure changes user scores dramatically
   - **Mitigation**: Keep μ=3.0, σ=0.8 consistent; test with sample data

3. **PDF Rendering Symbols**
   - **Risk**: New Arabic labels show as � or ���
   - **Mitigation**: Test HarfBuzz shaping; avoid English () in Arabic context

### MEDIUM RISKS
4. **Database Migration in Prod**
   - **Risk**: Migration fails on Neon Postgres
   - **Mitigation**: Test migration on staging DB first; nullable columns = non-breaking

5. **Frontend Chart Library**
   - **Risk**: Heptagon radar chart not supported
   - **Mitigation**: Use SkiaSharp on backend (PDF already does this)

### LOW RISKS
6. **Performance with 210 Items**
   - **Risk**: Test session too long
   - **Mitigation**: Keep 60-min timer; pagination already handles large item sets

7. **Backward Compatibility**
   - **Risk**: Old results break
   - **Mitigation**: Keep `sdjData` field for V1; only populate `sevenPatternsData` for V2

---

## Success Criteria

### Functional
✅ Every sub-dimension has exactly 10 items (5 direct, 5 reverse)  
✅ Result payload includes 7 main patterns + 21 sub-dimensions  
✅ Admin UI displays "SDJ v2" badge and 7-pattern charts  
✅ PDF renders with STEST.png logo, Arabic text, no symbols  
✅ All unit tests pass (generator, scoring, integration)  
✅ Legacy mode (USE_SDJ=0) still works unchanged  

### Non-Functional
✅ Backend builds with 0 errors  
✅ Frontend builds with 0 errors  
✅ PDF generation <2 seconds for typical result  
✅ No secrets leaked in code or logs  
✅ Arabic text renders correctly (RTL, HarfBuzz shaping)  

### Acceptance
✅ Generate sample PDF with 7 patterns visible  
✅ Admin can see 7-pattern distribution chart  
✅ Rollback script successfully reverts to legacy  
✅ POST_IMPLEMENTATION_REPORT.md with screenshots  

---

## Timeline (Estimated)

| Day | Phase | Deliverables |
|-----|-------|--------------|
| 1 | 0-1 | Schema JSON, CSV generator, validator |
| 2 | 2-3 | Migration, DataSeeder, SdjV2ScoringService |
| 3 | 4-5 | DTOs, APIs, User UI verification |
| 4 | 6 | Admin UI charts, badges |
| 5 | 7 | PDF report updates, course recommendations |
| 6 | 8-9 | Tests, rollback script, CHANGELOG |
| 7 | 10 | Deployment, QA, POST_IMPLEMENTATION_REPORT |

**Total**: 7 days (1 week sprint)

---

## Rollback Plan

### Immediate Rollback (Within 5 Minutes)
```powershell
# 1. Set env var
$env:USE_SDJ="0"

# 2. Restart backend
docker restart psy-api

# 3. Verify health endpoint
curl http://localhost:5000/api/health
# Should show: "sdjMode": "legacy"
```

### Full Rollback (Revert All Changes)
```powershell
# 1. Checkout previous commit
git checkout HEAD~1

# 2. Rebuild and redeploy
./build.sh
git push origin develop --force

# 3. Re-run legacy migration
dotnet ef database update <previous-migration-id>
```

### Data Integrity
- NO data loss: New columns are nullable
- Old results remain accessible
- Legacy CSV still present in Resources/Questions/

---

## Constraints Compliance

✅ **Arabic RTL**: All UI and PDF text in Arabic  
✅ **No External AI**: All logic deterministic, no API calls  
✅ **Legacy Rollback**: USE_SDJ=0 instantly reverts  
✅ **Neon/Postgres**: Migration compatible, no special syntax  
✅ **No Secrets**: All credentials in .env, not in code  
✅ **HarfBuzz/Noto**: PDF uses existing font setup  

---

## Next Steps

1. **Review this PLAN.md** with stakeholders
2. **Approve schema** in `SdjV2SevenPatterns.json`
3. **Generate questions** and manually review Arabic quality
4. **Execute phases 2-10** as outlined above
5. **Deploy to staging** first, then production

---

**Document Version**: 1.0  
**Created**: 2025-10-29  
**Author**: Senior Full-Stack Team (AI)  
**Status**: READY FOR EXECUTION
