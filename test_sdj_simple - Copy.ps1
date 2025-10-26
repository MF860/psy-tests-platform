# Simple SDJ Test Script
# Tests complete flow: Start session -> Answer questions -> Submit -> Verify result

$baseUrl = "http://localhost:5019"
$nationalId = "1000000001"

Write-Host "`n=== Step 1: Start Session ===" -ForegroundColor Cyan
$startBody = @{ nationalId = $nationalId } | ConvertTo-Json
$session = Invoke-RestMethod -Uri "$baseUrl/api/sessions/start" -Method POST -Body $startBody -ContentType "application/json"
Write-Host "Session ID: $($session.sessionId)" -ForegroundColor Green
Write-Host "Total Questions: $($session.totalQuestions)" -ForegroundColor Green

$sessionId = $session.sessionId

Write-Host "`n=== Step 2: Answer All Questions ===" -ForegroundColor Cyan
$questionCount = 0
while ($questionCount -lt $session.totalQuestions) {
    try {
        $question = Invoke-RestMethod -Uri "$baseUrl/api/sessions/$sessionId/next" -Method GET
        $questionCount++
        
        # Determine answer based on question type
        $answerText = if ($question.type -eq "MCQ") { 
            # For MCQ, use first option
            ($question.options -split '\|')[0]
        } else { 
            # For Likert, use neutral (middle option)
            "محايد"
        }
        
        $answerBody = @{
            ItemId = $question.item_id
            Answer = $answerText
        } | ConvertTo-Json
        
        $result = Invoke-RestMethod -Uri "$baseUrl/api/sessions/$sessionId/answer" -Method POST -Body $answerBody -ContentType "application/json; charset=utf-8"
        
        if ($questionCount -eq 1 -or $questionCount % 25 -eq 0) {
            Write-Host "Answered $questionCount questions..." -ForegroundColor Yellow
        }
    }
    catch {
        Write-Host "Error answering question $questionCount : $_" -ForegroundColor Red
        break
    }
}

Write-Host "Total answered: $questionCount questions" -ForegroundColor Green

Write-Host "`n=== Step 3: Submit Session ===" -ForegroundColor Cyan
$submitResult = Invoke-RestMethod -Uri "$baseUrl/api/sessions/$sessionId/submit" -Method POST
Write-Host "Result ID: $($submitResult.resultId)" -ForegroundColor Green
Write-Host "Session Status: $($submitResult.status)" -ForegroundColor Green

# Save full result to JSON file
$submitResult | ConvertTo-Json -Depth 10 | Out-File "submit_result.json"
Write-Host "Full result saved to submit_result.json" -ForegroundColor Green

Write-Host "`n=== Step 4: Verify SDJ Data ===" -ForegroundColor Cyan
if ($submitResult.sdjData) {
    Write-Host "SDJ Data Present: Yes" -ForegroundColor Green
    Write-Host "Number of SubDimensions: $($submitResult.sdjData.subDimensions.Count)" -ForegroundColor Green
    
    Write-Host "`nSubDimensions:" -ForegroundColor Cyan
    foreach ($sd in $submitResult.sdjData.subDimensions) {
        Write-Host "  - $($sd.name): T=$($sd.tScore), Band=$($sd.band)" -ForegroundColor White
    }
    
    Write-Host "`n=== TEST PASSED ===" -ForegroundColor Green
    Write-Host "Successfully completed full SDJ flow with Neon PostgreSQL!" -ForegroundColor Green
}
else {
    Write-Host "SDJ Data Present: No" -ForegroundColor Red
    Write-Host "=== TEST FAILED ===" -ForegroundColor Red
}

Write-Host "`nTest completed at $(Get-Date)" -ForegroundColor Cyan
