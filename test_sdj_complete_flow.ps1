# Complete SDJ Session Flow Test
# This script tests the entire SDJ session flow: start → answer all questions → submit → verify sdjData

$ErrorActionPreference = "Stop"
$baseUrl = "http://localhost:5019"

Write-Host "=== SDJ Complete Flow Test ===" -ForegroundColor Cyan
Write-Host ""

# Step 1: Start new session
Write-Host "[1/4] Starting new SDJ session..." -ForegroundColor Yellow
$startBody = @{
    nationalId = "1000000001"
} | ConvertTo-Json

$session = Invoke-RestMethod -Uri "$baseUrl/api/sessions/start" -Method POST -ContentType "application/json" -Body $startBody
$sessionId = $session.sessionId
$totalQuestions = $session.totalQuestions

Write-Host "  ✓ Session ID: $sessionId" -ForegroundColor Green
Write-Host "  ✓ Total Questions: $totalQuestions" -ForegroundColor Green
Write-Host ""

# Step 2: Answer all questions
Write-Host "[2/4] Answering $totalQuestions questions..." -ForegroundColor Yellow
$answeredCount = 0
$errors = @()

for ($i = 0; $i -lt $totalQuestions; $i++) {
    try {
        # Get next question
        $item = Invoke-RestMethod -Uri "$baseUrl/api/sessions/$sessionId/next" -Method GET
        
        if ($null -eq $item -or $null -eq $item.item_id) {
            Write-Host "  ! No more questions available after $answeredCount answers" -ForegroundColor Yellow
            break
        }
        
        # Submit answer (using numeric value for Likert items: 1-5 scale, we'll use 3 = neutral)
        $answerBody = @{
            itemId = $item.item_id
            answer = "3"  # Neutral answer for all Likert items
        } | ConvertTo-Json
        
        $null = Invoke-RestMethod -Uri "$baseUrl/api/sessions/$sessionId/answer" -Method POST -ContentType "application/json" -Body $answerBody
        $answeredCount++
        
        # Progress indicator
        if ($answeredCount % 10 -eq 0) {
            Write-Host "  Progress: $answeredCount/$totalQuestions" -ForegroundColor Gray
        }
    }
    catch {
        $errors += "Question $($i+1): $($_.Exception.Message)"
        Write-Host "  ✗ Error at question $($i+1): $($_.Exception.Message)" -ForegroundColor Red
        break
    }
}

Write-Host "  ✓ Answered $answeredCount questions" -ForegroundColor Green
Write-Host ""

# Step 3: Submit session to generate results
Write-Host "[3/4] Submitting session for scoring..." -ForegroundColor Yellow
try {
    $submitResponse = Invoke-RestMethod -Uri "$baseUrl/api/sessions/$sessionId/submit" -Method POST
    Write-Host "  ✓ Session submitted successfully" -ForegroundColor Green
    
    # Extract result ID from response if available
    if ($submitResponse.resultId) {
        $resultId = $submitResponse.resultId
        Write-Host "  ✓ Result ID: $resultId" -ForegroundColor Green
    }
}
catch {
    Write-Host "  ✗ Submit failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Error Details:" -ForegroundColor Red
    Write-Host $_.Exception -ForegroundColor Red
    exit 1
}
Write-Host ""

# Step 4: Retrieve and verify result with sdjData
Write-Host "[4/4] Retrieving result and verifying sdjData..." -ForegroundColor Yellow
try {
    # Try to get the result - we need to find the most recent result for this user
    $results = Invoke-RestMethod -Uri "$baseUrl/api/results" -Method GET
    
    if ($results -and $results.Count -gt 0) {
        # Get the most recent result
        $latestResult = $results[0]
        $resultId = $latestResult.id
        
        # Get full result details
        $result = Invoke-RestMethod -Uri "$baseUrl/api/results/$resultId" -Method GET
        
        # Save to file
        $resultJson = $result | ConvertTo-Json -Depth 15
        $resultJson | Out-File "C:\Users\ASUS\Desktop\saitest\psy-tests-platform\docs\samples\sdj\submit_result.json" -Encoding UTF8
        
        Write-Host "  ✓ Result saved to submit_result.json" -ForegroundColor Green
        
        # Verify sdjData presence
        if ($result.sdjData) {
            Write-Host "  ✓ sdjData present in result" -ForegroundColor Green
            
            # Check for SubDimensions
            if ($result.sdjData.SubDimensions) {
                $subdimensionCount = $result.sdjData.SubDimensions.Count
                Write-Host "  ✓ Found $subdimensionCount subdimensions in sdjData" -ForegroundColor Green
                
                # List subdimensions
                Write-Host ""
                Write-Host "  SDJ Subdimensions:" -ForegroundColor Cyan
                foreach ($subdim in $result.sdjData.SubDimensions) {
                    $tScore = if ($subdim.TScore) { $subdim.TScore.ToString("F1") } else { "N/A" }
                    $band = if ($subdim.Band) { $subdim.Band } else { "N/A" }
                    Write-Host "    - $($subdim.NameAr): T=$tScore, Band=$band" -ForegroundColor Gray
                }
            }
            else {
                Write-Host "  ! sdjData exists but SubDimensions array is empty" -ForegroundColor Yellow
            }
        }
        else {
            Write-Host "  ✗ sdjData NOT found in result (this indicates legacy mode or scoring issue)" -ForegroundColor Red
        }
    }
    else {
        Write-Host "  ✗ No results found" -ForegroundColor Red
    }
}
catch {
    Write-Host "  ✗ Failed to retrieve result: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== Test Complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  - Questions answered: $answeredCount/$totalQuestions"
Write-Host "  - Session ID: $sessionId"
if ($resultId) {
    Write-Host "  - Result ID: $resultId"
}
Write-Host ""

if ($errors.Count -gt 0) {
    Write-Host "Errors encountered:" -ForegroundColor Red
    foreach ($error in $errors) {
        Write-Host "  - $error" -ForegroundColor Red
    }
}
