# Direct PDF Download Test - Bypass Frontend
# This script downloads PDF directly from Render API to verify Ultra Hi-Fi format

param(
    [string]$ResultId = "46",
    [string]$AdminUsername = "root",
    [string]$AdminPassword = "StrongAdmin!23!"
)

$BaseUrl = "https://psy-api-backend.onrender.com"

Write-Host "`n$('=' * 80)" -ForegroundColor Cyan
Write-Host "  DIRECT PDF DOWNLOAD TEST" -ForegroundColor Yellow
Write-Host "$('=' * 80)`n" -ForegroundColor Cyan

# Step 1: Admin Login
Write-Host "[1/3] Logging in as admin..." -ForegroundColor Yellow

$loginBody = @{
    username = $AdminUsername
    password = $AdminPassword
} | ConvertTo-Json

try {
    $authResponse = Invoke-RestMethod -Method POST -Uri "$BaseUrl/api/auth/admin/login" -Body $loginBody -ContentType "application/json" -TimeoutSec 30
    $token = $authResponse.token
    Write-Host "✅ Admin logged in successfully" -ForegroundColor Green
    Write-Host "   Token: $($token.Substring(0, 20))...`n" -ForegroundColor Gray
}
catch {
    Write-Host "❌ Admin login failed: $_" -ForegroundColor Red
    exit 1
}

# Step 2: Download PDF
Write-Host "[2/3] Downloading PDF for Result ID $ResultId..." -ForegroundColor Yellow
Write-Host "   URL: $BaseUrl/api/admin/results/$ResultId/pdf" -ForegroundColor Gray

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$pdfPath = Join-Path $PSScriptRoot "Direct_Download_Result${ResultId}_$timestamp.pdf"

$headers = @{
    "Authorization" = "Bearer $token"
    "Accept" = "application/pdf"
}

try {
    Invoke-RestMethod -Method GET -Uri "$BaseUrl/api/admin/results/$ResultId/pdf" -Headers $headers -OutFile $pdfPath -TimeoutSec 120
    
    $fileSize = (Get-Item $pdfPath).Length
    $fileSizeKB = [Math]::Round($fileSize / 1KB, 2)
    
    Write-Host "✅ PDF downloaded successfully" -ForegroundColor Green
    Write-Host "   File: $(Split-Path $pdfPath -Leaf)" -ForegroundColor Gray
    Write-Host "   Size: $fileSizeKB KB" -ForegroundColor Gray
    Write-Host "   Path: $pdfPath`n" -ForegroundColor Gray
}
catch {
    Write-Host "❌ PDF download failed: $_" -ForegroundColor Red
    if ($_.ErrorDetails.Message) {
        Write-Host "   Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
    exit 1
}

# Step 3: Open PDF
Write-Host "[3/3] Opening PDF..." -ForegroundColor Yellow

try {
    Start-Process $pdfPath
    Write-Host "✅ PDF opened in default viewer`n" -ForegroundColor Green
}
catch {
    Write-Host "⚠️  Could not auto-open PDF. Please open manually: $pdfPath`n" -ForegroundColor Yellow
}

# Summary
Write-Host "$('=' * 80)" -ForegroundColor Green
Write-Host "  ✅ DIRECT DOWNLOAD COMPLETED" -ForegroundColor Green
Write-Host "$('=' * 80)`n" -ForegroundColor Green

Write-Host "VERIFICATION CHECKLIST:" -ForegroundColor Cyan
Write-Host "  [ ] PDF has 6 pages" -ForegroundColor White
Write-Host "  [ ] Shows Ultra Hi-Fi design (gradients, glass morphism)" -ForegroundColor White
Write-Host "  [ ] Has 7-pattern heptagon radar chart" -ForegroundColor White
Write-Host "  [ ] Charts are 450 DPI quality (crisp, sharp)" -ForegroundColor White
Write-Host "  [ ] Arabic text perfect (no � symbols)" -ForegroundColor White
Write-Host "  [ ] Theme: Aurora Glass or Noir Executive" -ForegroundColor White
Write-Host "`n"

Write-Host "If PDF is STILL OLD FORMAT:" -ForegroundColor Yellow
Write-Host "  1. Check Render deployment logs for errors" -ForegroundColor White
Write-Host "  2. Verify commit e704eac is deployed" -ForegroundColor White
Write-Host "  3. Check Render Events tab for deploy timestamp" -ForegroundColor White
Write-Host "  4. Try restarting Render service manually" -ForegroundColor White
Write-Host "`n"
