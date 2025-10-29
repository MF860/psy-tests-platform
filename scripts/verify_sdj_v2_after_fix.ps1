# ================================================================
# SDJ V2 Post-Fix Verification Script
# ================================================================
# Tests all endpoints after fixing the migration conflict
# ================================================================

$ErrorActionPreference = "Continue"
$baseUrl = "https://psy-tests-backend.onrender.com"

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "SDJ V2 DEPLOYMENT VERIFICATION" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Track results
$results = @{
    Passed = 0
    Failed = 0
    Tests = @()
}

function Test-Endpoint {
    param(
        [string]$Name,
        [string]$Url,
        [string]$Method = "GET",
        [string]$Body = $null,
        [scriptblock]$Validation
    )
    
    Write-Host "[$Name] Testing..." -NoNewline
    
    try {
        $params = @{
            Uri = $Url
            Method = $Method
            TimeoutSec = 30
        }
        
        if ($Body) {
            $params.Body = $Body
            $params.ContentType = "application/json"
        }
        
        $response = Invoke-RestMethod @params
        
        # Run validation
        $validationResult = & $Validation $response
        
        if ($validationResult.Success) {
            Write-Host " PASS" -ForegroundColor Green
            Write-Host "  $($validationResult.Message)" -ForegroundColor Gray
            $results.Passed++
        } else {
            Write-Host " FAIL" -ForegroundColor Red
            Write-Host "  $($validationResult.Message)" -ForegroundColor Red
            $results.Failed++
        }
        
        $results.Tests += @{
            Name = $Name
            Status = if ($validationResult.Success) { "PASS" } else { "FAIL" }
            Message = $validationResult.Message
        }
        
    } catch {
        Write-Host " ERROR" -ForegroundColor Red
        Write-Host "  $($_.Exception.Message)" -ForegroundColor Red
        $results.Failed++
        
        $results.Tests += @{
            Name = $Name
            Status = "ERROR"
            Message = $_.Exception.Message
        }
    }
    
    Start-Sleep -Milliseconds 500
}

# ================================================================
# TEST 1: Health Check
# ================================================================
Test-Endpoint -Name "Health Check" `
    -Url "$baseUrl/api/health" `
    -Validation {
        param($response)
        
        if ($response.status -eq "Healthy") {
            return @{ Success = $true; Message = "Backend is healthy" }
        } else {
            return @{ Success = $false; Message = "Status: $($response.status)" }
        }
    }

# ================================================================
# TEST 2: Create Session
# ================================================================
Write-Host "`n[Session Creation] Creating test session..." -NoNewline

try {
    $sessionBody = @{
        userId = "test-validation-$(Get-Date -Format 'yyyyMMddHHmmss')"
        examType = "sdj"
    } | ConvertTo-Json
    
    $session = Invoke-RestMethod -Uri "$baseUrl/api/sessions" `
        -Method Post `
        -Body $sessionBody `
        -ContentType "application/json" `
        -TimeoutSec 30
    
    Write-Host " SUCCESS" -ForegroundColor Green
    Write-Host "  Session ID: $($session.id)" -ForegroundColor Gray
    $sessionId = $session.id
    $results.Passed++
    
} catch {
    Write-Host " FAILED" -ForegroundColor Red
    Write-Host "  $($_.Exception.Message)" -ForegroundColor Red
    $results.Failed++
    $sessionId = $null
}

