# Test PDF Generation Script
# Generates 3 sample PDFs with diverse profiles (weak/mixed/strong)

$ErrorActionPreference = "Stop"
$baseUrl = "http://localhost:5000"
$outputDir = "c:\Users\ASUS\Desktop\saitest\psy-tests-platform\reports\samples\pdf"

Write-Host "=== PDF Generation Test ===" -ForegroundColor Cyan
Write-Host "Output directory: $outputDir" -ForegroundColor Yellow

# Create output directory if not exists
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
    Write-Host "[OK] Created output directory" -ForegroundColor Green
}

# Test if backend is running
try {
    $healthCheck = Invoke-WebRequest -Uri "$baseUrl/health" -Method GET -UseBasicParsing -TimeoutSec 5
    Write-Host "[OK] Backend is running at $baseUrl" -ForegroundColor Green
} catch {
    Write-Host "[ERROR] Backend not accessible. Please start the backend first:" -ForegroundColor Red
    Write-Host "   cd backend/PsyApi; dotnet run" -ForegroundColor Yellow
    exit 1
}

# Function to get or create test user
function Get-TestUser {
    param($username, $email, $nationalId)
    
    Write-Host "Checking user: $username..." -ForegroundColor Gray
    
    # Try login first
    $loginBody = @{
        email = $email
        password = "Test123!"
    } | ConvertTo-Json
    
    try {
        $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method POST -Body $loginBody -ContentType "application/json"
        Write-Host "  [OK] User exists, logged in" -ForegroundColor Green
        return $loginResponse.token
    } catch {
        Write-Host "  User not found, creating..." -ForegroundColor Gray
    }
    
    # Register new user
    $registerBody = @{
        email = $email
        password = "Test123!"
        fullName = $username
        nationalId = $nationalId
        role = "User"
    } | ConvertTo-Json
    
    try {
        $registerResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/register" -Method POST -Body $registerBody -ContentType "application/json"
        Write-Host "  [OK] User created successfully" -ForegroundColor Green
        return $registerResponse.token
    } catch {
        Write-Host "  [FAIL] Failed to create user: $_" -ForegroundColor Red
        return $null
    }
}

# Function to generate PDF for a result ID
function Get-PdfReport {
    param($resultId, $token, $outputFile)
    
    Write-Host "Fetching PDF for result $resultId..." -ForegroundColor Gray
    
    $headers = @{
        "Authorization" = "Bearer $token"
    }
    
    try {
        $response = Invoke-WebRequest -Uri "$baseUrl/api/results/$resultId/pdf" -Method GET -Headers $headers -OutFile $outputFile -UseBasicParsing
        
        if (Test-Path $outputFile) {
            $sizeKB = [math]::Round((Get-Item $outputFile).Length / 1KB, 1)
            Write-Host "  [OK] PDF saved: $outputFile (${sizeKB} KB)" -ForegroundColor Green
            return $true
        } else {
            Write-Host "  [FAIL] PDF file not created" -ForegroundColor Red
            return $false
        }
    } catch {
        Write-Host "  [FAIL] Failed to fetch PDF: $_" -ForegroundColor Red
        return $false
    }
}

Write-Host "`n--- Step 1: Finding existing results ---" -ForegroundColor Cyan

# Get admin token to query results
Write-Host "Logging in as admin..." -ForegroundColor Gray
$adminLogin = @{
    email = "admin@psytests.com"
    password = "Admin123!"
} | ConvertTo-Json

try {
    $adminResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method POST -Body $adminLogin -ContentType "application/json"
    $adminToken = $adminResponse.token
    Write-Host "[OK] Admin logged in" -ForegroundColor Green
} catch {
    Write-Host "[WARN] Admin login failed. Using fallback test user creation." -ForegroundColor Yellow
    $adminToken = $null
}

# Get latest results
$resultIds = @()
if ($adminToken) {
    try {
        $headers = @{ "Authorization" = "Bearer $adminToken" }
        $results = Invoke-RestMethod -Uri "$baseUrl/api/results" -Method GET -Headers $headers
        
        if ($results -and $results.Count -gt 0) {
            $resultIds = $results | Select-Object -First 3 -ExpandProperty id
            Write-Host "[OK] Found $($resultIds.Count) existing results" -ForegroundColor Green
        }
    } catch {
        Write-Host "[WARN] Could not fetch results: $_" -ForegroundColor Yellow
    }
}

Write-Host "`n--- Step 2: Generating PDFs ---" -ForegroundColor Cyan

$pdfCount = 0

# Generate PDFs for existing results
foreach ($resultId in $resultIds) {
    $outputFile = Join-Path $outputDir "sample_result_${resultId}.pdf"
    
    if (Get-PdfReport -resultId $resultId -token $adminToken -outputFile $outputFile) {
        $pdfCount++
    }
    
    Start-Sleep -Milliseconds 500
}

# Summary
Write-Host "`n=== Test Complete ===" -ForegroundColor Cyan
Write-Host "PDFs generated: $pdfCount" -ForegroundColor $(if ($pdfCount -gt 0) { "Green" } else { "Yellow" })
Write-Host "Output directory: $outputDir" -ForegroundColor Yellow

if ($pdfCount -gt 0) {
    Write-Host "`n[SUCCESS] PDFs are ready for review." -ForegroundColor Green
    Write-Host "  Open the output directory to view the PDFs." -ForegroundColor Gray
    
    # Open output directory in Explorer
    Start-Process explorer.exe $outputDir
} else {
    Write-Host "`n[WARN] No PDFs generated. Possible reasons:" -ForegroundColor Yellow
    Write-Host "  - No completed sessions in database" -ForegroundColor Gray
    Write-Host "  - Backend not fully seeded" -ForegroundColor Gray
    Write-Host "  Suggestion: Complete a test session first" -ForegroundColor Gray
}
