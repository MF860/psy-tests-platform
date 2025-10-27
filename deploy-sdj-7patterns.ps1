# SDJ 7-Pattern Deployment Script for Vercel + Render
# Execute these commands from the repository root

# ============================================
# PHASE 1: PRE-DEPLOYMENT CHECKS
# ============================================

Write-Host "`n=== SDJ 7-PATTERN DEPLOYMENT SCRIPT ===" -ForegroundColor Cyan
Write-Host "Target: Vercel (Frontend) + Render (Backend)" -ForegroundColor Cyan
Write-Host "`n"

# Check current branch
Write-Host "[1/10] Checking current branch..." -ForegroundColor Yellow
git branch --show-current

# Ensure we're on develop or main
$currentBranch = git branch --show-current
if ($currentBranch -ne "develop" -and $currentBranch -ne "main") {
    Write-Host "WARNING: Not on develop or main branch. Current: $currentBranch" -ForegroundColor Red
    $continue = Read-Host "Continue anyway? (y/n)"
    if ($continue -ne "y") {
        exit 1
    }
}

# Check git status
Write-Host "`n[2/10] Checking git status..." -ForegroundColor Yellow
git status --short

# ============================================
# PHASE 2: STAGE ALL SDJ 7-PATTERN FILES
# ============================================

Write-Host "`n[3/10] Staging new files..." -ForegroundColor Yellow

# Stage new service files
git add backend/PsyApi/Services/Scoring/SevenPatternsMapper.cs
git add backend/PsyApi/Services/Scoring/CourseRecommendationsMapper.cs
git add backend/PsyApi/Services/Reports/HeptagonRadarChartRenderer.cs
git add backend/PsyApi/Services/Reports/ModernSdjSevenPatternReportService.cs

# Stage modified service files
git add backend/PsyApi/Services/Scoring/SdjScoringService.cs
git add backend/PsyApi/Services/Reports/UltimateArabicPdfReportService.cs

# Stage new assets
git add backend/PsyApi/Resources/Brand/SAITEST-ICON.png

# Stage tests
git add backend/PsyApi.Tests/SevenPatternsMapperTests.cs

# Stage documentation
git add PHASE_SDJ_7PATTERNS_REPORT_COMPLETION.md
git add SDJ_7PATTERNS_QUICK_REFERENCE.md
git add commit_message_sdj_7patterns.txt

# Optional: Stage Docker deployment guide if it's related
# git add DOCKER_DEPLOYMENT_GUIDE.md

Write-Host "✓ Files staged successfully" -ForegroundColor Green

# ============================================
# PHASE 3: VERIFY STAGED FILES
# ============================================

Write-Host "`n[4/10] Verifying staged files..." -ForegroundColor Yellow
git diff --cached --name-only

# ============================================
# PHASE 4: COMMIT CHANGES
# ============================================

Write-Host "`n[5/10] Creating commit..." -ForegroundColor Yellow

# Use the pre-written commit message file
git commit -F commit_message_sdj_7patterns.txt

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Git commit failed!" -ForegroundColor Red
    exit 1
}

Write-Host "✓ Commit created successfully" -ForegroundColor Green

# ============================================
# PHASE 5: PUSH TO REMOTE
# ============================================

Write-Host "`n[6/10] Pushing to remote repository..." -ForegroundColor Yellow

# Push to current branch
git push origin $currentBranch

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Git push failed!" -ForegroundColor Red
    Write-Host "Run manually: git push origin $currentBranch" -ForegroundColor Yellow
    exit 1
}

Write-Host "✓ Pushed to origin/$currentBranch" -ForegroundColor Green

# ============================================
# PHASE 6: RENDER DEPLOYMENT (AUTO-DEPLOYS)
# ============================================

Write-Host "`n[7/10] Backend Deployment (Render)..." -ForegroundColor Yellow
Write-Host "Render will auto-deploy when it detects the push to $currentBranch" -ForegroundColor Cyan
Write-Host "Expected deployment time: 5-10 minutes" -ForegroundColor Cyan
Write-Host "`nMonitor deployment at: https://dashboard.render.com/" -ForegroundColor White

Write-Host "`nRender will execute:" -ForegroundColor Gray
Write-Host "  1. dotnet restore" -ForegroundColor Gray
Write-Host "  2. dotnet build -c Release" -ForegroundColor Gray
Write-Host "  3. dotnet run --configuration Release" -ForegroundColor Gray

# ============================================
# PHASE 7: VERCEL DEPLOYMENT (AUTO-DEPLOYS)
# ============================================

Write-Host "`n[8/10] Frontend Deployment (Vercel)..." -ForegroundColor Yellow
Write-Host "Vercel will auto-deploy frontends when it detects the push" -ForegroundColor Cyan
Write-Host "Expected deployment time: 2-5 minutes per frontend" -ForegroundColor Cyan
Write-Host "`nMonitor deployments at: https://vercel.com/dashboard" -ForegroundColor White

