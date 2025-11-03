# 🧪 SDJ Session & PDF Testing Scripts

## Overview

These PowerShell scripts automate the complete workflow of:
1. ✅ Creating a test user
2. ✅ Starting an SDJ session
3. ✅ Submitting 80 realistic answers (varied performance across 7 patterns)
4. ✅ Completing the session & triggering SDJ V2 scoring
5. ✅ Downloading the Ultra Hi-Fi PDF report via Admin API
6. ✅ Opening the PDF automatically

**No manual UI interaction required!** 🎉

---

## 📁 Files

| File | Purpose |
|------|---------|
| `test_sdj_session_and_pdf.ps1` | **Main script** - Full automation with configurable parameters |
| `test_production_pdf.ps1` | **Quick launcher** - Tests production Render API |
| `test_local_pdf.ps1` | **Quick launcher** - Tests local development API |

---

## 🚀 Quick Start

### Test Production (Render)

```powershell
.\test_production_pdf.ps1
```

This will:
- Connect to `https://psy-api-backend.onrender.com`
- Create a test user with timestamp
- Complete a full SDJ session
- Download PDF to current directory
- Open PDF automatically

### Test Local Development

```powershell
# Make sure backend is running on localhost:5124
dotnet run --project backend/PsyApi

# In another terminal:
.\test_local_pdf.ps1
```

### Custom Configuration

```powershell
.\test_sdj_session_and_pdf.ps1 `
    -BaseUrl "https://your-api.com" `
    -AdminUsername "admin" `
    -AdminPassword "YourPassword123"
```

---

## 🎯 What the Script Does

### Step-by-Step Breakdown

```
┌─────────────────────────────────────────────────────────────┐
│ 1️⃣  Admin Authentication                                    │
│    • Logs in as admin                                       │
│    • Gets admin JWT token for API access                    │
└─────────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────────┐
│ 2️⃣  Create Test User                                        │
│    • National ID: TEST_20251103_143052 (timestamped)       │
│    • Full Name: محمد أحمد التجريبي                         │
│    • Registers or reuses existing user                      │
└─────────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────────┐
│ 3️⃣  Fetch SDJ Test Configuration                            │
│    • Queries /api/tests for SDJ test                        │
│    • Gets test ID                                           │
└─────────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────────┐
│ 4️⃣  Start SDJ Session                                       │
│    • POST /api/sessions/start                               │
│    • Gets session ID                                        │
└─────────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────────┐
│ 5️⃣  Fetch Questions                                         │
│    • GET /api/sessions/{id}/questions                       │
│    • Receives 80+ SDJ questions (MCQ + Likert)              │
└─────────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────────┐
│ 6️⃣  Submit Realistic Answers                                │
│    • Simulates 7 different performance patterns:            │
│      - Pattern 1: 85% correct, Likert avg 4.2 (Excellent)  │
│      - Pattern 2: 75% correct, Likert avg 3.8 (Good)       │
│      - Pattern 3: 65% correct, Likert avg 3.2 (Average)    │
│      - Pattern 4: 55% correct, Likert avg 2.8 (Weak)       │
│      - Pattern 5: 70% correct, Likert avg 3.5 (Good)       │
│      - Pattern 6: 90% correct, Likert avg 4.5 (Excellent)  │
│      - Pattern 7: 60% correct, Likert avg 3.0 (Average)    │
│    • Rotates patterns every ~11 questions                   │
│    • Adds realistic time delays (5-15 sec per question)     │
└─────────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────────┐
│ 7️⃣  Complete Session                                        │
│    • POST /api/sessions/{id}/complete                       │
│    • Triggers SDJ V2 scoring engine                         │
│    • Returns result ID                                      │
└─────────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────────┐
│ 8️⃣  Verify Result                                           │
│    • GET /api/admin/results/{id}                            │
│    • Checks for SDJ V2 format (PatternScores)               │
│    • Validates scoring completed                            │
└─────────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────────┐
│ 9️⃣  Download PDF                                            │
│    • GET /api/admin/results/{id}/pdf                        │
│    • Saves to: SDJ_Report_TEST123_timestamp.pdf             │
│    • Displays file size and path                            │
└─────────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────────┐
│ 🔟 Open PDF & Display Summary                               │
│    • Automatically opens PDF in default viewer              │
│    • Shows session/result IDs                               │
│    • Provides verification checklist                        │
│    • Saves session info to last_test_session.json           │
└─────────────────────────────────────────────────────────────┘
```

---

## 📊 Sample Output

```
================================================================================
  STEP 1: Admin Authentication
================================================================================
✅ Admin logged in successfully
ℹ️  Admin Token: eyJhbGciOiJIUzI1NiIs...

================================================================================
  STEP 2: Create Test User
================================================================================
✅ Test user created: TEST20251103_143052
ℹ️  User ID: 42

