# SDJ Platform - Deployment & Go-Live Guide

## 🎯 Executive Summary

This guide provides comprehensive instructions for deploying the SDJ (Sustainable Development Framework) psychometric assessment platform to production. The platform has been fully upgraded with:

- ✅ **Admin Analytics Fix**: Score distribution chart now displays T-score buckets correctly
- ✅ **MCQ Question Type**: 5 new MCQ items added to question bank (total: 125 items)
- ✅ **Enhanced PDF Reports**: Arabic-only header with logo, larger charts, 7-category psychological map
- ✅ **Full Arabic RTL Support**: All UI components support right-to-left Arabic text
- ✅ **SDJ Scoring Model**: 5 main dimensions, 24 sub-dimensions, T-score normalization

---

## 📋 Prerequisites

### Backend Requirements
- **.NET 8 SDK** (verified working)
- **PostgreSQL 14+** with database `psytestdb`
- **Environment Variables**:
  ```powershell
  $env:USE_SDJ="1"  # Enable SDJ mode
  ```

### Frontend Requirements
- **Node.js 18+** with npm/pnpm
- **User UI**: `http://localhost:5173`
- **Admin UI**: `http://localhost:5175`

### Resources Verified
- ✅ Logo: `backend/PsyApi/Resources/Brand/SAITES-ICON.png`
- ✅ Font: `backend/PsyApi/Resources/Fonts/NotoNaskhArabic-*.ttf` (Regular & Bold)
- ✅ Question Bank: `backend/PsyApi/Resources/Questions/questions_sdj_ar.csv` (125 items)

---

## 🚀 SDJ Activation Steps

### Step 1: Build Backend
```powershell
cd psy-tests-platform\backend\PsyApi
dotnet build
```

**Expected Output**: Build succeeded with 0 errors (6 warnings about unused variables are OK)

### Step 2: Set Environment Variable
```powershell
# PowerShell
$env:USE_SDJ="1"

# Or add to appsettings.json:
{
  "Scoring": {
    "ModelType": "SDJ",
    "UseSdj": true
  }
}
```

### Step 3: Run Database Migrations
```powershell
dotnet run --urls="http://localhost:5019"
```

**Verification**: Check logs for:
- `Successfully seeded 125 items` (120 Likert + 5 MCQ)
- `Successfully seeded 200 item parameters`
- `Now listening on: http://localhost:5019`

### Step 4: Start Frontend Services
```powershell
# Terminal 1 - User UI
cd psy-tests-platform\frontend\user-ui
npm install  # First time only
npm run dev

# Terminal 2 - Admin UI
cd psy-tests-platform\frontend\admin-ui
npm install  # First time only
npm run dev
```

---

## ✅ Go-Live Checklist

### Pre-Deployment Validation

#### 1. Backend Health Check
- [ ] Server starts without errors: `GET http://localhost:5019/health`
- [ ] Database seeded with 125 items: Check logs for "Successfully seeded 125 items"
- [ ] MCQ items present: Verify M001-M005 in database
- [ ] SDJ mode enabled: Look for SDJ-specific log messages

#### 2. User Flow Testing
- [ ] **Registration**: Create test user account
- [ ] **Login**: Authenticate successfully
- [ ] **Start Assessment**: Initialize new SDJ session
- [ ] **Answer Questions**: Complete 5-10 items (mix of Likert + MCQ)
  - Verify Likert scale renders with Arabic labels
  - Verify MCQ options display with radio buttons
  - Verify RTL text alignment
- [ ] **Submit Session**: POST answers to `/api/sessions/{id}/submit`
- [ ] **View Results**: Check results page displays 5 dimensions + 24 sub-dimensions
- [ ] **Download PDF**: Verify PDF downloads with:
  - Centered SAITES-ICON logo (140px)
  - Arabic title "منصة التحليل النفسي المتقدم"
  - NO English subtitle
  - Enhanced charts (larger fonts: 11-12pt)
  - 7-category psychological map

#### 3. Admin Dashboard Testing
- [ ] **Login**: Admin credentials (username: `admin`, password: `Admin@123`)
- [ ] **View Analytics**: Navigate to dashboard
- [ ] **Verify Score Distribution Chart**: 
  - Chart shows 5 T-score buckets: <40, 40-44, 45-54, 55-59, ≥60
  - Counts are non-zero (if users completed sessions)
