# SDJ Activation Notes

**Date:** October 25, 2025  
**Mission:** End-to-end SDJ activation from audit findings

## Audit Summary (from AUDIT_SDJ_ACTIVATION.md)

**Root causes why legacy persisted:**
1. SDJ CSV missing from `backend/PsyApi/Resources/Questions/`
2. USE_SDJ environment variable not set to "1"
3. Admin UI correctly reflected backend mode (no sdjData = no badge)

**Evidence observed:**
- Runtime logs: "Successfully seeded 200 item parameters" (legacy count)
- Only `questions_fixed_extended_plus_personality.csv` present in Resources/Questions
- No `sdjData` block in API responses
- SDJ badge logic in adminContract.ts waiting for sdjData.SubDimensions

## Activation Steps Taken

### Phase 1: Dataset Preparation
- ✅ Copied `questions_sdj_ar.csv` (122 lines, 120 items) from `seed/` to `backend/PsyApi/Resources/Questions/`
- ✅ Verified file presence and UTF-8 encoding
- ✅ Created documentation structure: `docs/samples/sdj/`, `reports/samples/pdf/`

### SDJ CSV Structure
```csv
item_code,text_ar,type,dimension,sub_dimension,anchors_ar,reverse,time_limit_seconds,max_score,difficulty
```

**Key features:**
- All items are LikertAgreement type (1-5 numeric scale)
- Arabic text in `text_ar` field
- Structured taxonomy: `dimension` + `sub_dimension`
- Reverse scoring flag (0 or 1)
- Time limit per item (default 45s)

**Expected 7 dimension groups in taxonomy:**
1. الأنماط الشخصية (Personality Patterns)
2. القدرات المعرفية والعقلية (Cognitive Abilities)
3. الأنماط النفسية (Psychological Patterns)
4. الأنماط السلوكية (Behavioral Patterns)
5. الأنماط العددية والمنطقية (Numerical & Logical Patterns)
6. الأنماط القيادية والتنظيمية (Leadership & Organizational)
7. الاستعدادات المهنية العامة (General Vocational Aptitudes)

## Truth Table: Flag × CSV Presence

| USE_SDJ | CSV Present | Seeder Items | Scoring Path | sdjData in Results | Admin Badge |
|---------|-------------|--------------|--------------|-------------------|-------------|
| 0/unset | legacy only | 200 (legacy) | Legacy       | No                | No          |
| 0/unset | both        | 200 (legacy) | Legacy       | No                | No          |
| **1**   | **both**    | **120 (SDJ)**| **SDJ**      | **Yes**           | **Yes**     |
| 1       | legacy only | Error/fallback| Legacy      | No                | No          |

**Current target:** Row 3 (USE_SDJ=1, both CSVs present)

## Runtime Evidence Collection Plan

### Backend Startup Logs
- [ ] Effective USE_SDJ value printed
- [ ] CSV file path logged
- [ ] Item count seeded (~120 for SDJ)
- [ ] Sample dimension/subdimension logged

### API Payloads
- [ ] POST /api/sessions/start → capture session ID
- [ ] GET /api/sessions/{id}/next → verify Likert items with SDJ fields
- [ ] POST /api/sessions/{id}/submit → result with sdjData block
- [ ] GET /api/results/{id} → full sdjData structure

### PDF Generation
- [ ] Generate 3 sample PDFs with SDJ data
- [ ] Verify Arabic shaping (HarfBuzz)
- [ ] Confirm chart sizing (donuts 180px, bars horizontal)
- [ ] Check band colors (Red <40, Orange 40-54.9, Green ≥55)

### Admin UI
- [ ] Screenshot: Results list with SDJ badge
- [ ] Screenshot: Result detail with SDJ taxonomy
- [ ] Verify Arabic/RTL rendering
- [ ] No console errors

## Rollback Safety

To return to legacy mode:
```powershell
$env:USE_SDJ = "0"  # or unset
# Delete psy_dev.db to force reseed
dotnet run
```

Expected behavior:
- Seeds 200 legacy items
- No sdjData in results
- Admin UI shows no SDJ badge
- Reports show legacy dimensions

## Next Steps

1. Start backend with USE_SDJ=1 and capture startup logs
2. Delete psy_dev.db to force fresh seed
3. Complete session flow and capture all payloads
4. Generate sample PDFs
5. Visual QA in Admin UI
6. Document findings with screenshots
7. Create SDJ_GO_LIVE_CHECKLIST.md

---
*Status: Phase 1 complete, proceeding to Phase 2 (Backend startup & testing)*
