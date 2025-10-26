# 🎯 Installation Script - Platform Modernization
# Run this script to install all dependencies

Write-Host "🚀 Starting Platform Modernization Setup..." -ForegroundColor Cyan
Write-Host ""

$ErrorActionPreference = "Continue"

# Check if Node.js is installed
Write-Host "Checking Node.js installation..." -ForegroundColor Yellow
$nodeVersion = node --version 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Node.js is not installed. Please install Node.js 18+ from https://nodejs.org" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Node.js $nodeVersion detected" -ForegroundColor Green
Write-Host ""

# Check if .NET is installed
Write-Host "Checking .NET installation..." -ForegroundColor Yellow
$dotnetVersion = dotnet --version 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ .NET is not installed. Please install .NET 8.0 SDK from https://dotnet.microsoft.com" -ForegroundColor Red
    exit 1
}
Write-Host "✅ .NET $dotnetVersion detected" -ForegroundColor Green
Write-Host ""

# Admin UI Setup
Write-Host "📦 Installing Admin UI dependencies..." -ForegroundColor Cyan
Set-Location "$PSScriptRoot\frontend\admin-ui"
if (Test-Path "package.json") {
    npm install
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Admin UI dependencies installed successfully" -ForegroundColor Green
    } else {
        Write-Host "⚠️  Admin UI installation completed with warnings" -ForegroundColor Yellow
    }
} else {
    Write-Host "❌ Admin UI package.json not found" -ForegroundColor Red
}
Write-Host ""

# User UI Setup
Write-Host "📦 Installing User UI dependencies..." -ForegroundColor Cyan
Set-Location "$PSScriptRoot\frontend\user-ui"
if (Test-Path "package.json") {
    npm install
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ User UI dependencies installed successfully" -ForegroundColor Green
    } else {
        Write-Host "⚠️  User UI installation completed with warnings" -ForegroundColor Yellow
    }
} else {
    Write-Host "❌ User UI package.json not found" -ForegroundColor Red
}
Write-Host ""

# Backend Setup
Write-Host "📦 Installing Backend dependencies..." -ForegroundColor Cyan
Set-Location "$PSScriptRoot\backend\PsyApi"
if (Test-Path "PsyApi.csproj") {
    # Install QuestPDF for PDF generation
    dotnet add package QuestPDF --version 2024.3.0
    dotnet restore
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Backend dependencies installed successfully" -ForegroundColor Green
    } else {
        Write-Host "⚠️  Backend installation completed with warnings" -ForegroundColor Yellow
    }
} else {
    Write-Host "❌ Backend PsyApi.csproj not found" -ForegroundColor Red
}
Write-Host ""

# Create necessary directories
Write-Host "📁 Creating necessary directories..." -ForegroundColor Cyan
Set-Location $PSScriptRoot

$directories = @(
    "frontend\admin-ui\src\components\ai",
    "frontend\admin-ui\src\components\charts",
    "frontend\admin-ui\src\assets\lottie",
    "frontend\user-ui\src\assets\lottie",
    "backend\PsyApi\Services\Reports"
)

foreach ($dir in $directories) {
    if (!(Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
        Write-Host "✅ Created $dir" -ForegroundColor Green
    }
}
Write-Host ""

# Summary
Write-Host "═══════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🎉 Installation Complete!" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "📋 Next Steps:" -ForegroundColor Yellow
Write-Host "1. Review QUICK_START.md for integration instructions"
Write-Host "2. Update main.tsx files with ThemeProvider"
Write-Host "3. Download Lottie animations from https://lottiefiles.com"
Write-Host "4. Test the applications:"
Write-Host "   - Backend: cd backend\PsyApi; dotnet run"
Write-Host "   - Admin UI: cd frontend\admin-ui; npm run dev"
Write-Host "   - User UI: cd frontend\user-ui; npm run dev"
Write-Host ""
Write-Host "📚 Documentation:" -ForegroundColor Yellow
Write-Host "   - MODERNIZATION_PLAN.md - Full implementation guide"
Write-Host "   - QUICK_START.md - Quick setup instructions"
Write-Host ""
Write-Host "✨ Happy coding!" -ForegroundColor Magenta
