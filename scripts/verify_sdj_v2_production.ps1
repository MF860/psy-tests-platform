# SDJ V2 Production Verification Script
# Verifies that SDJ V2 Seven Patterns is deployed and working

param(
    [string]$BackendUrl = "https://psy-tests-backend.onrender.com",
    [string]$TestUserId = "1000000001"
)

Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   SDJ V2 Seven Patterns - Production Verification             ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

$results = @()

# Test 1: Health Check
Write-Host "[1/7] Testing Backend Health..." -ForegroundColor Yellow
try {
    $health = Invoke-RestMethod -Uri "$BackendUrl/api/health" -Method GET -ErrorAction Stop
    if ($health.status -eq "healthy") {
        Write-Host "✓ Backend is healthy" -ForegroundColor Green
        Write-Host "  Database: $($health.database)" -ForegroundColor Gray
        $results += "✅ Health Check"
    } else {
        Write-Host "✗ Backend unhealthy: $($health.status)" -ForegroundColor Red
        $results += "❌ Health Check"
    }
} catch {
    Write-Host "✗ Health check failed: $($_.Exception.Message)" -ForegroundColor Red
    $results += "❌ Health Check"
}

Write-Host ""

# Test 2: Check Database Item Count
Write-Host "[2/7] Checking Question Count..." -ForegroundColor Yellow
try {
    # This endpoint may not exist, so we'll try to create a session and count items
    $sessionPayload = @{
        examId = 1
        userId = $TestUserId
    } | ConvertTo-Json -Compress

    $session = Invoke-RestMethod -Uri "$BackendUrl/api/sessions" -Method POST `
        -Body $sessionPayload -ContentType "application/json" -ErrorAction Stop
    
    $itemCount = $session.sessionItems.Count
    Write-Host "  Session created: $($session.id)" -ForegroundColor Gray
    Write-Host "  Item count: $itemCount" -ForegroundColor Gray
    
    if ($itemCount -eq 210) {
        Write-Host "✓ Correct item count (210) - SDJ V2 is loaded!" -ForegroundColor Green
        $results += "✅ Question Count (210)"
    } elseif ($itemCount -eq 125) {
        Write-Host "✗ Wrong item count ($itemCount) - Still using SDJ V1!" -ForegroundColor Red
        $results += "❌ Question Count (125 - V1)"
    } elseif ($itemCount -eq 200) {
        Write-Host "✗ Wrong item count ($itemCount) - Still using Legacy!" -ForegroundColor Red
        $results += "❌ Question Count (200 - Legacy)"
    } else {
        Write-Host "⚠ Unexpected item count: $itemCount" -ForegroundColor Yellow
        $results += "⚠️ Question Count ($itemCount)"
    }
    
    # Save session ID for later tests
    $global:TestSessionId = $session.id
} catch {
    Write-Host "✗ Session creation failed: $($_.Exception.Message)" -ForegroundColor Red
    $results += "❌ Question Count"
}

Write-Host ""

# Test 3: Check Item Structure (PatternId presence)
Write-Host "[3/7] Checking Item Structure..." -ForegroundColor Yellow
try {
    if ($session.sessionItems.Count -gt 0) {
        $firstItem = $session.sessionItems[0].item
        
        if ($firstItem.patternId) {
            Write-Host "✓ Items have PatternId - SDJ V2 schema detected!" -ForegroundColor Green
            Write-Host "  PatternId: $($firstItem.patternId)" -ForegroundColor Gray
            Write-Host "  PatternNameAr: $($firstItem.patternNameAr)" -ForegroundColor Gray
            Write-Host "  SubId: $($firstItem.subId)" -ForegroundColor Gray
            $results += "✅ V2 Schema (PatternId)"
        } else {
            Write-Host "✗ Items missing PatternId - Still using old schema!" -ForegroundColor Red
            $results += "❌ V2 Schema Missing"
        }
    }
} catch {
    Write-Host "✗ Schema check failed: $($_.Exception.Message)" -ForegroundColor Red
    $results += "❌ V2 Schema"
}

Write-Host ""

# Test 4: Submit Sample Answers
Write-Host "[4/7] Submitting Test Answers..." -ForegroundColor Yellow
try {
    if ($global:TestSessionId) {
        $answers = @()
        foreach ($si in $session.sessionItems) {
            $answer = if ($si.item.type -eq "MultipleChoice") { "A" } else { "3" }
            $answers += @{
                sessionItemId = $si.id
                answer = $answer
            }
        }
        
        $submitPayload = @{
            answers = $answers
        } | ConvertTo-Json -Depth 10 -Compress
        
        $result = Invoke-RestMethod -Uri "$BackendUrl/api/sessions/$($global:TestSessionId)/submit" `
            -Method POST -Body $submitPayload -ContentType "application/json" -ErrorAction Stop
        
        Write-Host "✓ Submission successful" -ForegroundColor Green
        Write-Host "  Result ID: $($result.id)" -ForegroundColor Gray
        
        $global:TestResultId = $result.id
        $results += "✅ Answer Submission"
    }
} catch {
    Write-Host "✗ Submission failed: $($_.Exception.Message)" -ForegroundColor Red
    $results += "❌ Answer Submission"
}

