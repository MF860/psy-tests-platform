# Quick Neon Backend Test (with fresh schema)
Write-Host "=== Neon PostgreSQL Quick Test ===" -ForegroundColor Cyan
Write-Host ""

# Set environment variables
$env:ConnectionStrings__DefaultConnection = "Host=ep-holy-glitter-a40pexde-pooler.us-east-1.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=npg_OUubznM7eZW0;SSL Mode=Require;Trust Server Certificate=true"
$env:USE_SQLITE = "0"
$env:USE_SDJ = "1"
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = "http://localhost:5019"
$env:NEON_RESET_DB = "1"  # Signal to reset database

Write-Host "WARNING: This will DROP and RECREATE the database schema!" -ForegroundColor Yellow
Write-Host "All existing data will be lost." -ForegroundColor Yellow
Write-Host ""
$confirmation = Read-Host "Type 'YES' to continue"

if ($confirmation -ne "YES") {
    Write-Host "Cancelled." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Starting backend with database reset..." -ForegroundColor Cyan
Write-Host ""

Set-Location -Path "$PSScriptRoot\backend\PsyApi"
dotnet run
