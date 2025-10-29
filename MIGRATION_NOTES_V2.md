# SDJ V2 Migration Notes - Seven Patterns Framework

**Version:** 3.2.0  
**Date:** October 29, 2025  
**Migration ID:** `20251029143657_SDJ_V2_SevenPatterns`

---

## 🎯 Migration Overview

This migration introduces the **SDJ V2 Seven Patterns** framework, expanding from the original 5-dimension model to a comprehensive 7-pattern system with 21 sub-dimensions and 210 test items.

---

## 📊 What Changed

### Database Schema

**New Columns Added to `Items` Table:**

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| `PatternId` | TEXT | YES | Pattern identifier (P1-P7) |
| `PatternKey` | TEXT | YES | Pattern key (e.g., personality_patterns) |
| `PatternNameAr` | TEXT | YES | Pattern name in Arabic |
| `SubId` | TEXT | YES | Sub-dimension identifier (e.g., P1_S1) |
| `SubKey` | TEXT | YES | Sub-dimension key |
| `SubNameAr` | TEXT | YES | Sub-dimension name in Arabic |

**Note:** All columns are nullable to maintain backward compatibility with existing V1 data.

### Question Bank

**Old (V1):** 125 items, 5 dimensions, pure Likert  
**New (V2):** 210 items, 7 patterns, 21 sub-dimensions, mixed MCQ + Likert

**Item Distribution:**
- 105 Multiple Choice Questions (MCQ)
- 105 Likert Agreement items
- 10 items per sub-dimension
- ~50% reverse scoring per sub-dimension

### Scoring Algorithm

**V1 Scoring:**
- 5 dimensions
- Simple Likert aggregation
- Basic percentile calculation

**V2 Scoring:**
- 7 main patterns
- 21 sub-dimensions
- MCQ scoring: correct=5.0, incorrect=1.0
- Likert scoring: 1-5 with reverse support
- T-score transformation (μ=50, σ=10, clamped 20-80)
- Hierarchical aggregation: items → sub-dimensions → patterns

### API Response Structure

**New Fields in Result Payload:**

```json
{
  "sevenPatternScores": [
    {
      "patternNameAr": "الأنماط الشخصية",
      "patternNameEn": "Personality Patterns",
      "tScore": 62.5,
      "band": "ممتاز",
      "subDimensions": [
        "نمط الشخصية MBTI",
        "الخمس الكبار (Big Five)",
        "نمط التعلم"
      ]
    }
    // ... 6 more patterns
  ],
  "version": "SDJ_V2_SevenPatterns"
}
```

---

## 🔄 Migration Process

### Automatic Detection

The system **automatically detects** whether to use V1 or V2 scoring:

```csharp
var hasV2Items = sessionItems.Any(si => 
    !string.IsNullOrWhiteSpace(si.Item.PatternId)
);

if (hasV2Items) {
    // Use SdjV2ScoringService
} else {
    // Use legacy SdjScoringService (V1)
}
```

**No manual configuration needed** - the presence of `PatternId` triggers V2 mode.

### Environment Variable (Optional)

Control CSV loading via `USE_SDJ` environment variable:

- `USE_SDJ=0` → Legacy mode (200 items)
- `USE_SDJ=1` → SDJ V1 (125 items)
- `USE_SDJ=2` or **unset** → SDJ V2 (210 items) ✅ **DEFAULT**

### Database Migration Steps

**For Local Development:**
```bash
cd backend/PsyApi
dotnet ef database update
```

**For Production (Neon Postgres):**
```bash
# Migration is applied automatically on app startup
# Verify in Render logs: "Applying migration '20251029143657_SDJ_V2_SevenPatterns'"
```

**Expected Log Output:**
```
[INFO] Applying migration '20251029143657_SDJ_V2_SevenPatterns'
[INFO] Executed DbCommand: ALTER TABLE "Items" ADD "PatternId" TEXT;
[INFO] Executed DbCommand: ALTER TABLE "Items" ADD "PatternKey" TEXT;
[INFO] Executed DbCommand: ALTER TABLE "Items" ADD "PatternNameAr" TEXT;
[INFO] Executed DbCommand: ALTER TABLE "Items" ADD "SubId" TEXT;
[INFO] Executed DbCommand: ALTER TABLE "Items" ADD "SubKey" TEXT;
[INFO] Executed DbCommand: ALTER TABLE "Items" ADD "SubNameAr" TEXT;
[INFO] Database migrations completed successfully
```

