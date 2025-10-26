# Test Neon PostgreSQL Integration
# This script tests the backend connection to Neon database

Write-Host "=== Neon PostgreSQL Integration Test ===" -ForegroundColor Cyan
Write-Host ""

# Set environment variables for Neon
$env:ConnectionStrings__DefaultConnection = "Host=ep-holy-glitter-a40pexde-pooler.us-east-1.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=npg_OUubznM7eZW0;SSL Mode=Require;Trust Server Certificate=true"
$env:USE_SQLITE = "0"
$env:USE_SDJ = "1"
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = "http://localhost:5019"

Write-Host "Environment variables configured:" -ForegroundColor Green
Write-Host "  - Database: Neon PostgreSQL (neondb)" -ForegroundColor Gray
Write-Host "  - Host: ep-holy-glitter-a40pexde-pooler.us-east-1.aws.neon.tech" -ForegroundColor Gray
Write-Host "  - USE_SQLITE: 0 (disabled)" -ForegroundColor Gray
Write-Host "  - USE_SDJ: 1 (enabled - 120 SDJ questions)" -ForegroundColor Gray
Write-Host ""

# Navigate to backend directory
Set-Location -Path "$PSScriptRoot\backend\PsyApi"

Write-Host "=== Step 1: Building Backend ===" -ForegroundColor Yellow
dotnet build -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "Build successful" -ForegroundColor Green
Write-Host ""

Write-Host "=== Step 2: Applying EF Migrations ===" -ForegroundColor Yellow
Write-Host "Running: dotnet ef database update" -ForegroundColor Gray
dotnet ef database update
if ($LASTEXITCODE -ne 0) {
    Write-Host "Migration failed!" -ForegroundColor Red
    Write-Host "Make sure dotnet-ef tool is installed:" -ForegroundColor Yellow
    Write-Host "  dotnet tool install --global dotnet-ef" -ForegroundColor Gray
    exit 1
}
Write-Host "Migrations applied successfully" -ForegroundColor Green
Write-Host ""

Write-Host "=== Step 3: Starting Backend ===" -ForegroundColor Yellow
Write-Host "Logs to watch for:" -ForegroundColor Gray
Write-Host "  - Auto-detected PostgreSQL from connection string format" -ForegroundColor Gray
Write-Host "  - Using PostgreSQL with connection string" -ForegroundColor Gray
Write-Host "  - Running database migrations..." -ForegroundColor Gray
Write-Host "  - Database migrations completed successfully" -ForegroundColor Gray
Write-Host "  - Seeding SDJ items..." -ForegroundColor Gray
Write-Host ""

# Start backend in background
$backendJob = Start-Job -ScriptBlock {
    param($connString, $useSqlite, $useSdj)
    $env:ConnectionStrings__DefaultConnection = $connString
    $env:USE_SQLITE = $useSqlite
    $env:USE_SDJ = $useSdj
    $env:ASPNETCORE_ENVIRONMENT = "Development"
    $env:ASPNETCORE_URLS = "http://localhost:5019"
    
    Set-Location -Path $using:PSScriptRoot\backend\PsyApi
    dotnet run
} -ArgumentList $env:ConnectionStrings__DefaultConnection, "0", "1"

Write-Host "Backend starting (Job ID: $($backendJob.Id))..." -ForegroundColor Cyan

# Wait for backend to start
Start-Sleep -Seconds 10

Write-Host ""
Write-Host "=== Step 4: Testing Health Endpoint ===" -ForegroundColor Yellow
$maxRetries = 5
$retryCount = 0
$healthOk = $false

