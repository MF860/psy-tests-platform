# Test AI Recommendations with OpenRouter DeepSeek
Write-Host "Testing AI Recommendations with OpenRouter DeepSeek..." -ForegroundColor Green

# Start the server in background
Write-Host "Starting backend server..." -ForegroundColor Yellow
$serverProcess = Start-Process -FilePath "dotnet" -ArgumentList "run" -PassThru -WindowStyle Hidden

# Wait for server to start
Start-Sleep -Seconds 10

try {
    # Test the AI recommendations endpoint
    Write-Host "Testing AI recommendations endpoint..." -ForegroundColor Yellow
    
    $testData = @{
        userId = 1
        testResultId = 1
        personalityScores = @{
            Openness = 0.75
            Conscientiousness = 0.65
            Extraversion = 0.55
            Agreeableness = 0.85
            Neuroticism = 0.25
        }
        cognitiveAbilities = @{
            VerbalReasoning = 0.80
            NumericalReasoning = 0.70
            AbstractReasoning = 0.75
        }
        context = "Career development assessment for software engineering role"
    } | ConvertTo-Json -Depth 3

    $headers = @{
        "Content-Type" = "application/json"
    }

    Write-Host "Sending request to AI recommendations API..." -ForegroundColor Cyan
    $response = Invoke-RestMethod -Uri "http://localhost:5019/api/recommendations/ai" -Method Post -Body $testData -Headers $headers -TimeoutSec 30

    Write-Host "✅ SUCCESS! AI Recommendations Response:" -ForegroundColor Green
    Write-Host ($response | ConvertTo-Json -Depth 3) -ForegroundColor White

} catch {
    Write-Host "❌ ERROR: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Response: $($_.Exception.Response)" -ForegroundColor Red
} finally {
    # Stop the server
    Write-Host "Stopping backend server..." -ForegroundColor Yellow
    if ($serverProcess -and !$serverProcess.HasExited) {
        $serverProcess.Kill()
        Write-Host "Server stopped." -ForegroundColor Green
    }
}

Write-Host "Test completed!" -ForegroundColor Green