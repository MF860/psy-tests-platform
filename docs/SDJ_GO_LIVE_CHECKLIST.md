# SDJ Production Go-Live Checklist

**Document Version:** 1.0.0  
**Target Release:** v2.0.0  
**Last Updated:** 2025  
**Status:** Production-Ready

---

## Overview

This checklist ensures a safe, complete deployment of the **SDJ (Structured Dimensional Judgment) Framework** to production. The SDJ mode provides a 120-item psychometric assessment with 5 dimensions, 24 subdimensions, and career track recommendations.

---

## Pre-Deployment Checklist

### 1. Environment Configuration

#### Backend Configuration
- [ ] **Set `USE_SDJ=1`** in production environment variables
  ```bash
  # Linux/macOS
  export USE_SDJ=1
  export ASPNETCORE_ENVIRONMENT=Production
  
  # Windows (PowerShell)
  $env:USE_SDJ = "1"
  $env:ASPNETCORE_ENVIRONMENT = "Production"
  ```

- [ ] **Verify CSV file deployment**
  - Confirm `questions_sdj_ar.csv` exists in seed directory
  - File location: `backend/PsyApi/seed/questions_sdj_ar.csv`
  - Expected size: 120 items (rows)

- [ ] **Database backup completed**
  ```bash
  # Example SQLite backup
  cp psy_dev.db psy_dev_backup_$(date +%Y%m%d_%H%M%S).db
  ```

- [ ] **Clean database deployment** (if fresh install)
  - Delete existing `psy_dev.db` (or production equivalent)
  - Backend will auto-seed on first run

---

### 2. Backend Verification

#### Startup Logs
- [ ] **120 items seeded correctly**
  - Look for: `Successfully seeded 120 item parameters into the database.`
  - Verify no errors in logs

- [ ] **5 dimensions configured**
  - Dimensions: Realistic, Investigative, Artistic, Social, Enterprising

- [ ] **24 subdimensions mapped**
  - Each dimension has 4-5 subdimensions
  - Verify in database: `SELECT COUNT(*) FROM Items WHERE SubDimension IS NOT NULL`

#### API Testing
- [ ] **POST /api/sessions/start** returns SDJ Likert items
  ```bash
  curl -X POST http://localhost:5019/api/sessions/start \
    -H "Content-Type: application/json" \
    -d '{"nationalId":"1000000001"}'
  ```
  - Expected: `currentItem.type` should be `"LikertAgreement_SDJ"`
  - Expected: `totalItems` should be `120`

- [ ] **Result contains `sdjData` field**
  - After completing session, GET `/api/results/{resultId}`
  - Verify JSON structure:
    ```json
    {
      "sdjData": {
        "Dimensions": [...],       // 5 parent dimensions with T-scores
        "SubDimensions": [...],    // 24 subdimensions with scores
        "TrackFits": [...]         // 3 career track recommendations
      },
      "sdjProfile": "Realistic-Investigative"
    }
    ```

---

### 3. Admin UI Verification

#### Results List Page
- [ ] **SDJ badge visible**
  - Blue badge displaying: `SDJ: {profileType}`
  - Example: `SDJ: Realistic-Investigative`
  - Component: `ResultsList.tsx` lines 238-245

- [ ] **Badge only appears for SDJ results**
  - Legacy results (USE_SDJ=0) should NOT show badge
  - Verify by checking `result.hasSdjData` flag

#### Result Detail Page
- [ ] **ResultDetailSDJ page renders correctly**
  - Route: `/results/{id}` (auto-detects SDJ data)
  - URL: `http://localhost:5173/results/{id}`

- [ ] **Horizontal bars show all 24 subdimensions**
  - Each bar labeled with subdimension name (Arabic)
  - Color-coded by score range (red/yellow/green)
  - T-scores displayed (0-100 scale)

- [ ] **Dimension accordion displays 5 parent dimensions**
  - Collapsible cards for each dimension
  - Shows dimension T-score
  - Lists child subdimensions

- [ ] **Career track cards show 3 recommendations**
  - Top 3 tracks sorted by fit percentage
  - Each card displays: track name, fit score, key dimensions
  - Examples: Academic, Technical, Creative, Leadership, Service, Administrative

---

### 4. PDF Verification

#### PDF Generation
- [ ] **PDF download button works without errors**
  - Click download in admin UI
  - Endpoint: `GET /api/results/{resultId}/pdf`
  - Expected: HTTP 200 response, PDF file download

- [ ] **PDF contains 3 pages**
  1. **Page 1:** Cover with logo, title, user info
  2. **Page 2:** Charts (horizontal bars for 24 subdimensions)
  3. **Page 3:** Interpretation text + career track cards

- [ ] **Arabic text renders correctly**
  - No `�` glyphs or tofu characters
  - Font: Noto Naskh Arabic
  - RTL layout correct

- [ ] **Charts display properly**
  - 24 horizontal bars visible
  - 5 dimension cards rendered
  - 3 career track cards shown

#### PDF Routing Logic
- [ ] **SDJ results use `RenderSdjResultPdfAsync`**
  - Check logs for: `Rendering SDJ PDF report...`
  - Service: `UltimateArabicPdfReportService`

- [ ] **Legacy results use `RenderResultPdfAsync`**
  - Fallback for non-SDJ results
  - Service: `ModernPdfReportService` (stub)

