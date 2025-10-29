# SDJ V2 Emergency Deployment Script
# Forces Render to rebuild and apply SDJ V2 migration
# Run this script to trigger deployment

Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  SDJ V2 Emergency Deployment Trigger" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Step 1: Verify we're on develop branch
Write-Host "[1/5] Checking Git status..." -ForegroundColor Yellow
$branch = git branch --show-current
if ($branch -ne "develop") {
    Write-Host "  Warning: Not on develop branch. Current: $branch" -ForegroundColor Red
    exit 1
}
Write-Host "  Current branch: develop" -ForegroundColor Green

# Step 2: Verify latest commit is pushed
Write-Host ""
Write-Host "[2/5] Verifying latest commit is pushed..." -ForegroundColor Yellow
$localCommit = git rev-parse HEAD
$remoteCommit = git rev-parse origin/develop
if ($localCommit -eq $remoteCommit) {
    Write-Host "  Local and remote are in sync: $($localCommit.Substring(0,7))" -ForegroundColor Green
} else {
    Write-Host "  WARNING: Local and remote are out of sync!" -ForegroundColor Red
    Write-Host "  Local:  $($localCommit.Substring(0,7))" -ForegroundColor Gray
    Write-Host "  Remote: $($remoteCommit.Substring(0,7))" -ForegroundColor Gray
    exit 1
}

# Step 3: Show latest commit details
Write-Host ""
Write-Host "[3/5] Latest commit details:" -ForegroundColor Yellow
git log -1 --oneline
$commitMsg = git log -1 --pretty=%B
if ($commitMsg -like "*sdj-v2*" -or $commitMsg -like "*seven patterns*") {
    Write-Host "  SDJ V2 commit confirmed!" -ForegroundColor Green
} else {
    Write-Host "  WARNING: Latest commit doesn't mention SDJ V2" -ForegroundColor Yellow
}

# Step 4: Create deployment marker file
Write-Host ""
Write-Host "[4/5] Creating deployment marker..." -ForegroundColor Yellow
$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
$marker = @"
SDJ V2 Deployment Marker
========================
Timestamp: $timestamp
Commit: $localCommit
Branch: develop
Status: READY FOR DEPLOYMENT

This file triggers Render auto-deploy.
Delete this file after successful deployment.
"@
Set-Content -Path "DEPLOY_SDJ_V2.txt" -Value $marker
Write-Host "  Marker file created: DEPLOY_SDJ_V2.txt" -ForegroundColor Green

# Step 5: Commit and push the marker
Write-Host ""
Write-Host "[5/5] Pushing deployment trigger..." -ForegroundColor Yellow
git add DEPLOY_SDJ_V2.txt
git commit -m "deploy: trigger SDJ V2 deployment to Render"
git push origin develop

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host "  DEPLOYMENT TRIGGERED SUCCESSFULLY!" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Go to Render Dashboard: https://dashboard.render.com" -ForegroundColor White
Write-Host "2. Select: psy-tests-backend service" -ForegroundColor White
Write-Host "3. Monitor deployment progress (5-10 minutes)" -ForegroundColor White
Write-Host "4. Check logs for:" -ForegroundColor White
Write-Host "   - 'Applying migration 20251029143657_SDJ_V2_SevenPatterns'" -ForegroundColor Gray
Write-Host "   - '[SDJ V2] Using CSV file: questions_sdj_v2_ar.csv'" -ForegroundColor Gray
Write-Host "   - 'Post-seed verification passed: 210 items'" -ForegroundColor Gray
Write-Host ""
Write-Host "If deployment doesn't auto-trigger:" -ForegroundColor Yellow
Write-Host "1. Go to Render service settings" -ForegroundColor White
Write-Host "2. Click 'Manual Deploy' > 'Deploy latest commit'" -ForegroundColor White
Write-Host "3. Select branch: develop" -ForegroundColor White
Write-Host ""
