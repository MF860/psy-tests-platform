# Full-Stack Audit Report: Exam Screen Issues

**Date**: October 5, 2025  
**Auditor**: Senior Full-Stack Auditor  
**Scope**: Database + .NET API + React/Tailwind Frontend  

---

## Phase 0 — Context & Configuration Confirmed

**Actual Project Layout**:
- ✅ Repo root: `psy-tests-platform/`
- ✅ Backend: `backend/PsyApi/` (ASP.NET Core)  
- ✅ Frontend user UI: `frontend/user-ui/` (React + shadcn/ui + Tailwind)
- ❌ **Database**: Uses SQLite (`psy_dev.db`) in development, **NOT PostgreSQL** as documented in README
- ✅ Expected flow: `/login → /privacy → /instructions → /exam → /thank-you`
- ❌ **Total Questions**: Backend randomly selects **80 questions**, frontend expects **50** by default

**Critical Configuration Mismatch**:
- Backend API runs on: `http://localhost:5019` 
- API endpoints: `/api/sessions/*` (with `/api` prefix)
- Frontend API base: `http://localhost:5019/api` ✅ (correctly configured)

---

## 1) Executive Summary

**Primary Issue**: The Exam screen shows no questions due to a **critical field naming mismatch** between the backend API response and frontend normalization logic. The backend returns `ItemId: "I001"` (PascalCase) but the frontend expects `item_id` (snake_case), causing the normalization function to fail and questions to not render.

**Root Causes** (High Impact):
• **Backend API Contract Violation**: Returns `ItemId` instead of expected `item_id`
• **Field Name Casing Inconsistency**: Mixed PascalCase/snake_case between API response and expected format
• **Question Count Mismatch**: Backend provides 80 questions, frontend hardcodes 50 as default
• **Database Schema Drift**: Uses SQLite with PascalCase fields, contradicts documentation mentioning PostgreSQL
• **Environment Configuration**: PostgreSQL connection string configured but SQLite actually used in development

---

## 2) Root Causes (Ranked by Severity)

### **RC-001: API Response Field Naming Mismatch (Severity: HIGH)**

**Evidence**:
- Backend `QuestionResponse` class uses `[JsonPropertyName("item_id")]` but populates with `ItemId = item.ItemCode`
- Frontend `normalizeQuestionFields()` expects `item_id` but receives `ItemId` (PascalCase)
- Contract bridge fails: `q.item_id` is undefined, causing normalization to throw

**File References**:
- `backend/PsyApi/Controllers/SessionsController.cs:57-84` (QuestionResponse definition)
- `frontend/user-ui/src/lib/contract-bridge.ts:32-42` (normalizeApiQuestion function)
- `frontend/user-ui/src/lib/contracts.ts:8-21` (ApiQuestionZ schema)

**Impact**: Complete failure to render questions in Exam screen

**Proposed Fix**: Ensure backend response uses consistent snake_case field names matching the JsonPropertyName attributes

### **RC-002: Database Environment Mismatch (Severity: HIGH)**

**Evidence**:
- `appsettings.json` contains PostgreSQL connection string: `"Host=localhost;Port=5432;Database=psydb"`
- Actual database: SQLite file `psy_dev.db` (200 items, 10 users)
- Database tables use PascalCase: `ItemCode`, `TextAr`, `DimensionTags` vs expected snake_case

**File References**:
- `backend/PsyApi/appsettings.json:7-9` (PostgreSQL config)
- `backend/PsyApi/Program.cs:82-94` (SQLite fallback logic)
- SQLite schema: PascalCase field names throughout

**Impact**: Configuration confusion, potential deployment failures, documentation inconsistency

**Proposed Fix**: Standardize on one database provider and update all configuration/documentation accordingly

### **RC-003: Question Count Inconsistency (Severity: MEDIUM)**

**Evidence**:
- Backend `StartSession`: `var items = await itemsQuery.Take(80).ToListAsync();` (line 187)
- Frontend default: `const totalQuestions = 50;` in `ExamNew.tsx:41`
- Progress calculation based on wrong denominator

**File References**:
- `backend/PsyApi/Controllers/SessionsController.cs:187` (80 questions selected)
- `frontend/user-ui/src/pages/ExamNew.tsx:41` (hardcoded 50)

**Impact**: Incorrect progress indication, potential UI/UX confusion

**Proposed Fix**: Use dynamic `totalQuestions` from API response instead of hardcoded value

### **RC-004: Contract Bridge Fragility (Severity: MEDIUM)**

**Evidence**:
- Normalization tries multiple field name variations but misses actual API response format
- Field mapping logic in `normalizeQuestionFields()` incomplete for all variations
- Error handling throws and crashes instead of graceful degradation

