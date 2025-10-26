# 🧪 vNext Report Testing Script
# Date: 2025-10-14
# Purpose: Test the vNext improvements (v3.2)

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🧪 QUICK TEST - Report vNext v3.2" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

Write-Host "📋 What's New in vNext:" -ForegroundColor Yellow
Write-Host "   ✅ Logo header (66px) + title (20pt)" -ForegroundColor Green
Write-Host "   ✅ NEW Horizontal Bar Chart with HarfBuzz" -ForegroundColor Green
Write-Host "   ✅ Enlarged charts (Radar 400px, Donut 160px, Bars 580px)" -ForegroundColor Green
Write-Host "   ✅ NO � symbols (full HarfBuzz support)" -ForegroundColor Green
Write-Host "   ✅ Western digits everywhere" -ForegroundColor Green
Write-Host "   ✅ Improved typography (16pt/12pt/11pt)" -ForegroundColor Green
Write-Host "   ✅ White cards on gray background" -ForegroundColor Green
Write-Host ""

# 1. Check assets
Write-Host "📁 Checking assets..." -ForegroundColor Yellow

$logo1 = ".\Resources\Brand\SAITES-ICON.png"
$logo2 = ".\Resources\Brand\SAITEST.jpeg"
$logo3 = ".\Resources\Brand\SITES-ICON.png"

if (Test-Path $logo1) {
    Write-Host "   ✅ SAITES-ICON.png found (primary)" -ForegroundColor Green
} elseif (Test-Path $logo2) {
    Write-Host "   ⚠️  Using SAITEST.jpeg (fallback)" -ForegroundColor Yellow
} elseif (Test-Path $logo3) {
    Write-Host "   ⚠️  Using SITES-ICON.png (fallback)" -ForegroundColor Yellow
} else {
    Write-Host "   ℹ️  No logo (will work without it)" -ForegroundColor Gray
}

$regularFont = ".\Resources\Fonts\NotoNaskhArabic-Regular.ttf"
$boldFont = ".\Resources\Fonts\NotoNaskhArabic-Bold.ttf"

if ((Test-Path $regularFont) -and (Test-Path $boldFont)) {
    Write-Host "   ✅ Noto Naskh Arabic fonts found" -ForegroundColor Green
} else {
    Write-Host "   ❌ Arabic fonts missing!" -ForegroundColor Red
    exit 1
}

# 2. Build
Write-Host ""
Write-Host "🔨 Building project..." -ForegroundColor Yellow
$buildResult = dotnet build --configuration Release --no-incremental 2>&1 | Out-String
if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✅ Build successful" -ForegroundColor Green
} else {
    Write-Host "   ❌ Build failed!" -ForegroundColor Red
    Write-Host $buildResult -ForegroundColor Red
    exit 1
}

# 3. Check for new renderer
Write-Host ""
Write-Host "🔍 Verifying new components..." -ForegroundColor Yellow
$newRenderer = ".\Services\Reports\HorizontalBarChartRenderer.cs"
if (Test-Path $newRenderer) {
    Write-Host "   ✅ HorizontalBarChartRenderer.cs present" -ForegroundColor Green
} else {
    Write-Host "   ❌ New renderer missing!" -ForegroundColor Red
    exit 1
}

# 4. Instructions
Write-Host ""
Write-Host "🚀 Starting server..." -ForegroundColor Yellow
Write-Host ""
Write-Host "───────────────────────────────────────────────────────" -ForegroundColor Cyan
Write-Host ""
Write-Host "📝 Testing Checklist:" -ForegroundColor White
Write-Host ""
Write-Host "   1. Wait for server to start" -ForegroundColor Gray
Write-Host "   2. Generate PDF:" -ForegroundColor Gray
Write-Host ""
Write-Host "      GET https://localhost:5001/api/results/{id}/pdf" -ForegroundColor Yellow
Write-Host ""
Write-Host "   3. Verify in PDF:" -ForegroundColor Gray
Write-Host ""
Write-Host "      Page 1:" -ForegroundColor White
Write-Host "      □ Logo centered (66px width)" -ForegroundColor Gray
Write-Host "      □ Title 'منصة التحليل النفسي المتقدم' (20pt Bold)" -ForegroundColor Gray
Write-Host "      □ Spacing: 8pt → 12pt → 16pt" -ForegroundColor Gray
Write-Host "      □ No subtitle" -ForegroundColor Gray
Write-Host ""
Write-Host "      Page 2 (vNext Improvements):" -ForegroundColor White
Write-Host "      □ Light gray background (#F9FAFB)" -ForegroundColor Gray
Write-Host "      □ White cards with 20pt padding" -ForegroundColor Gray
Write-Host "      □ Radar chart (~400px)" -ForegroundColor Gray
Write-Host "      □ NEW Horizontal bars (580px wide):" -ForegroundColor Green
Write-Host "         • Title: 'ترتيب الأبعاد تصاعدياً حسب T-Score' (16pt)" -ForegroundColor Gray
Write-Host "         • Legend with colors (Red/Orange/Green)" -ForegroundColor Gray
Write-Host "         • RTL dimension labels (12pt)" -ForegroundColor Gray
Write-Host "         • T-values at bar end: 'T=xx.x' (11pt)" -ForegroundColor Gray
Write-Host "         • Weakest dimensions first" -ForegroundColor Gray
Write-Host "         • NO � symbols" -ForegroundColor Green
Write-Host "      □ Donut chart (160px)" -ForegroundColor Gray
Write-Host "      □ Dividers between charts" -ForegroundColor Gray
Write-Host ""
Write-Host "   4. Console Messages to Watch:" -ForegroundColor Gray
Write-Host "      '✓ Logo loaded: SAITES-ICON.png'" -ForegroundColor Gray
Write-Host "      '✓ Noto Naskh Arabic + HarfBuzz ready'" -ForegroundColor Gray
Write-Host "      '✓ HorizontalBarChart ... ready'" -ForegroundColor Gray
Write-Host ""
Write-Host "   Use Ctrl+C to stop server" -ForegroundColor White
Write-Host ""
Write-Host "───────────────────────────────────────────────────────" -ForegroundColor Cyan
Write-Host ""

# Start server
dotnet run