Write-Host "`nVercel will execute:" -ForegroundColor Gray
Write-Host "  User UI: npm install; npm run build" -ForegroundColor Gray
Write-Host "  Admin UI: npm install; npm run build" -ForegroundColor Gray

# ============================================
# PHASE 8: POST-DEPLOYMENT VERIFICATION
# ============================================

Write-Host "`n[9/10] Post-Deployment Verification Steps..." -ForegroundColor Yellow

Write-Host "`n📋 MANUAL VERIFICATION CHECKLIST:" -ForegroundColor Cyan
Write-Host "  [ ] 1. Check Render deployment status: https://dashboard.render.com/" -ForegroundColor White
Write-Host "  [ ] 2. Verify backend health: https://YOUR-API.onrender.com/health" -ForegroundColor White
Write-Host "  [ ] 3. Check Vercel deployments: https://vercel.com/dashboard" -ForegroundColor White
Write-Host "  [ ] 4. Test SDJ session creation via Admin UI" -ForegroundColor White
Write-Host "  [ ] 5. Complete SDJ test session (120 questions)" -ForegroundColor White
Write-Host "  [ ] 6. Generate PDF report - verify 7 patterns appear" -ForegroundColor White
Write-Host "  [ ] 7. Verify heptagon chart renders correctly" -ForegroundColor White
Write-Host "  [ ] 8. Check weak sub-dimensions table with courses" -ForegroundColor White
Write-Host "  [ ] 9. Verify Arabic text (no broken glyphs)" -ForegroundColor White
Write-Host "  [ ] 10. Test backward compatibility (old sessions)" -ForegroundColor White

# ============================================
# PHASE 9: ENVIRONMENT VARIABLE CHECK
# ============================================

Write-Host "`n[10/10] Environment Variables Reminder..." -ForegroundColor Yellow

Write-Host "`n⚙️  RENDER ENVIRONMENT VARIABLES (verify these are set):" -ForegroundColor Cyan
Write-Host "  USE_SDJ=1 (CRITICAL - enables 7-pattern scoring)" -ForegroundColor White
Write-Host "  ASPNETCORE_ENVIRONMENT=Production" -ForegroundColor White
Write-Host "  ConnectionStrings__DefaultConnection=<Neon PostgreSQL>" -ForegroundColor White
Write-Host "  CORS_ALLOWED_ORIGINS=<Your Vercel URLs>" -ForegroundColor White

Write-Host "`nNo frontend environment changes needed (backend-only feature)" -ForegroundColor Green

# ============================================
# PHASE 10: SUCCESS SUMMARY
# ============================================

Write-Host "`n" -NoNewline
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "✅ SDJ 7-PATTERN DEPLOYMENT INITIATED SUCCESSFULLY" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan

Write-Host "`n📊 DEPLOYMENT SUMMARY:" -ForegroundColor Yellow
Write-Host "  Branch: $currentBranch" -ForegroundColor White
Write-Host "  Commit: feat(sdj): 7-Pattern Overhaul" -ForegroundColor White
Write-Host "  Backend: Render (auto-deploying)" -ForegroundColor White
Write-Host "  Frontend: Vercel (auto-deploying)" -ForegroundColor White
Write-Host "  Database: Neon (no migrations needed)" -ForegroundColor White

Write-Host "`n🔗 USEFUL LINKS:" -ForegroundColor Yellow
Write-Host "  Render Dashboard: https://dashboard.render.com/" -ForegroundColor White
Write-Host "  Vercel Dashboard: https://vercel.com/dashboard" -ForegroundColor White
Write-Host "  Neon Console: https://console.neon.tech/" -ForegroundColor White
Write-Host "  Completion Doc: PHASE_SDJ_7PATTERNS_REPORT_COMPLETION.md" -ForegroundColor White

Write-Host "`n⏱️  ESTIMATED DEPLOYMENT TIME:" -ForegroundColor Yellow
Write-Host "  Backend (Render): 5-10 minutes" -ForegroundColor White
Write-Host "  Frontend (Vercel): 2-5 minutes each" -ForegroundColor White
Write-Host "  Total: ~10-15 minutes" -ForegroundColor White

Write-Host "`n🎯 NEXT STEPS:" -ForegroundColor Yellow
Write-Host "  1. Wait for deployments to complete" -ForegroundColor White
Write-Host "  2. Run verification checklist above" -ForegroundColor White
Write-Host "  3. Generate test PDF with 7 patterns" -ForegroundColor White
Write-Host "  4. Share report with stakeholders" -ForegroundColor White

Write-Host "`n✨ SDJ 7-Pattern Report is now deploying to production!" -ForegroundColor Green
Write-Host "`n"