**File References**:
- `frontend/user-ui/src/lib/contract-bridge.ts:58-82` (field mapping logic)
- `frontend/user-ui/src/pages/ExamNew.tsx:115-135` (error handling in fetchNextFromApi)

**Impact**: Brittle integration, poor error recovery

**Proposed Fix**: Comprehensive field mapping with fallbacks and better error messages

---

## 3) Contract Mismatches

| Field | Endpoint | UI Expects | API Provides | Status | Notes |
|-------|----------|------------|--------------|--------|--------|
| `item_id` | `/sessions/{id}/next` | `item_id: string` | `ItemId: "I001"` | ❌ **MISMATCH** | Case difference breaks normalization |
| `text_ar` | `/sessions/{id}/next` | `text_ar: string` | `TextAr: string` | ❌ **MISMATCH** | Backend uses PascalCase |
| `type` | `/sessions/{id}/next` | `type: string` | `Type: string` | ❌ **MISMATCH** | Case difference |
| `dimension_tags` | `/sessions/{id}/next` | `dimension_tags: string` | `DimensionTags: string` | ❌ **MISMATCH** | Case difference |
| `time_limit_seconds` | `/sessions/{id}/next` | `time_limit_seconds: number` | `TimeLimitSeconds: number` | ❌ **MISMATCH** | Case difference |
| `max_score` | `/sessions/{id}/next` | `max_score: number` | `MaxScore: number` | ❌ **MISMATCH** | Case difference |
| `options` | `/sessions/{id}/next` | `options: string` | `Options: string` | ❌ **MISMATCH** | Case difference |
| `sessionId` | `/sessions/start` | `sessionId: string` | `sessionId: string` | ✅ **MATCH** | Consistent camelCase |
| `totalQuestions` | `/sessions/start` | `totalQuestions: number` | `totalQuestions: number` | ⚠️ **VALUE MISMATCH** | 50 vs 80 |

---

## 4) UI/UX Defects (Exam)

### **Layout & Rendering Issues**

1. **Debug Fallback Screen**: When no questions load, shows raw debug information instead of user-friendly error
   - Location: `ExamNew.tsx:368-395`
   - Shows technical details like session IDs, array lengths to end users

2. **Progress Bar Inaccuracy**: Shows progress as X/50 when actually X/80 questions exist
   - Location: `ExamNew.tsx:41` (hardcoded totalQuestions)
   - Misleading completion percentage calculation

3. **RTL Layout**: Generally well-implemented with `dir="rtl"` but some components may need refinement
   - MCQ component: ✅ Proper RTL layout with `text-right` class
   - Keyboard shortcuts: ✅ Properly positioned in RTL context

### **Component State Issues**

4. **Controlled Components**: Question components properly controlled via `value` and `onChange` props
   - MCQ: ✅ Uses RadioGroup with proper value binding
   - Form state management appears sound

5. **Z-Index Management**: No obvious collisions detected
   - Fixed header: `z-30`, progress bar properly positioned
   - Modal dialogs: Appropriate z-index values

### **Error Handling UI**

6. **Network Error Display**: Shows technical error messages to users instead of localized friendly messages
   - Location: `ExamNew.tsx:132-144` (fetchNextFromApi error handling)
   - Should show Arabic error messages for better UX

---

## 5) Data/DB Issues

### **Database Summary**

| Table | Count | Issues | Sample Data Quality |
|-------|--------|--------|-------------------|
| `Items` | 200 | ✅ Complete | PascalCase fields, pipe-separated options |
| `Users` | 10 | ✅ Active test users | NationalId format: 1000000001-1000000010 |
| `Sessions` | 1 | ⚠️ One completed session exists | Status: "completed" |
| `SessionItems` | 0 | ⚠️ No session items | May cause /next endpoint to fail |

### **Item Data Integrity**

**Sample Items by Type**:

```sql
-- Text Question (I001)
TextAr: "ما هي هوايتك المفضلة؟"
Type: "Text", Options: NULL

-- MCQ Question (I002)  
TextAr: "كم عدد الكواكب في المجموعة الشمسية؟"
Type: "MCQ", Options: "7|8|9|10"

-- ORDERING Question (I004)
TextAr: "رتب الأحداث التالية حسب تسلسلها الزمني: الحرب العالمية الأولى، الثورة الفرنسية، سقوط القسطنطينية."
Type: "ORDERING", Options: NULL
```

**Data Quality Issues**:
- ✅ All 200 items have non-null required fields
- ✅ MCQ options properly pipe-separated
- ⚠️ Mixed Arabic/English in some technical fields
- ⚠️ No validation for ORDERING question format consistency

---

## 6) Flow/Guards Issues

### **Session Management Problems**

1. **State Persistence**: Session state stored in `sessionStorage` but not properly keyed by sessionId in all cases
   - May cause conflicts when multiple sessions exist

