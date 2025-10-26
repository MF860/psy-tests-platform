# Reset Neon Database and Start Backend
Write-Host "=== Neon Database Reset and Backend Start ===" -ForegroundColor Cyan
Write-Host ""

# Connection details
$connString = "Host=ep-holy-glitter-a40pexde-pooler.us-east-1.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=npg_OUubznM7eZW0;SSL Mode=Require;Trust Server Certificate=true"

Write-Host "Step 1: Resetting Neon database schema..." -ForegroundColor Yellow
Write-Host "This will drop all tables and data for a fresh start" -ForegroundColor Gray
Write-Host ""

# Use psql if available, otherwise provide instructions
$psqlAvailable = Get-Command psql -ErrorAction SilentlyContinue

if ($psqlAvailable) {
    Write-Host "Executing reset script with psql..." -ForegroundColor Gray
    $env:PGPASSWORD = "npg_OUubznM7eZW0"
    psql -h ep-holy-glitter-a40pexde-pooler.us-east-1.aws.neon.tech `
         -U neondb_owner `
         -d neondb `
         -f "$PSScriptRoot\backend\PsyApi\reset_neon_db.sql"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Database reset successful!" -ForegroundColor Green
    } else {
        Write-Host "Database reset failed. Continuing anyway..." -ForegroundColor Yellow
    }
} else {
    Write-Host "psql not found. Please run this SQL manually in Neon console:" -ForegroundColor Yellow
    Write-Host ""
    Get-Content "$PSScriptRoot\backend\PsyApi\reset_neon_db.sql" | Write-Host -ForegroundColor Gray
    Write-Host ""
    Write-Host "After running the SQL, press Enter to continue..." -ForegroundColor Yellow
    Read-Host
}

Write-Host ""
Write-Host "Step 2: Starting backend with Neon PostgreSQL..." -ForegroundColor Yellow
Write-Host ""

# Set environment variables
$env:ConnectionStrings__DefaultConnection = $connString
$env:USE_SQLITE = "0"
$env:USE_SDJ = "1"
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = "http://localhost:5019"

Write-Host "Environment configured:" -ForegroundColor Green
Write-Host "  - Database: Neon PostgreSQL" -ForegroundColor Gray
Write-Host "  - USE_SDJ: 1 (120 SDJ questions)" -ForegroundColor Gray
Write-Host "  - Server: http://localhost:5019" -ForegroundColor Gray
Write-Host ""

# Navigate and run
Set-Location -Path "$PSScriptRoot\backend\PsyApi"
Write-Host "Starting backend..." -ForegroundColor Cyan
Write-Host "Watch for: 'Auto-detected PostgreSQL' and 'Seeding 120 SDJ items'" -ForegroundColor Gray
Write-Host ""

dotnet run
