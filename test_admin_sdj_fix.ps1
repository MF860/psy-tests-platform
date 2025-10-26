# Test Admin SDJ Deserialization Fix (v2.0.1)
# Verifies admin endpoints handle SDJ data without JsonException

$baseUrl = "http://localhost:5019"
$ErrorActionPreference = "Continue"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Admin SDJ Fix Verification (v2.0.1)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Admin Login
Write-Host "[1/4] Admin Login..." -ForegroundColor Yellow
$loginBody = @{
    username = "root"
    password = "root123"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/admin/login" `
        -Method Post `
        -ContentType "application/json" `
        -Body $loginBody
    
    $token = $loginResponse.token
    Write-Host "  ✓ Login successful" -ForegroundColor Green
    Write-Host "  Token: $($token.Substring(0, 20))..." -ForegroundColor Gray
} catch {
    Write-Host "  ✗ Login failed: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Step 2: Test Analytics Overview
Write-Host "[2/4] Testing /api/admin/analytics/overview..." -ForegroundColor Yellow
try {
    $headers = @{
        "Authorization" = "Bearer $token"
    }
    
    $overview = Invoke-RestMethod -Uri "$baseUrl/api/admin/analytics/overview" `
        -Method Get `
        -Headers $headers
    
    Write-Host "  ✓ Overview endpoint works" -ForegroundColor Green
    Write-Host "  Total Results: $($overview.totalResults)" -ForegroundColor Gray
    Write-Host "  Total Sessions: $($overview.totalSessions)" -ForegroundColor Gray
    Write-Host "  Avg Score: $($overview.avgTotalScore)" -ForegroundColor Gray
    Write-Host "  Dimensions Found: $($overview.byDimension.Count)" -ForegroundColor Gray
    
    if ($overview.byDimension.Count -gt 0) {
        Write-Host "  Sample Dimension: $($overview.byDimension[0].dimension) (T=$($overview.byDimension[0].tScore))" -ForegroundColor Gray
    }
} catch {
    Write-Host "  ✗ Analytics overview failed: $_" -ForegroundColor Red
    Write-Host "  This indicates JsonException still present!" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Step 3: Test Results List
Write-Host "[3/4] Testing /api/admin/results..." -ForegroundColor Yellow
try {
    $results = Invoke-RestMethod -Uri "$baseUrl/api/admin/results?page=1&pageSize=5" `
        -Method Get `
        -Headers $headers
    
    Write-Host "  ✓ Results list endpoint works" -ForegroundColor Green
    Write-Host "  Total: $($results.total)" -ForegroundColor Gray
    Write-Host "  Page: $($results.page)/$([Math]::Ceiling($results.total / $results.pageSize))" -ForegroundColor Gray
    
    if ($results.data.Count -gt 0) {
        $firstResult = $results.data[0]
        Write-Host "  Latest Result ID: $($firstResult.id)" -ForegroundColor Gray
        Write-Host "  National ID: $($firstResult.nationalId)" -ForegroundColor Gray
        Write-Host "  Score: $($firstResult.score)" -ForegroundColor Gray
        
        $resultId = $firstResult.id
    }
} catch {
    Write-Host "  ✗ Results list failed: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Step 4: Test Result Detail (if we have a result)
if ($resultId) {
    Write-Host "[4/4] Testing /api/admin/results/$resultId..." -ForegroundColor Yellow
    try {
        $detail = Invoke-RestMethod -Uri "$baseUrl/api/admin/results/$resultId" `
            -Method Get `
            -Headers $headers
        
        Write-Host "  ✓ Result detail endpoint works" -ForegroundColor Green
        Write-Host "  Session ID: $($detail.sessionId)" -ForegroundColor Gray
        Write-Host "  Total Score: $($detail.totalScore)" -ForegroundColor Gray
        Write-Host "  Dimensions: $($detail.dimensions.Count)" -ForegroundColor Gray
        
        if ($detail.dimensions.Count -gt 0) {
            Write-Host "  Sample: $($detail.dimensions[0].dimension) (T=$($detail.dimensions[0].t), %ile=$($detail.dimensions[0].percentile))" -ForegroundColor Gray
        }
        
        Write-Host "  Model Version: $($detail.scoringModelVersion)" -ForegroundColor Gray
    } catch {
        Write-Host "  ✗ Result detail failed: $_" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "[4/4] Skipping result detail (no results in database)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "✓ ALL TESTS PASSED!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Admin endpoints handle SDJ data correctly." -ForegroundColor Green
Write-Host "Zero JsonException errors detected." -ForegroundColor Green
Write-Host "v2.0.1 hotfix verified successfully." -ForegroundColor Green
Write-Host ""
