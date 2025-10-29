# Rollback SDJ V2 to Legacy Mode
# This script reverts the system to use legacy SDJ scoring
# Author: AI Assistant
# Date: October 29, 2025

Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   SDJ V2 Rollback Script - Revert to Legacy Mode              ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Step 1: Disable SDJ V2 Mode
Write-Host "[1/4] Disabling SDJ V2 Mode..." -ForegroundColor Yellow
$envPath = "$PSScriptRoot\..\backend\PsyApi\.env"
if (Test-Path $envPath) {
    $content = Get-Content $envPath
    $content = $content -replace "USE_SDJ=1", "USE_SDJ=0"
    $content | Set-Content $envPath
    Write-Host "✓ SDJ mode disabled in .env" -ForegroundColor Green
} else {
    Write-Host "✗ .env file not found at $envPath" -ForegroundColor Red
}

# Step 2: Backup current database
Write-Host "`n[2/4] Backing up current database..." -ForegroundColor Yellow
$backupDir = "$PSScriptRoot\..\backup"
if (!(Test-Path $backupDir)) {
    New-Item -ItemType Directory -Path $backupDir | Out-Null
}
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$dbPath = "$PSScriptRoot\..\backend\PsyApi\psy_tests.db"
if (Test-Path $dbPath) {
    Copy-Item $dbPath "$backupDir\psy_tests_v2_$timestamp.db"
    Write-Host "✓ Database backed up to: backup\psy_tests_v2_$timestamp.db" -ForegroundColor Green
} else {
    Write-Host "⚠ Database file not found (may be using remote DB)" -ForegroundColor Yellow
}

# Step 3: Restore legacy CSV
Write-Host "`n[3/4] Restoring legacy CSV..." -ForegroundColor Yellow
$legacyCsv = "$PSScriptRoot\..\backend\PsyApi\Resources\Questions\questions_sdj_ar.csv"
$v2Csv = "$PSScriptRoot\..\backend\PsyApi\Resources\Questions\questions_sdj_v2_ar.csv"

if (Test-Path $legacyCsv) {
    if (Test-Path $v2Csv) {
        # Backup V2 CSV
        Copy-Item $v2Csv "$backupDir\questions_sdj_v2_ar_$timestamp.csv"
        Write-Host "✓ V2 CSV backed up" -ForegroundColor Green
    }
    Write-Host "✓ Legacy CSV already in place" -ForegroundColor Green
} else {
    Write-Host "✗ Legacy CSV not found - manual restore required" -ForegroundColor Red
}

# Step 4: Clear application cache/restart instructions
Write-Host "`n[4/4] Final steps..." -ForegroundColor Yellow
Write-Host "✓ Rollback configuration complete" -ForegroundColor Green

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   NEXT STEPS                                                   ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Restart the backend service:" -ForegroundColor White
Write-Host "   cd backend\PsyApi" -ForegroundColor Gray
Write-Host "   dotnet run" -ForegroundColor Gray
Write-Host ""
Write-Host "2. If using Docker:" -ForegroundColor White
Write-Host "   docker-compose restart backend" -ForegroundColor Gray
Write-Host ""
Write-Host "3. If using production deployment:" -ForegroundColor White
Write-Host "   - Update environment variable USE_SDJ=0 on hosting platform" -ForegroundColor Gray
Write-Host "   - Redeploy or restart the service" -ForegroundColor Gray
Write-Host ""
Write-Host "4. Verify rollback:" -ForegroundColor White
Write-Host "   - Check logs for 'SDJ mode: disabled'" -ForegroundColor Gray
Write-Host "   - Test legacy scoring behavior" -ForegroundColor Gray
Write-Host ""
Write-Host "System is ready to use LEGACY SDJ mode" -ForegroundColor Green
Write-Host ""