================================================================================
  STEP 3: Fetch SDJ Test Configuration
================================================================================
✅ Found SDJ test: اختبار سيلمان دانيال جوردان - الإصدار الثاني
ℹ️  Test ID: 1
ℹ️  Test Code: SDJ

================================================================================
  STEP 4: Start SDJ Session
================================================================================
✅ Session started successfully
ℹ️  Session ID: 156

================================================================================
  STEP 5: Fetch SDJ Questions
================================================================================
✅ Fetched 80 questions
ℹ️  MCQ Questions: 40
ℹ️  Likert Questions: 40

================================================================================
  STEP 6: Submit 80 SDJ Answers (Realistic Distribution)
================================================================================
ℹ️  Submitted 10/80 answers...
ℹ️  Submitted 20/80 answers...
ℹ️  Submitted 30/80 answers...
ℹ️  Submitted 40/80 answers...
ℹ️  Submitted 50/80 answers...
ℹ️  Submitted 60/80 answers...
ℹ️  Submitted 70/80 answers...
ℹ️  Submitted 80/80 answers...
✅ Submitted 80/80 answers

================================================================================
  STEP 7: Complete Session & Trigger Scoring
================================================================================
✅ Session completed successfully
ℹ️  Result ID: 178
ℹ️  Total Score: 520
ℹ️  Scoring Version: SDJ_V2

================================================================================
  STEP 8: Verify Result via Admin API
================================================================================
✅ Result verified via Admin API
ℹ️  Session ID: 156
ℹ️  User: محمد أحمد التجريبي
ℹ️  Scoring Model: SDJ_V2
ℹ️  Has Dimension Scores: True
✅ ✅ Result contains SDJ V2 data (PatternScores detected)

================================================================================
  STEP 9: Download PDF Report (Ultra Hi-Fi)
================================================================================
ℹ️  Downloading PDF from: /api/admin/results/178/pdf
✅ PDF downloaded successfully
ℹ️  File: SDJ_Report_TEST20251103_143052_20251103_143052.pdf
ℹ️  Size: 1847.23 KB
ℹ️  Path: C:\Users\ASUS\Desktop\saitest\psy-tests-platform\SDJ_Report_TEST20251103_143052_20251103_143052.pdf

================================================================================
  STEP 10: Opening PDF Report
================================================================================
✅ PDF opened in default viewer

================================================================================
  🎉 SDJ SESSION TEST COMPLETED SUCCESSFULLY
================================================================================

📋 Test Summary:
  • User National ID: TEST20251103_143052
  • Session ID: 156
  • Result ID: 178
  • Questions Answered: 80
  • PDF Downloaded: SDJ_Report_TEST20251103_143052_20251103_143052.pdf (1847.23 KB)

🔗 Quick Links:
  • Admin Dashboard: https://admin-ui-lyart-nu.vercel.app/sessions/156
  • PDF Location: C:\Users\ASUS\Desktop\saitest\psy-tests-platform\SDJ_Report_TEST20251103_143052_20251103_143052.pdf

✅ Verification Checklist:
  [ ] PDF opens successfully
  [ ] Contains 6 pages (Cover, Summary, Charts, Patterns, Plan, Courses)
  [ ] Shows 7-pattern heptagon radar chart
  [ ] Charts are high quality (450 DPI with halos)
  [ ] Arabic text displays correctly (no � symbols)
  [ ] T-scores displayed for all 7 patterns
  [ ] Theme colors visible (Aurora Glass or Noir Executive)