- [ ] **Inspect Network Tab**: Verify `GET /api/admin/analytics` returns `scoreDistribution` array

#### 4. PDF Report Verification
Download a sample PDF and verify:

**Page 1 - Cover**
- [ ] Logo: SAITES-ICON.png centered, 140px width
- [ ] Title: "منصة التحليل النفسي المتقدم" (22pt bold, Arabic-only)
- [ ] Subtitle: Arabic inspirational text (no English)
- [ ] User info card: Name, National ID, Date, Model version

**Page 2 - Charts**
- [ ] 24 sub-dimensions horizontal bars (11pt labels, 16px height)
- [ ] Bar chart numbers: 11pt font size
- [ ] 5 main dimensions summary cards (22pt scores, 10pt labels)

**Page 3 - Interpretation**
- [ ] Band interpretation table (Excellent/Average/Weak)
- [ ] **7-Category Psychological Map**: New visualization showing:
  1. الأنماط الشخصية (Personality Patterns)
  2. القدرات المعرفية والعقلية (Cognitive Abilities)
  3. الأنماط النفسية (Psychological Patterns)
  4. الأنماط السلوكية (Behavioral Patterns)
  5. الأنماط العددية والمنطقية (Numerical/Logical)
  6. الأنماط القيادية والتنظيمية (Leadership)
  7. الاستعدادات المهنية العامة (Career Readiness)
- [ ] Dimension narratives (5 main dimensions)
- [ ] Top 3 strengths
- [ ] Career track recommendations

---

## 🔧 Configuration Reference

### Critical Environment Variables
```bash
USE_SDJ=1                    # Enable SDJ mode
ConnectionStrings__Default   # PostgreSQL connection string
JWT__Secret                  # JWT signing key (production)
```

### Database Connection String
```
Host=localhost;Port=5432;Database=psytestdb;Username=postgres;Password=yourpassword
```