# ================================================================
# TEST 3: Get Questions (Should be 210)
# ================================================================
if ($sessionId) {
    Test-Endpoint -Name "Questions Count" `
        -Url "$baseUrl/api/sessions/$sessionId/questions" `
        -Validation {
            param($response)
            
            $count = $response.Count
            if ($count -eq 210) {
                return @{ Success = $true; Message = "Found 210 questions (SDJ V2 active)" }
            } elseif ($count -eq 125) {
                return @{ Success = $false; Message = "Found 125 questions (Old SDJ V1 - migration failed)" }
            } else {
                return @{ Success = $false; Message = "Found $count questions (expected 210)" }
            }
        }
    
    # ================================================================
    # TEST 4: Verify V2 Structure (PatternId exists)
    # ================================================================
    Write-Host "`n[Question Structure] Checking V2 fields..." -NoNewline
    
    try {
        $questions = Invoke-RestMethod -Uri "$baseUrl/api/sessions/$sessionId/questions" -TimeoutSec 30
        
        $firstQuestion = $questions[0]
        
        if ($firstQuestion.PSObject.Properties.Name -contains 'patternId' -or 
            $firstQuestion.PSObject.Properties.Name -contains 'PatternId') {
            
            Write-Host " PASS" -ForegroundColor Green
            
            # Count patterns
            $patterns = $questions | Group-Object { $_.patternId ?? $_.PatternId } | 
                Select-Object Name, Count | 
                Where-Object { $_.Name }
            
            Write-Host "  Pattern Distribution:" -ForegroundColor Gray
            foreach ($p in $patterns | Sort-Object Name) {
                Write-Host "    $($p.Name): $($p.Count) questions" -ForegroundColor Gray
            }
            
            $results.Passed++
            
        } else {
            Write-Host " FAIL" -ForegroundColor Red
            Write-Host "  PatternId field not found - V2 migration incomplete" -ForegroundColor Red
            $results.Failed++
        }
        
    } catch {
        Write-Host " ERROR" -ForegroundColor Red
        Write-Host "  $($_.Exception.Message)" -ForegroundColor Red
        $results.Failed++
    }
    
    # ================================================================
    # TEST 5: Submit Answers (Sample)
    # ================================================================
    Write-Host "`n[Answer Submission] Submitting sample answers..." -NoNewline
    
    try {
        # Get first 10 questions
        $questions = Invoke-RestMethod -Uri "$baseUrl/api/sessions/$sessionId/questions" -TimeoutSec 30
        $sampleQuestions = $questions | Select-Object -First 10
        
        # Create sample answers
        $answers = @()
        foreach ($q in $sampleQuestions) {
            if ($q.type -eq "MCQ") {
                $answers += @{
                    itemId = $q.id
                    answer = "A"
                }
            } else {
                $answers += @{
                    itemId = $q.id
                    answer = "3"
                }
            }
        }
        
        $answersBody = @{ answers = $answers } | ConvertTo-Json -Depth 5
        
        $submitResult = Invoke-RestMethod -Uri "$baseUrl/api/sessions/$sessionId/submit" `
            -Method Post `
            -Body $answersBody `
            -ContentType "application/json" `
            -TimeoutSec 30
        
        Write-Host " SUCCESS" -ForegroundColor Green
        Write-Host "  Submitted $($answers.Count) answers" -ForegroundColor Gray
        $results.Passed++
        
    } catch {
        Write-Host " FAILED" -ForegroundColor Red
        Write-Host "  $($_.Exception.Message)" -ForegroundColor Red
        $results.Failed++
    }
}

# ================================================================
# TEST 6: Database Migration Check (via logs inference)
# ================================================================
Write-Host "`n[Migration Status] Checking if V2 migration is registered..." -NoNewline

try {
    # Try to create another session - if it works with 210 questions, migration is OK
    $testBody = @{ userId = "migration-check"; examType = "sdj" } | ConvertTo-Json
    $testSession = Invoke-RestMethod -Uri "$baseUrl/api/sessions" -Method Post -Body $testBody -ContentType "application/json" -TimeoutSec 30
    $testQuestions = Invoke-RestMethod -Uri "$baseUrl/api/sessions/$($testSession.id)/questions" -TimeoutSec 30
    
    if ($testQuestions.Count -eq 210) {
        Write-Host " PASS" -ForegroundColor Green
        Write-Host "  Migration 20251029143657_SDJ_V2_SevenPatterns is active" -ForegroundColor Gray
        $results.Passed++
    } else {
        Write-Host " FAIL" -ForegroundColor Red
        Write-Host "  Migration not fully applied (got $($testQuestions.Count) questions)" -ForegroundColor Red
        $results.Failed++
    }
    
} catch {
    Write-Host " ERROR" -ForegroundColor Red
    Write-Host "  $($_.Exception.Message)" -ForegroundColor Red
    $results.Failed++
}

# ================================================================
# SUMMARY
# ================================================================
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "VERIFICATION SUMMARY" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

Write-Host "`nTests Passed: " -NoNewline
Write-Host $results.Passed -ForegroundColor Green

Write-Host "Tests Failed: " -NoNewline
Write-Host $results.Failed -ForegroundColor $(if ($results.Failed -eq 0) { "Green" } else { "Red" })

Write-Host "`nDetailed Results:" -ForegroundColor Yellow
foreach ($test in $results.Tests) {
    $color = switch ($test.Status) {
        "PASS" { "Green" }
        "FAIL" { "Red" }
        "ERROR" { "Red" }
    }
    
    Write-Host "  [$($test.Status)] $($test.Name)" -ForegroundColor $color
    Write-Host "      $($test.Message)" -ForegroundColor Gray
}

# ================================================================
# FINAL STATUS
# ================================================================
Write-Host "`n========================================" -ForegroundColor Cyan

if ($results.Failed -eq 0) {
    Write-Host "STATUS: READY FOR PRODUCTION" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "`nAll tests passed! SDJ V2 is fully deployed." -ForegroundColor Green
    Write-Host "You can now use the platform with 210 questions and 7 patterns." -ForegroundColor Green
    exit 0
} else {
    Write-Host "STATUS: ISSUES DETECTED" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "`nSome tests failed. Please review the issues above." -ForegroundColor Yellow
    Write-Host "Refer to TROUBLESHOOTING_MIGRATION_ERROR.md for solutions." -ForegroundColor Yellow
    exit 1
}