================================================================================
```

---

## 🔍 Verification Checklist

After the PDF opens, verify:

### ✅ Page Structure
- [ ] **Page 1:** Cover page with logo, user info, date
- [ ] **Page 2:** Executive summary with 7-pattern scores
- [ ] **Page 3:** Charts (heptagon radar + horizontal bars)
- [ ] **Page 4:** Pattern details and interpretations
- [ ] **Page 5:** Action plan for weak subdimensions
- [ ] **Page 6:** Suggested courses

### ✅ Visual Quality
- [ ] **Charts:** 450 DPI quality (sharp, no pixelation)
- [ ] **Halos:** Charts have glowing halos around bars/points
- [ ] **Theme:** Aurora Glass (light gradients) or Noir Executive (dark)
- [ ] **Typography:** Clean, professional Arabic font (Noto Naskh Arabic)

### ✅ Data Accuracy
- [ ] **7 Patterns:** All 7 SDJ V2 patterns displayed
- [ ] **T-Scores:** Numeric T-scores visible (range: 20-80)
- [ ] **Bands:** Color-coded bands (Excellent/Good/Average/Weak)
- [ ] **Heptagon:** 7-sided radar chart with pattern names

### ✅ Text Encoding
- [ ] **No � symbols:** Arabic text renders perfectly
- [ ] **RTL Direction:** Arabic flows right-to-left
- [ ] **Diacritics:** Tashkeel marks display correctly

---

## 🎨 Expected PDF Features (Ultra Hi-Fi v4.0)

### Design System
- **Aurora Glass Theme (Default):**
  - Light background with gradient overlays
  - Glassmorphism cards (frosted glass effect)
  - Vibrant accent colors (blues, purples, teals)
  
- **Noir Executive Theme:**
  - Dark charcoal background (#1A1A1A)
  - High contrast elements
  - Gold/amber accents

### Charts (450 DPI)
1. **Heptagon Radar Chart:**
   - 7 axes (one per pattern)
   - Filled polygon with transparency
   - Glowing halos on data points
   - Outside labels with Arabic pattern names

2. **Horizontal Bar Charts:**
   - T-score scale (0-100)
   - Color-coded bands overlay
   - Halos on bar ends
   - Pattern names on left (Arabic)

3. **Donut Chart:**
   - Band distribution (Excellent/Good/Average/Weak)
   - Center text with total count
   - Legend with percentages

### Typography
- **Arabic:** Noto Naskh Arabic (HarfBuzz shaped)
- **English:** Inter (fallback)
- **Headings:** Bold, large size, theme colors
- **Body:** Regular weight, readable line height

---

## 🛠️ Troubleshooting

### Script Fails at Admin Login
```powershell
# Check admin credentials in script
$AdminUsername = "admin"
$AdminPassword = "Admin@123"

# Or override when running:
.\test_sdj_session_and_pdf.ps1 -AdminUsername "youradmin" -AdminPassword "yourpass"
```

### No SDJ Test Found
```sql
-- Check if SDJ test exists in database:
SELECT id, code, name FROM "Tests" WHERE code = 'SDJ';

-- If missing, run seed script:
dotnet run --project backend/PsyApi
```

### PDF Download Timeout
```powershell
# Increase timeout in script (line ~360):
-TimeoutSec 120  # Default is 60 seconds
```

### PDF Opens But Old Format
```powershell
# Check Render deployment status:
# 1. Go to https://dashboard.render.com
# 2. Find "psy-api-backend" service
# 3. Check "Events" tab - should show recent deploy
# 4. Check "Logs" tab - look for:
#    "[UltraHiFi] SDJ V2 loaded - Patterns: 7"
```

### Connection Refused (localhost)
```powershell
# Make sure backend is running:
cd backend/PsyApi
dotnet run

# Then run test script in another terminal
```

---

## 📝 Saved Output Files

### `last_test_session.json`
Contains session details for later reference:
```json
{
  "Timestamp": "20251103_143052",
  "BaseUrl": "https://psy-api-backend.onrender.com",
  "NationalId": "TEST20251103_143052",
  "SessionId": 156,
  "ResultId": 178,
  "PdfPath": "C:\\Users\\ASUS\\Desktop\\saitest\\psy-tests-platform\\SDJ_Report_TEST20251103_143052_20251103_143052.pdf",
  "QuestionsAnswered": 80
}
```

Use this to:
- Retrieve session ID for admin dashboard
- Find PDF file path
- Track test history

---

## 🔗 Related Documentation

- **Ultra Hi-Fi PDF Documentation:** `psy-tests-platform/docs/ULTRA_HIFI_PDF_REPORT.md`
- **SDJ V2 Scoring:** `psy-tests-platform/docs/SDJ_V2_SEVEN_PATTERNS.md`
- **API Documentation:** `psy-tests-platform/docs/API.md`
- **Deployment Guide:** `psy-tests-platform/DEPLOYMENT_GUIDE_RENDER_VERCEL.md`

---

## 🎯 Advanced Usage

### Test Multiple Sessions in Parallel
```powershell
# Run 5 test sessions
1..5 | ForEach-Object -Parallel {
    .\test_production_pdf.ps1
}
```

### Test Different Performance Levels
Edit `test_sdj_session_and_pdf.ps1` line ~180-215 to adjust answer patterns:
```powershell
# Example: Make all patterns excellent
"Pattern1" = @{
    MCQ_Correct_Rate = 0.95
    Likert_Avg = 4.8
}
```

### Extract Session Info from JSON
```powershell
$lastSession = Get-Content .\last_test_session.json | ConvertFrom-Json
Write-Host "Session ID: $($lastSession.SessionId)"
Write-Host "Result ID: $($lastSession.ResultId)"
```

---

## 📧 Support

If you encounter issues:
1. Check Render logs for backend errors
2. Verify admin credentials are correct
3. Ensure database is seeded with SDJ test
4. Check network connectivity to API
5. Review `last_test_session.json` for details

---

## 📜 License

Part of the Psy Tests Platform project.

---

**Happy Testing! 🧪✨**
