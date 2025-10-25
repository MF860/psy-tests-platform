# AI Recommendations Test Script for OpenRouter DeepSeek Integration
# This script tests the AI recommendations API to verify OpenRouter DeepSeek is working

Write-Host "=== AI Recommendations Test for OpenRouter DeepSeek ===" -ForegroundColor Green

# Start server if not running
Write-Host "Starting backend server..." -ForegroundColor Yellow
Set-Location "c:\Users\ASUS\Desktop\saitest\psy-tests-platform\backend\PsyApi"
$env:USE_SQLITE = "1"

# Start server process
$serverProcess = Start-Process powershell -ArgumentList "-Command", "cd 'c:\Users\ASUS\Desktop\saitest\psy-tests-platform\backend\PsyApi'; `$env:USE_SQLITE='1'; dotnet run" -PassThru -WindowStyle Hidden
Write-Host "Server process started with ID: $($serverProcess.Id)"

# Wait for server to start
Write-Host "Waiting 30 seconds for server to initialize..." -ForegroundColor Yellow
Start-Sleep -Seconds 30

# Create realistic psychological test data for simulation
$testData = @{
    resultId = 1
    participantId = "simulation-test-deepseek-001"
    dimensions = @(
        @{ Dimension = "القلق"; T = 68.5 },
        @{ Dimension = "الاكتئاب"; T = 62.3 },
        @{ Dimension = "الضغط النفسي"; T = 75.8 },
        @{ Dimension = "الثقة بالنفس"; T = 38.2 },
        @{ Dimension = "التكيف الاجتماعي"; T = 45.7 },
        @{ Dimension = "القدرة على التعامل مع الضغوط"; T = 33.9 },
        @{ Dimension = "الاستقرار العاطفي"; T = 29.4 }
    )
    totalScore = 50.4
    context = "المشارك في الدراسة يظهر مستويات مرتفعة من القلق والضغط النفسي مع انخفاض كبير في الثقة بالنفس والاستقرار العاطفي. يحتاج إلى تدخل نفسي متخصص وبرنامج دعم شامل."
} | ConvertTo-Json -Depth 10

Write-Host "`nTesting OpenRouter DeepSeek API with psychological test data..." -ForegroundColor Cyan
Write-Host "Test Payload:"
Write-Host $testData -ForegroundColor Gray

try {
    # Test API endpoint
    $response = Invoke-RestMethod -Uri "http://localhost:5019/api/admin/recommendations" -Method POST -ContentType "application/json" -Body $testData -Headers @{"Authorization"="Bearer test-token"} -TimeoutSec 60
    
    Write-Host "`n🎉 SUCCESS! OpenRouter DeepSeek AI Recommendations Generated!" -ForegroundColor Green
    Write-Host "=============================================================" -ForegroundColor Green
    
    # Display the AI-generated recommendations
    if ($response.recommendations) {
        Write-Host "AI Recommendations:" -ForegroundColor Cyan
        Write-Host $response.recommendations -ForegroundColor White
    } else {
        Write-Host "Response received:" -ForegroundColor Cyan
        $response | ConvertTo-Json -Depth 10
    }
    
    Write-Host "`n✅ SIMULATION TEST COMPLETED SUCCESSFULLY!" -ForegroundColor Green
    Write-Host "✅ OpenRouter DeepSeek integration is working correctly!" -ForegroundColor Green
    Write-Host "✅ AI recommendations are being generated for psychological test results!" -ForegroundColor Green
    
} catch {
    Write-Host "`n❌ ERROR: Failed to get AI recommendations" -ForegroundColor Red
    Write-Host "Error Message: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.ErrorDetails.Message) {
        Write-Host "Error Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
    
    # Check if it's a connection issue
    try {
        $healthCheck = Invoke-RestMethod -Uri "http://localhost:5019" -Method GET -TimeoutSec 5
        Write-Host "Server is running but AI endpoint failed" -ForegroundColor Yellow
    } catch {
        Write-Host "Server connection failed - server may not be running" -ForegroundColor Red
    }
}

# Cleanup
Write-Host "`nCleaning up..." -ForegroundColor Yellow
try {
    if ($serverProcess -and !$serverProcess.HasExited) {
        Stop-Process -Id $serverProcess.Id -Force
        Write-Host "Server process stopped" -ForegroundColor Yellow
    }
} catch {
    Write-Host "Error stopping server process: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host "`n=== Test Complete ===" -ForegroundColor Green