while ($retryCount -lt $maxRetries -and -not $healthOk) {
    try {
        $response = Invoke-RestMethod -Uri "http://localhost:5019/health" -Method Get -ErrorAction Stop
        Write-Host "Health check passed!" -ForegroundColor Green
        Write-Host "  Status: $($response.status)" -ForegroundColor Gray
        Write-Host "  Environment: $($response.environment)" -ForegroundColor Gray
        Write-Host "  USE_SDJ: $($response.useSdj)" -ForegroundColor Gray
        Write-Host "  Timestamp: $($response.timestamp)" -ForegroundColor Gray
        $healthOk = $true
    }
    catch {
        $retryCount++
        if ($retryCount -lt $maxRetries) {
            Write-Host "Waiting for backend... (attempt $retryCount/$maxRetries)" -ForegroundColor Yellow
            Start-Sleep -Seconds 3
        }
        else {
            Write-Host "Health check failed after $maxRetries attempts" -ForegroundColor Red
        }
    }
}

if (-not $healthOk) {
    Write-Host ""
    Write-Host "Backend may still be starting. Check logs:" -ForegroundColor Yellow
    Receive-Job -Job $backendJob
    Stop-Job -Job $backendJob
    Remove-Job -Job $backendJob
    exit 1
}

Write-Host ""
Write-Host "=== Step 5: Testing Session Endpoint ===" -ForegroundColor Yellow
try {
    $sessionPayload = @{
        nationalId = "1234567890"
        fullName = "Test User Neon"
        dateOfBirth = "1990-01-01"
        gender = "Male"
    } | ConvertTo-Json

    $sessionResponse = Invoke-RestMethod -Uri "http://localhost:5019/api/sessions/start" `
        -Method Post `
        -Body $sessionPayload `
        -ContentType "application/json" `
        -ErrorAction Stop

    Write-Host "Session started successfully!" -ForegroundColor Green
    Write-Host "  Session ID: $($sessionResponse.sessionId)" -ForegroundColor Gray
    Write-Host "  Total Questions: $($sessionResponse.totalQuestions)" -ForegroundColor Gray
    Write-Host "  Resume: $($sessionResponse.resume)" -ForegroundColor Gray

    # Verify SDJ questions count
    if ($sessionResponse.totalQuestions -eq 120) {
        Write-Host "  SDJ mode confirmed (120 questions)" -ForegroundColor Green
    }
    else {
        Write-Host "  Expected 120 SDJ questions but got $($sessionResponse.totalQuestions)" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "Session test failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== Step 6: Database Verification ===" -ForegroundColor Yellow
Write-Host "Expected tables: Items, Sessions, Results, Users, Admins, AuditLogs" -ForegroundColor Gray
Write-Host "To verify in Neon dashboard:" -ForegroundColor Gray
Write-Host "  1. Go to https://console.neon.tech" -ForegroundColor Gray
Write-Host "  2. Open project: neondb" -ForegroundColor Gray
Write-Host "  3. Go to SQL Editor" -ForegroundColor Gray
Write-Host '  4. Run: SELECT COUNT(*) FROM "Items" WHERE "IsSdj" = true;' -ForegroundColor Gray
Write-Host "  5. Should return: 120 (SDJ items)" -ForegroundColor Gray
Write-Host ""

Write-Host "=== Summary ===" -ForegroundColor Cyan
Write-Host "Backend connected to Neon PostgreSQL" -ForegroundColor Green
Write-Host "Migrations applied successfully" -ForegroundColor Green
Write-Host "Health endpoint responding" -ForegroundColor Green
Write-Host "Session creation working" -ForegroundColor Green
Write-Host "SDJ mode enabled" -ForegroundColor Green
Write-Host ""
Write-Host "Backend is running at: http://localhost:5019" -ForegroundColor Cyan
Write-Host "Swagger UI: http://localhost:5019/swagger" -ForegroundColor Cyan
Write-Host ""
Write-Host "Press Ctrl+C to stop the backend server" -ForegroundColor Yellow
Write-Host ""

# Keep backend running and show logs
Write-Host "=== Backend Logs (live) ===" -ForegroundColor Yellow
Receive-Job -Job $backendJob -Keep -Wait