---

### 5. Rollback Verification

#### Legacy Mode Testing
- [ ] **Set `USE_SDJ=0`** seeds 200 items correctly
  ```bash
  export USE_SDJ=0
  # Delete database
  rm psy_dev.db
  # Restart backend
  dotnet run
  ```
  - Expected: `Successfully seeded 200 item parameters`
  - CSV file: `questions_fixed_extended_plus_personality.csv`

- [ ] **Legacy sessions work without errors**
  - POST `/api/sessions/start` returns diverse item types
  - Item types: MCQ, TIMED_NUMERIC, ORDERING, LikertAgreement, Frequency
  - NO `LikertAgreement_SDJ` type

- [ ] **Legacy PDF generation succeeds**
  - GET `/api/results/{legacyResultId}/pdf` returns 200
  - PDF uses legacy template (4-6 pages, not 3)

- [ ] **No SDJ badges appear for legacy results**
  - Admin UI results list shows no blue badges
  - `hasSdjData` flag is `false`

#### Mode Switching Safety
- [ ] **Switch back to USE_SDJ=1 works cleanly**
  ```bash
  export USE_SDJ=1
  rm psy_dev.db
  dotnet run
  ```
  - Expected: 120 items seeded
  - SDJ features functional

- [ ] **No data corruption during switches**
  - Database schema unchanged
  - No foreign key errors
  - Session state preserved

---

## Post-Deployment Verification

### 6. Production Smoke Tests

#### Critical Path Testing
- [ ] **Create SDJ session**
  - POST `/api/sessions/start` → HTTP 200
  - Returns session ID and first SDJ Likert item

- [ ] **Answer all 120 questions**
  - POST `/api/sessions/{id}/answer` (x120) → HTTP 200
  - Session progresses through all items

- [ ] **Submit session**
  - POST `/api/sessions/{id}/submit` → HTTP 200
  - Result created with `sdjData` field

- [ ] **View result in admin UI**
  - Navigate to `http://localhost:5173/results`
  - SDJ badge visible
  - Click to open detail page

- [ ] **Generate PDF**
  - Click download button → PDF file saved
  - Open PDF: 3 pages, all charts visible

#### Performance Testing
- [ ] **Session creation < 2 seconds**
- [ ] **Item answer processing < 500ms**
- [ ] **Result calculation < 5 seconds** (120 items)
- [ ] **PDF generation < 10 seconds** (3-page SDJ template)

---

### 7. Documentation Complete

#### README Updates
- [ ] **Root `README.md` updated**
  - Section added: "## SDJ Mode Configuration"
  - Environment variable instructions
  - Mode switching guide

- [ ] **Backend `README.md` updated**
  - Location: `backend/PsyApi/README.md`
  - API endpoint documentation for SDJ
  - CSV file structure explained

#### Screenshots Captured
- [ ] **5 screenshots saved** to `docs/screenshots/sdj/`:
  1. `admin-results-list-sdj-badge.png` – Results list with blue badge
  2. `result-detail-horizontal-bars.png` – 24 subdimensions chart
  3. `result-detail-radar-chart.png` – 5-axis radar or dimension cards
  4. `pdf-page1-cover.png` – PDF cover page
  5. `pdf-page2-charts.png` – PDF charts page

#### Version Control
- [ ] **Git commit created**
  ```bash
  git add -A
  git commit -m "feat: SDJ Framework Migration Complete - v2.0.0"
  ```

- [ ] **Git tag `v2.0.0` created**
  ```bash
  git tag -a v2.0.0 -m "SDJ Migration Complete\n\n- Dual-mode support (SDJ/Legacy)\n- 120 SDJ items with 5 dimensions, 24 subdimensions\n- Admin UI badge and charts\n- 3-page PDF template\n- Rollback safety verified"
  git push origin main --tags
  ```

---

## Rollback Plan

### Emergency Rollback Procedure

If critical issues arise in production:

1. **Stop backend application**
   ```bash
   pkill -f dotnet
   ```

2. **Set legacy mode**
   ```bash
   export USE_SDJ=0
   ```

3. **Restore database backup** (if needed)
   ```bash
   mv psy_dev_backup_YYYYMMDD_HHMMSS.db psy_dev.db
   ```

4. **Restart backend**
   ```bash
   dotnet run
   ```

5. **Verify legacy mode active**
   - Check logs: `Successfully seeded 200 item parameters`
   - Test session creation with legacy items

---

## Sign-Off

| Role | Name | Signature | Date |
|------|------|-----------|------|
| **Developer** | __________ | __________ | __/__/__ |
| **QA Lead** | __________ | __________ | __/__/__ |
| **Product Owner** | __________ | __________ | __/__/__ |
| **DevOps Engineer** | __________ | __________ | __/__/__ |

---

## References

- **SDJ Framework Documentation:** `IMPLEMENTATIONS/SDJ_IMPLEMENTATION_COMPLETE.md`
- **Migration Audit:** `AUDIT_SDJ_ACTIVATION.md`
- **Phase Reports:** `docs/SDJ_PHASE{1-4}_COMPLETE.md`
- **API Documentation:** `backend/PsyApi/README.md`

---

**Status:** ✅ All checks must pass before production deployment  
**Version:** 1.0.0  
**Release:** v2.0.0 Production Candidate