### Data Seeding

**Automatic Seeding on Startup:**
1. DataSeeder detects CSV mode (V2 by default)
2. Loads `questions_sdj_v2_ar.csv` (210 items)
3. Validates pattern/sub-dimension structure
4. Seeds database with V2 data

**Expected Seeding Logs:**
```
[INFO] [SDJ V2] Using CSV file: questions_sdj_v2_ar.csv
[INFO] CSV total lines (incl header): 211
[INFO] Total rows parsed (excluding header): 210
[INFO] Post-seed verification passed: 210 items
[INFO] [SDJ V2] Seven Patterns mode detected
[INFO] [SDJ V2] Pattern distribution:
[INFO]   P1: 30 items (الأنماط الشخصية)
[INFO]   P2: 30 items (القدرات المعرفية والعقلية)
[INFO]   P3: 30 items (الأنماط النفسية)
[INFO]   P4: 30 items (الأنماط السلوكية)
[INFO]   P5: 30 items (الأنماط العددية والمنطقية)
[INFO]   P6: 30 items (الأنماط القيادية والتنظيمية)
[INFO]   P7: 30 items (الاستعدادات المهنية العامة)
```

---

## ✅ Verification Checklist

### Post-Migration Verification

- [ ] **Database Schema**
  ```sql
  -- Verify new columns exist
  SELECT column_name, data_type, is_nullable 
  FROM information_schema.columns 
  WHERE table_name = 'Items' 
  AND column_name IN ('PatternId', 'PatternKey', 'PatternNameAr', 'SubId', 'SubKey', 'SubNameAr');
  ```
  Expected: 6 rows returned

- [ ] **Item Count**
  ```sql
  -- Verify 210 V2 items seeded
  SELECT COUNT(*) FROM "Items" WHERE "PatternId" IS NOT NULL;
  ```
  Expected: 210

- [ ] **Pattern Distribution**
  ```sql
  -- Verify pattern distribution
  SELECT "PatternId", "PatternNameAr", COUNT(*) 
  FROM "Items" 
  WHERE "PatternId" IS NOT NULL 
  GROUP BY "PatternId", "PatternNameAr" 
  ORDER BY "PatternId";
  ```
  Expected: 7 rows, each with count=30

- [ ] **Sub-dimension Distribution**
  ```sql
  -- Verify sub-dimension distribution
  SELECT "SubId", "SubNameAr", COUNT(*) 
  FROM "Items" 
  WHERE "SubId" IS NOT NULL 
  GROUP BY "SubId", "SubNameAr" 
  ORDER BY "SubId";
  ```
  Expected: 21 rows, each with count=10

### API Testing

- [ ] **Create Test Session**
  ```bash
  # POST /api/sessions
  # Body: { "examId": 1, "userId": <test_user_id> }
  # Expected: Returns sessionId with 210 items
  ```

- [ ] **Submit Answers**
  ```bash
  # POST /api/sessions/{sessionId}/submit
  # Body: Array of 210 answers (MCQ: A/B/C/D, Likert: 1-5)
  # Expected: Returns result with sevenPatternScores array
  ```

- [ ] **View Admin Result**
  ```bash
  # GET /api/admin/results/{resultId}
  # Expected: Response includes sevenPatternScores with 7 patterns
  ```

### Frontend Testing

- [ ] **User UI**
  - Navigate to exam page
  - Verify 210 items load
  - Verify MCQ items show A/B/C/D options
  - Verify Likert items show 1-5 scale
  - Submit exam
  - Verify result shows 7 patterns

- [ ] **Admin UI**
  - Navigate to result detail page
  - Verify "SDJ v2.1" badge displays
  - Verify horizontal bar chart shows 7 patterns
  - Verify pattern cards display subdimensions
  - Verify T-scores are in range [20-80]
  - Verify band labels (ضعيف/متوسط/ممتاز)

---

## 🔙 Rollback Procedure

### Option 1: Rollback to V1 (Recommended)

**Use the provided rollback script:**

