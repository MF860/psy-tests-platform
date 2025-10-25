# SDJ Migration: Quick Reference Card

## 🚀 Quick Start (5 Commands)

```powershell
# 1. Apply migration
cd backend/PsyApi
dotnet ef database update

# 2. Enable SDJ mode
$env:USE_SDJ = "1"

# 3. Seed SDJ questions
dotnet run --seed

# 4. Start backend
dotnet run  # http://localhost:5000

# 5. Start frontend
cd ../../frontend/user-ui
npm run dev  # http://localhost:5173
```

## ✅ Completion Status

| Phase | Status | Files | Acceptance |
|-------|--------|-------|------------|
| **A: Planning** | ✅ Complete | 1 doc | 5/5 criteria |
| **B: Schema** | ✅ Complete | 4 files | 7/7 criteria |
| **C: Backend** | ✅ Complete | 4 files | 11/11 criteria |
| **D: User UI** | ✅ Complete | 2 files | 9/9 criteria |
| **E: Admin UI** | ⏳ Pending | 5 files | 0/5 criteria |
| **F: PDF** | ⏳ Pending | 1 file | 0/10 criteria |
| **G: Testing** | ⏳ Pending | 5 files | 0/7 criteria |

**Progress**: 33% Complete (4/7 phases) | 26-33 hours remaining

## 📊 SDJ Framework Structure

```
SDJ (Sustainable Development Journey)
├── 5 Parent Dimensions
│   ├── التميز الذاتي (Self-Excellence) - 25 items
│   ├── التواصل والعلاقات (Relationships) - 25 items
│   ├── النجاح المهني (Career Success) - 25 items
│   ├── المسؤولية الاجتماعية (Social Responsibility) - 25 items
│   └── الصحة والتوازن (Health & Balance) - 20 items
├── 24 Sub-Dimensions (5 items each)
├── 120 Total Likert Items (1-5 scale)
└── 23 Reverse-Scored Items (19.2%)
```

## 🔢 Scoring Algorithm

```csharp
// 1. Reverse Scoring
score = item.Reverse ? (6 - raw) : raw;

// 2. Sub-Dimension Aggregation
subDimScore = Mean(itemScores);  // 5 items

// 3. Dimension Aggregation
dimScore = Mean(subDimScores);   // 5 sub-dimensions

// 4. T-Score Transformation
Z = (raw - 3.0) / 0.8;
T = 50 + (10 × Z);

// 5. Banding
if (T < 40) → "Weak"
else if (T < 55) → "Average"
else → "Excellent"
```

## 🗂️ Key Files

### Backend
```
backend/PsyApi/
├── Services/Scoring/SdjScoringService.cs    [400 lines] ✅
├── Controllers/SessionsController.cs         [Modified] ✅
├── Controllers/ResultsController.cs          [Modified] ✅
├── Models/Item.cs                            [+3 fields] ✅
├── Migrations/20251024_AddSdjFieldsToItems.cs [EF] ✅
└── Program.cs                                [+DI reg] ✅
```

### Frontend
```
frontend/user-ui/src/
├── components/Question/Likert.tsx            [Modified] ✅
└── pages/Instructions.tsx                    [Modified] ✅
```

### Data
```
seed/
└── questions_sdj_ar.csv                      [120 items] ✅
```

### Docs
```
psy-tests-platform/
├── README_SDJO_MIGRATION.md                  [Spec] ✅
├── SDJ_PROGRESS_REPORT.md                    [Status] ✅
├── PHASE_C_COMPLETION.md                     [Phase C] ✅
├── PHASE_D_COMPLETION.md                     [Phase D] ✅
├── PHASES_E_F_G_ROADMAP.md                   [E-G Guide] ✅
└── SDJ_MIGRATION_COMPLETE_SUMMARY.md         [Summary] ✅
```

## 🔧 Environment Variables

```powershell
# Required
$env:USE_SDJ = "1"              # Enable SDJ mode

# Optional
$env:POPULATION_MEAN = "3.0"    # T-score calibration
$env:POPULATION_SD = "0.8"      # T-score calibration
```

## 📝 API Changes (Non-Breaking)

### `GET /api/results/{id}/birkman`
**New Field**: `SdjData` (nullable)
```json
{
  "SdjData": {
    "Dimensions": [{...}],      // 5 parent dimensions
    "SubDimensions": [{...}],   // 24 sub-dimensions
    "TrackFits": [{...}]        // 3 career tracks
  }
}
```

### `POST /api/sessions/{id}/submit-answer`
**Changed**: Likert answers now accept `"1"` to `"5"` (previously Arabic labels)
```json
{
  "ItemId": "I001",
  "Answer": "4"  // Was: "أوافق"
}
```

## 🧪 Testing Commands

### Validate CSV
```powershell
cd tools
node validate_sdj_csv.js
```

### Run Backend Tests (Phase G)
```powershell
cd backend/PsyApi.Tests
dotnet test
```

### Run E2E Tests (Phase G)
```powershell
cd frontend/user-ui
npx playwright test exam-sdj.spec.ts
```

### Generate Sample PDFs (Phase G)
```powershell
./test_sdj_pdf_generation.ps1
```

## 🔄 Rollback Procedure

```powershell
# 1. Disable SDJ
$env:USE_SDJ = "0"

# 2. Restart backend
cd backend/PsyApi
dotnet run

# 3. (Optional) Rollback migration
dotnet ef database update 20251023_PreviousMigration
```

**Data Safety**: No data loss. Both SDJ and legacy sessions coexist.

## 🚨 Troubleshooting

### Questions not loading?
```powershell
echo $env:USE_SDJ              # Should be "1"
Test-Path seed/questions_sdj_ar.csv
dotnet run --seed              # Re-seed
```

### Scores missing?
```powershell
# Check scoring service registered
# In Program.cs: builder.Services.AddScoped<ISdjScoringService>

curl http://localhost:5000/api/sessions/{id}/submit
```

### PDF fails?
```powershell
# Check QuestPDF license in Program.cs
# Ensure HarfBuzzSharp package installed
curl http://localhost:5000/api/results/1/pdf -o test.pdf
```

## 📦 Dependencies

### Backend (.NET 8.0)
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.*" />
<PackageReference Include="QuestPDF" Version="2024.*" />
<PackageReference Include="HarfBuzzSharp" Version="7.*" />
<PackageReference Include="SkiaSharp" Version="2.88.*" />
```

### Frontend (React 18)
```json
{
  "recharts": "^2.10.0",  // For Phase E
  "react": "^18.2.0",
  "vite": "^5.0.0"
}
```

## 📞 Support Resources

- **Migration Spec**: `README_SDJO_MIGRATION.md` (15 sections)
- **Scoring Logic**: `SdjScoringService.cs` (lines 77-270)
- **CSV Format**: `questions_sdj_ar.csv` (headers + 120 rows)
- **Validation**: `validate_sdj_csv.js` (150 lines)

## 🎯 Next Steps

1. **Phase E**: Install Recharts → Create horizontal bar chart component
2. **Phase F**: Extend PDF service → 5-page redesign
3. **Phase G**: Write 30+ tests → Generate sample PDFs

**Estimated Time**: 26-33 hours total for Phases E-G

---

**Last Updated**: December 2024  
**Migration Version**: SDJ v1.0  
**Status**: 33% Complete (Phases A-D done, E-G pending)