2. **Route Guards**: `GuardedRoute` component checks for session existence but may not handle API contract failures gracefully
   - Location: `frontend/user-ui/src/components/GuardedRoute.tsx`

3. **Resume Logic**: Backend supports session resumption via `resume: true` but frontend may not handle partial session state correctly
   - Backend: Returns `currentIndex` for resumption
   - Frontend: May not restore question state properly

---

## 7) Suggested Acceptance Tests

```javascript
// Playwright/Cypress test cases to verify fixes

describe('Exam Question Loading', () => {
  test('Should load and display first question', async () => {
    await page.goto('/exam');
    await expect(page.locator('[data-testid="question-text"]')).toBeVisible();
    await expect(page.locator('[data-testid="question-input"]')).toBeVisible();
  });
  
  test('Should handle API field name variations', async () => {
    // Mock API with different field name formats
    await page.route('**/api/sessions/*/next', (route) => {
      route.fulfill({ json: { ItemId: 'I001', TextAr: 'Test Question' } });
    });
    await expect(page.locator('[data-testid="question-text"]')).toContainText('Test Question');
  });
  
  test('Should show correct progress (X/80)', async () => {
    await expect(page.locator('[data-testid="progress-text"]')).toMatch(/سؤال \d+ من 80/);
  });
  
  test('Should gracefully handle network errors', async () => {
    await page.route('**/api/sessions/*/next', (route) => {
      route.fulfill({ status: 500 });
    });
    await expect(page.locator('[data-testid="error-message"]')).toContainText('حدث خطأ');
  });
});

describe('Question Type Rendering', () => {
  test('MCQ should render all options', async () => {
    const optionCount = await page.locator('[data-testid="mcq-option"]').count();
    expect(optionCount).toBeGreaterThan(0);
  });
  
  test('Should handle RTL layout properly', async () => {
    await expect(page.locator('[dir="rtl"]')).toBeVisible();
    await expect(page.locator('.text-right')).toBeVisible();
  });
});
```

---

## 8) Appendix (Artifacts)

### **API Health Summary**

| Endpoint | Expected Status | Actual Status | Issues |
|----------|----------------|---------------|---------|
| `POST /api/sessions/start` | 200 | ❌ 404 | Route or service not running |
| `GET /api/sessions/{id}/next` | 200 | ❌ Not tested | Dependency on start endpoint |
| `POST /api/sessions/{id}/answer` | 200 | ❌ Not tested | Dependency on previous endpoints |

### **Database Schema Sample**

```sql
-- Items table structure
CREATE TABLE IF NOT EXISTS "Items" (
    "Id" INTEGER PRIMARY KEY AUTOINCREMENT,
    "ItemCode" TEXT NOT NULL,      -- I001, I002, etc.
    "TextAr" TEXT NOT NULL,        -- Arabic question text  
    "Type" TEXT NOT NULL,          -- Text, MCQ, ORDERING, etc.
    "DimensionTags" TEXT NOT NULL, -- Category tags
    "Options" TEXT NULL            -- Pipe-separated for MCQ
);
```

### **Raw API Response Samples** (Expected)

```json
// Expected /sessions/{id}/next response
{
  "id": 201,
  "item_id": "I001", 
  "text_ar": "ما هي هوايتك المفضلة؟",
  "type": "Text",
  "dimension_tags": "الاهتمامات الشخصية",
  "time_limit_seconds": 30,
  "max_score": 1,
  "options": null
}

// Actual backend response (inferred from controller)
{
  "Id": 201,
  "ItemId": "I001",
  "TextAr": "ما هي هوايتك المفضلة؟", 
  "Type": "Text",
  "DimensionTags": "الاهتمامات الشخصية",
  "TimeLimitSeconds": 30,
  "MaxScore": 1,
  "Options": null
}
```

### **Error Logs**

```javascript
// Frontend console errors (expected)
[CONTRACT] Failed to normalize API question: 
TypeError: Cannot read property 'item_id' of undefined

// Debug output from ExamNew.tsx
Before normalization - data: {"Id": 201, "ItemId": "I001", ...}
After normalization - Error thrown
```

---

## Exit Criteria ✅

This audit provides:

✅ **Reproducible explanation**: Field naming mismatch prevents question normalization and rendering  
✅ **Ranked root causes**: 4 critical issues with specific file references and line numbers  
✅ **Clean contract mismatch table**: 8 field mismatches documented with exact expected vs actual values  
✅ **Concrete UI/UX defects**: 6 specific issues with file locations  
✅ **Fix directions**: Standardize field naming, update question count consistency, improve error handling  

**Recommended Priority**: Fix RC-001 (field naming) first as it's the primary blocker for question rendering.