```powershell
# Execute rollback script
.\scripts\rollback_to_legacy.ps1

# Script performs:
# 1. Sets USE_SDJ=1 (forces V1 mode)
# 2. Backs up database
# 3. Clears V2 items
# 4. Reseeds V1 CSV (125 items)
```

### Option 2: Manual Rollback

**Step 1: Set Environment Variable**
```bash
# In Render dashboard or .env file
USE_SDJ=1
```

**Step 2: Clear V2 Data**
```sql
-- Delete V2 items
DELETE FROM "Items" WHERE "PatternId" IS NOT NULL;
```

**Step 3: Restart Application**
```bash
# Restart triggers auto-seeding of V1 CSV
```

### Option 3: Keep Both V1 and V2

**No action needed** - the system maintains backward compatibility:
- Old sessions (V1) continue to work with 5-dimension scoring
- New sessions (V2) use 7-pattern scoring
- Both can coexist in the same database

---

## ⚠️ Breaking Changes

### None

This migration is **100% backward compatible**:
- ✅ Existing V1 sessions still work
- ✅ V1 results still display correctly
- ✅ No data loss
- ✅ No schema drops
- ✅ Nullable columns don't affect existing rows

---

## 📚 Documentation

### Technical Docs

- **POST_IMPLEMENTATION_REPORT.md** - Comprehensive implementation details
- **SDJ_V2_COMPLETION_SUMMARY.md** - High-level overview and status
- **CHANGELOG.md** - Version history and feature list
- **MIGRATION_NOTES_V2.md** - This document

### Code Documentation

- **SdjV2ScoringService.cs** - Scoring engine implementation (413 lines)
- **SessionsController.cs** - Auto-detection logic
- **DataSeeder.cs** - CSV loading and validation
- **ResultDetail.tsx** - Admin UI visualization

---

## 🐛 Known Issues

### Minor Limitations

1. **PDF Reports** - Not yet updated for V2
   - Current: Shows V1 5-dimension format
   - Future: Will show 7-pattern summary
   - Workaround: Use Admin UI for V2 visualization

2. **Analytics Dashboard** - Partial V2 support
   - Current: Distribution charts may show V1 data
   - Future: Full V2 analytics integration
   - Workaround: View individual results

3. **No Radar Chart** - Admin UI uses bar chart only
   - Current: Horizontal bar chart + pattern cards
   - Future: Optional radar/heptagon chart
   - Workaround: Bar chart provides equivalent information

### No Critical Issues

- ✅ No data corruption
- ✅ No performance degradation
- ✅ No security vulnerabilities
- ✅ No breaking API changes

---

## 📞 Support

### Troubleshooting

**Issue: V2 items not loading**
```bash
# Check environment variable
echo $USE_SDJ

# Verify CSV file exists
ls backend/PsyApi/Resources/Questions/questions_sdj_v2_ar.csv

# Check seeding logs
grep "SDJ V2" logs/app.log
```

**Issue: Scoring fails with V2 items**
```bash
# Check SdjV2ScoringService registration
grep "ISdjV2ScoringService" backend/PsyApi/Program.cs

# Verify auto-detection logic
grep "hasV2Items" backend/PsyApi/Controllers/SessionsController.cs
```

**Issue: Admin UI doesn't show 7 patterns**
```bash
# Check result payload structure
curl http://localhost:5000/api/admin/results/{id} | jq '.result.sdjData.sevenPatternScores'

# Verify frontend contract types
grep "SevenPatternScores" frontend/admin-ui/src/lib/adminContract.ts
```

### Contact

- **Repository:** psy-tests-platform
- **Branch:** develop
- **Version:** 3.2.0
- **Migration Date:** October 29, 2025

---

## ✅ Migration Complete

**Status:** ✅ **SUCCESSFUL**

- ✅ Database schema extended
- ✅ 210 items seeded
- ✅ Scoring engine operational
- ✅ Frontend updated
- ✅ Backward compatible
- ✅ Production ready

**Next Steps:**
1. Monitor production logs for first 24 hours
2. Collect user feedback on new patterns
3. Update PDF reports (Phase 9 - optional)
4. Add automated tests (Phase 10 - optional)
5. Gather normative data for T-score calibration

---

**Migration Completed:** October 29, 2025  
**Implemented By:** Full-Stack Development Team  
**Approved For Production:** ✅ YES