### CORS Origins (Production)
Update `Program.cs`:
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("https://your-production-domain.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});
```

---

## 📊 Key Features Implemented

### 1. Admin Analytics Score Distribution
**File**: `backend/PsyApi/Controllers/AdminAnalyticsController.cs`

**Implementation**:
```csharp
var allTScores = dimensionData.Select(d => d.T).Where(t => t > 0).ToList();
var scoreDistribution = new List<object>
{
    new { score = "ضعيف (< 40)", count = allTScores.Count(t => t < 40) },
    new { score = "أقل من المتوسط (40-44)", count = allTScores.Count(t => t >= 40 && t < 45) },
    new { score = "متوسط (45-54)", count = allTScores.Count(t => t >= 45 && t < 55) },
    new { score = "أعلى من المتوسط (55-59)", count = allTScores.Count(t => t >= 55 && t < 60) },
    new { score = "ممتاز (≥ 60)", count = allTScores.Count(t => t >= 60) }
};
```

### 2. MCQ Question Type Support
**File**: `backend/PsyApi/Resources/Questions/questions_sdj_ar.csv`

**New Items** (M001-M005):
- M001: Work values (MCQ with 5 options)
- M002: Problem-solving approach
- M003: Communication preferences
- M004: Workplace motivation
- M005: Leisure activities

**Format**:
```csv
Code,Text,Type,Dimension,SubDimension,Options,MinScore,MaxScore,Weight,Difficulty
M001,ما هي القيمة الأكثر أهمية بالنسبة لك في العمل؟,MCQ,النجاح المهني,التخطيط الاستراتيجي,الإبداع والابتكار|التعاون مع الفريق|تحقيق الأهداف|الاستقلالية|التطوير المستمر,0,60,5,3
```

### 3. Enhanced PDF Reports
**File**: `backend/PsyApi/Services/Reports/UltimateArabicPdfReportService.cs`

**Changes**:
- Logo size: 120px → 140px
- Title font: 24pt → 22pt (Arabic-only)
- Removed English subtitle
- Added inspirational Arabic text
- Bar chart labels: 9pt → 11pt
- Bar chart height: 14px → 16px
- Dimension cards: 20pt → 22pt (scores), 80px → 90px (height)
- **New**: 7-category psychological map with color-coded bars

---

## 🛠️ Troubleshooting

### Issue: Admin Dashboard Shows Zero Distribution
**Symptom**: "توزيع الدرجات" chart displays all zeros

**Solution**: 
1. Verify backend API returns `scoreDistribution` array:
   ```powershell
   Invoke-RestMethod -Uri "http://localhost:5019/api/admin/analytics" -Headers @{Authorization="Bearer YOUR_TOKEN"}
   ```
2. Check frontend contract includes `scoreDistribution` field (already fixed in `adminContract.ts`)

### Issue: MCQ Items Not Rendering
**Symptom**: MCQ questions display as Likert scales

**Solution**:
1. Verify item type is exactly `MCQ` (case-sensitive)
2. Check `Options` field contains pipe-separated values: `الخيار 1|الخيار 2|الخيار 3`
3. Ensure `MCQ.tsx` component is imported in `ExamNew.tsx`

### Issue: PDF Arabic Text Garbled
**Symptom**: PDF shows ???????? instead of Arabic

**Solution**:
1. Verify fonts exist: `backend/PsyApi/Resources/Fonts/NotoNaskhArabic-Regular.ttf`
2. Check HarfBuzz shaping is enabled (already configured in `UltimateArabicPdfReportService.cs`)
3. Ensure QuestPDF license is set: `QuestPDF.Settings.License = LicenseType.Community;`

### Issue: 7-Category Map Missing in PDF
**Symptom**: Page 3 doesn't show psychological map

**Solution**:
1. Verify `Calculate7Categories()` method exists in `UltimateArabicPdfReportService.cs`
2. Check sub-dimensions have matching names (keywords: "الاستقرار العاطفي", "التفكير النقدي", etc.)
3. Ensure categories are rendered after band interpretation table

---

## 🔄 Rollback Procedure

If issues arise post-deployment:

### Quick Rollback (Disable SDJ)
```powershell
# Option 1: Environment variable
$env:USE_SDJ="0"

# Option 2: appsettings.json
{
  "Scoring": {
    "UseSdj": false
  }
}

# Restart backend
dotnet run
```

### Full Rollback (Revert to Legacy)
1. Stop backend server
2. Restore previous `questions_legacy.csv`
3. Set `USE_SDJ=0`
4. Restart services
5. Verify legacy scoring works with test session

---

## 📞 Support Contacts

**Technical Lead**: [Your Name]
**Email**: [your.email@domain.com]
**Repository**: [GitHub URL]

---

## 📝 Change Log

### Version 2.1.0 (SDJ Full Release) - 2025-01-15
- ✅ Fixed admin analytics score distribution (zero bug)
- ✅ Added 5 MCQ items to question bank (M001-M005)
- ✅ Enhanced PDF Page 1: Arabic-only header with centered logo
- ✅ Enhanced PDF Page 2: Increased chart sizes and fonts (11-12pt)
- ✅ Added 7-category psychological map to PDF Page 3
- ✅ Verified all 125 items seed successfully
- ✅ Confirmed MCQ rendering with RTL Arabic support

### Version 2.0.1 (Previous Hotfix)
- Fixed admin data deserialization
- Improved TryParseSdjData helper
- Added better error handling

---

## 🎓 Training Resources

### For Administrators
1. **Dashboard Navigation**: Located at `/admin/dashboard`
2. **Key Metrics**:
   - Total users, sessions, completions
   - Completion rate percentage
   - Score distribution (5 T-score buckets)
   - Dimension averages
3. **Report Generation**: Download PDFs from user results page

### For End Users
1. **Registration**: Create account with National ID
2. **Assessment**: Answer 125 items (15-20 minutes)
3. **Results**: View 5 dimensions + 24 sub-dimensions
4. **PDF Report**: Download comprehensive 3-page report

---

## ✨ Future Enhancements (Roadmap)

- [ ] Multi-language support (English + Arabic)
- [ ] Real-time progress tracking during assessment
- [ ] Advanced analytics dashboard with trend analysis
- [ ] AI-powered career recommendations via OpenRouter
- [ ] Mobile app (React Native)
- [ ] Batch user imports (CSV)

---

**Last Updated**: 2025-01-15  
**Document Version**: 1.0  
**Status**: ✅ Ready for Production Deployment
