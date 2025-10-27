# SDJ 7-Pattern Deployment Guide - Vercel + Render

## Quick Deploy (5 Steps)

### Step 1: Add All Files to Git
```powershell
cd c:\Users\ASUS\Desktop\saitest\psy-tests-platform

# Stage all SDJ 7-pattern files
git add backend/PsyApi/Services/Scoring/SevenPatternsMapper.cs
git add backend/PsyApi/Services/Scoring/CourseRecommendationsMapper.cs
git add backend/PsyApi/Services/Reports/HeptagonRadarChartRenderer.cs
git add backend/PsyApi/Services/Reports/ModernSdjSevenPatternReportService.cs
git add backend/PsyApi/Services/Scoring/SdjScoringService.cs
git add backend/PsyApi/Services/Reports/UltimateArabicPdfReportService.cs
git add backend/PsyApi/Resources/Brand/SAITEST-ICON.png
git add backend/PsyApi.Tests/SevenPatternsMapperTests.cs
git add PHASE_SDJ_7PATTERNS_REPORT_COMPLETION.md
git add SDJ_7PATTERNS_QUICK_REFERENCE.md
git add commit_message_sdj_7patterns.txt
```

### Step 2: Commit Changes
```powershell
git commit -F commit_message_sdj_7patterns.txt
```

### Step 3: Push to GitHub
```powershell
# Push to your current branch (develop or main)
git push origin develop

# OR if on main branch:
# git push origin main
```

### Step 4: Wait for Auto-Deployment
- **Render** (Backend): Auto-deploys when it detects the push (~5-10 min)
  - Monitor: https://dashboard.render.com/
  - Logs will show: `dotnet restore` → `dotnet build` → `dotnet run`

- **Vercel** (Frontends): Auto-deploys when it detects the push (~2-5 min each)
  - Monitor: https://vercel.com/dashboard
  - User UI and Admin UI will rebuild automatically

### Step 5: Verify Deployment
```powershell
# Check backend health
curl https://YOUR-API-NAME.onrender.com/health

# Should return:
# {"status":"healthy","database":"connected","sdj":"enabled"}
```

---

## Important Notes

### ✅ What Auto-Deploys
- **Backend (Render)**: Automatically rebuilds and restarts when code changes detected
- **Frontend (Vercel)**: Automatically rebuilds both user-ui and admin-ui

### ⚠️ Environment Variables
Make sure these are set in **Render Dashboard** → Your Service → Environment:

```bash
USE_SDJ=1                          # ← CRITICAL for 7-pattern scoring
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=<Your Neon PostgreSQL>
CORS_ALLOWED_ORIGINS=<Your Vercel URLs>
```

### 🔍 Deployment Logs
**Render Logs** (check for errors):
```
==> Building...
[build] dotnet restore
[build] dotnet build -c Release
[build] Build succeeded.

==> Starting service...
[start] dotnet run --configuration Release
[start] info: Microsoft.Hosting.Lifetime[0]
[start] Now listening on: http://0.0.0.0:10000
```

**Vercel Logs** (should see):
```
Running "npm install"
Running "npm run build"
Build completed
Deployment ready
```

---

## Verification Checklist

After deployment completes, test these in production:

### Backend API Tests
1. ✅ Health endpoint: `GET /health`
2. ✅ Create SDJ test session: `POST /api/sessions` (with USE_SDJ=1)
3. ✅ Submit answers: `POST /api/sessions/{id}/submit`
4. ✅ Generate PDF: `GET /api/results/{id}/pdf?mode=sdj`
5. ✅ Verify PDF contains:
   - Centered SAITEST-ICON.png logo
   - 7 pattern scores with colored badges
   - Heptagon radar chart (7 axes)
   - Weak sub-dimensions table with courses

### Frontend Tests
1. ✅ Admin UI: Create test session (SDJ mode)
2. ✅ User UI: Complete 120-question SDJ assessment
3. ✅ User UI: Download PDF report
4. ✅ Verify Arabic text renders correctly (no � glyphs)
5. ✅ Test backward compatibility with old sessions

---

## Troubleshooting

### Issue: Render Deployment Failed
**Check**: Render deployment logs for build errors
```powershell
# Look for:
# - Missing NuGet packages
# - Compilation errors
# - Runtime errors during startup
```
**Fix**: Ensure all new .cs files are committed and pushed

### Issue: PDF Shows Legacy 5-Dimension Report
**Check**: Is `USE_SDJ=1` set in Render environment variables?
**Check**: Did the session get scored with v2.0_7Patterns?
```bash
# Verify in database:
SELECT "ScoringModelVersion" FROM "Results" WHERE "SessionId" = <id>;
# Should show: "SDJ_v2.0_7Patterns"
```

### Issue: Heptagon Chart Not Rendering
**Check**: Render logs for SkiaSharp errors
**Fix**: SkiaSharp native libraries are included in .NET publish - should work on Render Linux containers

### Issue: Arabic Text Broken (� glyphs)
**Check**: Ensure Noto Naskh Arabic fonts are in `backend/PsyApi/Resources/Fonts/`
**Check**: Fonts are marked as "Copy to Output Directory" in .csproj
```xml
<ItemGroup>
  <None Update="Resources\Fonts\**">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

---

## Rollback Plan

If something goes wrong, rollback with:

```powershell
# 1. Revert the commit locally
git revert HEAD

# 2. Push the revert
git push origin develop

# 3. Wait for auto-redeploy (~5-10 min)
```

Render and Vercel will automatically deploy the reverted version.

---

## Post-Deployment

### Share with Team
1. Generate a sample 7-pattern PDF from production
2. Share the completion report: `PHASE_SDJ_7PATTERNS_REPORT_COMPLETION.md`
3. Update internal documentation with new report format

### Monitor Performance
- **Render Metrics**: Check CPU/Memory usage after deployment
- **Vercel Analytics**: Monitor frontend performance
- **Neon Database**: Check connection pool usage

### User Communication
- Notify users that SDJ reports now include 7 major patterns
- Existing reports remain accessible (backward compatible)
- New assessments automatically use enhanced format

---

## Success Criteria

✅ Backend deployed to Render successfully  
✅ Frontends deployed to Vercel successfully  
✅ Health endpoint returns `{"sdj":"enabled"}`  
✅ 7-pattern PDF generates correctly  
✅ Heptagon chart renders with Arabic labels  
✅ Weak areas table shows courses  
✅ No broken Arabic glyphs  
✅ Backward compatibility maintained  

---

**Total Deployment Time**: ~15 minutes  
**Breaking Changes**: None (fully backward compatible)  
**Database Migrations**: Not required  

🎉 **Ready to Deploy!**
