# Neon Database Connection Verification Script
Write-Host "=== Neon PostgreSQL Connection Test ===" -ForegroundColor Cyan
Write-Host ""

# Try connecting to default 'postgres' database first (always exists)
$env:PGPASSWORD = "npg_OUubznM7eZW0"

Write-Host "Step 1: Testing connection to Neon..." -ForegroundColor Yellow
Write-Host "Host: ep-holy-glitter-a40pexde-pooler.us-east-1.aws.neon.tech" -ForegroundColor Gray
Write-Host "Username: neondb_owner" -ForegroundColor Gray
Write-Host ""

# Check if psql is available
$psqlAvailable = Get-Command psql -ErrorAction SilentlyContinue

if (-not $psqlAvailable) {
    Write-Host "psql not found. Installing via Chocolatey or manual instructions..." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "To install psql on Windows:" -ForegroundColor Cyan
    Write-Host "1. Option A: Install via Chocolatey" -ForegroundColor Gray
    Write-Host "   choco install postgresql" -ForegroundColor Gray
    Write-Host ""
    Write-Host "2. Option B: Download from PostgreSQL.org" -ForegroundColor Gray
    Write-Host "   https://www.postgresql.org/download/windows/" -ForegroundColor Gray
    Write-Host ""
    Write-Host "3. Option C: Use Neon SQL Editor in browser" -ForegroundColor Gray
    Write-Host "   https://console.neon.tech" -ForegroundColor Gray
    Write-Host ""
    
    Write-Host "Manual verification steps:" -ForegroundColor Yellow
    Write-Host "1. Go to https://console.neon.tech" -ForegroundColor Gray
    Write-Host "2. Click on your project" -ForegroundColor Gray
    Write-Host "3. Go to SQL Editor" -ForegroundColor Gray
    Write-Host "4. Run: SELECT current_database(), version();" -ForegroundColor Gray
    Write-Host "5. Check what databases exist: \list" -ForegroundColor Gray
    Write-Host ""
    exit 0
}

Write-Host "Testing connection to 'postgres' database (default)..." -ForegroundColor Gray
psql "postgresql://neondb_owner:npg_OUubznM7eZW0@ep-holy-glitter-a40pexde-pooler.us-east-1.aws.neon.tech/postgres?sslmode=require" -c "SELECT current_database(), version();"

if ($LASTEXITCODE -eq 0) {
    Write-Host "Connection successful!" -ForegroundColor Green
    Write-Host ""
    
    Write-Host "Step 2: Checking available databases..." -ForegroundColor Yellow
    psql "postgresql://neondb_owner:npg_OUubznM7eZW0@ep-holy-glitter-a40pexde-pooler.us-east-1.aws.neon.tech/postgres?sslmode=require" -c "\l"
    
    Write-Host ""
    Write-Host "Step 3: Creating 'neondb' database if it doesn't exist..." -ForegroundColor Yellow
    psql "postgresql://neondb_owner:npg_OUubznM7eZW0@ep-holy-glitter-a40pexde-pooler.us-east-1.aws.neon.tech/postgres?sslmode=require" -c "CREATE DATABASE neondb OWNER neondb_owner;"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Database 'neondb' created successfully!" -ForegroundColor Green
    } else {
        Write-Host "Database 'neondb' may already exist (this is OK)" -ForegroundColor Yellow
    }
    
    Write-Host ""
    Write-Host "Step 4: Verifying 'neondb' database exists..." -ForegroundColor Yellow
    psql "postgresql://neondb_owner:npg_OUubznM7eZW0@ep-holy-glitter-a40pexde-pooler.us-east-1.aws.neon.tech/neondb?sslmode=require" -c "SELECT current_database();"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "SUCCESS! Backend can now connect to 'neondb'" -ForegroundColor Green
        Write-Host ""
        Write-Host "Updated connection string:" -ForegroundColor Cyan
        Write-Host "Host=ep-holy-glitter-a40pexde-pooler.us-east-1.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=npg_OUubznM7eZW0;SSL Mode=Require;Trust Server Certificate=true" -ForegroundColor Gray
    } else {
        Write-Host "Failed to verify 'neondb' database" -ForegroundColor Red
    }
} else {
    Write-Host "Connection failed!" -ForegroundColor Red
    Write-Host "Please verify your Neon credentials in the Neon console" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "To test backend connection, run:" -ForegroundColor Cyan
Write-Host 'cd backend/PsyApi ; $env:ConnectionStrings__DefaultConnection = "Host=ep-holy-glitter-a40pexde-pooler.us-east-1.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=npg_OUubznM7eZW0;SSL Mode=Require;Trust Server Certificate=true" ; $env:USE_SQLITE = "0" ; $env:USE_SDJ = "1" ; dotnet run' -ForegroundColor Gray
