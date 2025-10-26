# 🧪 Quick Test Script for Refined PDF Reports
# Date: 2025-10-14
# Purpose: Test the refined Arabic PDF report system

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🧪 QUICK TEST - Refined Arabic PDF Report v3.1" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# 1. Check if logo exists
Write-Host "📸 Checking logo files..." -ForegroundColor Yellow
$logoPath = ".\Resources\Brand\SAITEST.jpeg"
$fallbackLogo = ".\Resources\Brand\SITES-ICON.png"

if (Test-Path $logoPath) {
    Write-Host "   ✅ SAITEST.jpeg found" -ForegroundColor Green
} elseif (Test-Path $fallbackLogo) {
    Write-Host "   ⚠️  SAITEST.jpeg not found, using fallback: SITES-ICON.png" -ForegroundColor Yellow
} else {
    Write-Host "   ❌ No logo found (will work without logo)" -ForegroundColor Red
}

# 2. Check fonts
Write-Host ""
Write-Host "🔤 Checking Arabic fonts..." -ForegroundColor Yellow
$regularFont = ".\Resources\Fonts\NotoNaskhArabic-Regular.ttf"
$boldFont = ".\Resources\Fonts\NotoNaskhArabic-Bold.ttf"

if ((Test-Path $regularFont) -and (Test-Path $boldFont)) {
    Write-Host "   ✅ Noto Naskh Arabic fonts found" -ForegroundColor Green
} else {
    Write-Host "   ❌ Arabic fonts missing!" -ForegroundColor Red
    exit 1
}

# 3. Build the project
Write-Host ""
Write-Host "🔨 Building project..." -ForegroundColor Yellow
$buildResult = dotnet build --configuration Release 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✅ Build successful" -ForegroundColor Green
} else {
    Write-Host "   ❌ Build failed!" -ForegroundColor Red
    Write-Host $buildResult -ForegroundColor Red
    exit 1
}

# 4. Start the server
Write-Host ""
Write-Host "🚀 Starting server..." -ForegroundColor Yellow
Write-Host "   To test the PDF report:" -ForegroundColor Cyan
Write-Host ""
Write-Host "   1. Wait for server to start (watch for 'Now listening on...')" -ForegroundColor White
Write-Host "   2. Open browser or use curl:" -ForegroundColor White
Write-Host ""
Write-Host "      GET https://localhost:5001/api/results/{resultId}/pdf" -ForegroundColor Yellow
Write-Host ""
Write-Host "   3. Check the PDF for:" -ForegroundColor White
Write-Host "      ✅ Centered logo (80px width)" -ForegroundColor Gray
Write-Host "      ✅ Title: 'منصة التحليل النفسي المتقدم' (22pt bold)" -ForegroundColor Gray
Write-Host "      ✅ Larger charts (Radar 550px, Donut 350px)" -ForegroundColor Gray
Write-Host "      ✅ Clean Arabic text (no � symbols)" -ForegroundColor Gray
Write-Host "      ✅ Footer on all pages: 'تم إنشاء هذا التقرير...' (9pt gray)" -ForegroundColor Gray
Write-Host "      ✅ Light gray background on charts page (#F9FAFB)" -ForegroundColor Gray
Write-Host "      ✅ Subtle dividers between charts" -ForegroundColor Gray
Write-Host ""
Write-Host "   Press Ctrl+C to stop the server" -ForegroundColor White
Write-Host ""
Write-Host "───────────────────────────────────────────────────────" -ForegroundColor Cyan

# Start the server
dotnet run