Write-Host ""

# Test 5: Check Result Structure (Seven Patterns)
Write-Host "[5/7] Checking Result Structure..." -ForegroundColor Yellow
try {
    if ($global:TestResultId) {
        $resultDetail = Invoke-RestMethod -Uri "$BackendUrl/api/results/$($global:TestResultId)" `
            -Method GET -ErrorAction Stop
        
        $sdjData = $resultDetail.dimensionScores | ConvertFrom-Json
        
        if ($sdjData.sevenPatternScores) {
            Write-Host "✓ SevenPatternScores found - SDJ V2 scoring active!" -ForegroundColor Green
            Write-Host "  Pattern count: $($sdjData.sevenPatternScores.Count)" -ForegroundColor Gray
            Write-Host "  Version: $($sdjData.version)" -ForegroundColor Gray
            
            if ($sdjData.sevenPatternScores.Count -eq 7) {
                Write-Host "✓ Correct pattern count (7)" -ForegroundColor Green
                $results += "✅ Seven Patterns"
            } else {
                Write-Host "⚠ Unexpected pattern count: $($sdjData.sevenPatternScores.Count)" -ForegroundColor Yellow
                $results += "⚠️ Seven Patterns ($($sdjData.sevenPatternScores.Count))"
            }
        } else {
            Write-Host "✗ SevenPatternScores missing - Still using V1!" -ForegroundColor Red
            $results += "❌ Seven Patterns Missing"
        }
    }
} catch {
    Write-Host "✗ Result check failed: $($_.Exception.Message)" -ForegroundColor Red
    $results += "❌ Seven Patterns"
}

Write-Host ""

# Test 6: Test PDF Generation
Write-Host "[6/7] Testing PDF Generation..." -ForegroundColor Yellow
try {
    if ($global:TestResultId) {
        $pdfUrl = "$BackendUrl/api/results/$($global:TestResultId)/pdf"
        $response = Invoke-WebRequest -Uri $pdfUrl -Method GET -ErrorAction Stop
        
        if ($response.StatusCode -eq 200 -and $response.Headers.'Content-Type' -eq 'application/pdf') {
            $pdfSize = $response.Content.Length
            Write-Host "✓ PDF generated successfully" -ForegroundColor Green
            Write-Host "  Size: $([math]::Round($pdfSize / 1024, 1)) KB" -ForegroundColor Gray
            
            # Save PDF for manual inspection
            $pdfPath = "test_sdj_v2_report.pdf"
            [System.IO.File]::WriteAllBytes($pdfPath, $response.Content)
            Write-Host "  Saved to: $pdfPath" -ForegroundColor Gray
            
            $results += "✅ PDF Generation"
        } else {
            Write-Host "✗ PDF response invalid" -ForegroundColor Red
            $results += "❌ PDF Generation"
        }
    }
} catch {
    Write-Host "✗ PDF generation failed: $($_.Exception.Message)" -ForegroundColor Red
    $results += "❌ PDF Generation"
}

Write-Host ""

# Test 7: Check Admin Analytics
Write-Host "[7/7] Testing Admin Analytics..." -ForegroundColor Yellow
try {
    $analytics = Invoke-RestMethod -Uri "$BackendUrl/api/admin/analytics" -Method GET -ErrorAction Stop
    
    Write-Host "✓ Analytics endpoint working" -ForegroundColor Green
    Write-Host "  Total users: $($analytics.totalUsers)" -ForegroundColor Gray
    Write-Host "  Total sessions: $($analytics.totalSessions)" -ForegroundColor Gray
    
    $results += "✅ Admin Analytics"
} catch {
    Write-Host "✗ Analytics check failed: $($_.Exception.Message)" -ForegroundColor Red
    $results += "❌ Admin Analytics"
}

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   VERIFICATION SUMMARY                                         ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

foreach ($result in $results) {
    Write-Host "  $result" -ForegroundColor White
}

Write-Host ""

$successCount = ($results | Where-Object { $_ -like "✅*" }).Count
$totalCount = $results.Count
$percentage = [math]::Round(($successCount / $totalCount) * 100, 0)

if ($percentage -eq 100) {
    Write-Host "🎉 ALL TESTS PASSED - SDJ V2 IS FULLY DEPLOYED!" -ForegroundColor Green
} elseif ($percentage -ge 70) {
    Write-Host "⚠️ PARTIAL DEPLOYMENT - $percentage% complete" -ForegroundColor Yellow
    Write-Host "   Some components may still be using V1 or need redeployment" -ForegroundColor Yellow
} else {
    Write-Host "❌ DEPLOYMENT INCOMPLETE - $percentage% complete" -ForegroundColor Red
    Write-Host "   SDJ V2 is NOT properly deployed. Check Render logs and database." -ForegroundColor Red
}

Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. If tests failed, check Render deployment logs" -ForegroundColor White
Write-Host "2. Verify environment variable USE_SDJ is not set (defaults to V2)" -ForegroundColor White
Write-Host "3. Ensure database migration ran: 20251029143657_SDJ_V2_SevenPatterns" -ForegroundColor White
Write-Host "4. Check that 210 items were seeded from questions_sdj_v2_ar.csv" -ForegroundColor White
Write-Host ""
