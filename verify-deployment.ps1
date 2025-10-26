# Post-Deployment Verification Script
# Tests all critical endpoints after deploying to Render + Vercel
# Usage: .\verify-deployment.ps1 -RenderUrl "https://psy-api-xxx.onrender.com" -UserUrl "https://user-ui-xxx.vercel.app" -AdminUrl "https://admin-ui-xxx.vercel.app"

param(
    [Parameter(Mandatory=$true)]
    [string]$RenderUrl,
    
    [Parameter(Mandatory=$true)]
    [string]$UserUrl,
    
    [Parameter(Mandatory=$true)]
    [string]$AdminUrl
)

$ErrorActionPreference = "Continue"
$SuccessCount = 0
$FailureCount = 0
$Warnings = @()

function Test-Endpoint {
    param(
        [string]$Name,
        [string]$Url,
        [string]$Method = "GET",
        [object]$Body = $null,
        [hashtable]$Headers = @{},
        [string]$ExpectedStatus = "200",
        [scriptblock]$ValidationScript = $null
    )
    
    Write-Host "`nTesting: $Name" -ForegroundColor Cyan
    Write-Host "  URL: $Url"
    Write-Host "  Method: $Method"
    
    try {
        $params = @{
            Uri = $Url
            Method = $Method
            Headers = $Headers
            UseBasicParsing = $true
        }
        
        if ($Body) {
            $params.Body = ($Body | ConvertTo-Json -Depth 10)
            $params.ContentType = "application/json; charset=utf-8"
        }
        
        $response = Invoke-WebRequest @params
        
        if ($response.StatusCode -eq $ExpectedStatus) {
            Write-Host "  ✅ Status: $($response.StatusCode)" -ForegroundColor Green
            
            if ($ValidationScript) {
                $content = $response.Content | ConvertFrom-Json
                $validationResult = & $ValidationScript $content
                if ($validationResult) {
                    Write-Host "  ✅ Validation passed" -ForegroundColor Green
                    $script:SuccessCount++
                    return $content
                } else {
                    Write-Host "  ❌ Validation failed" -ForegroundColor Red
                    $script:FailureCount++
                    return $null
                }
            }
            
            $script:SuccessCount++
            return $response.Content | ConvertFrom-Json
        } else {
            Write-Host "  ❌ Unexpected status: $($response.StatusCode)" -ForegroundColor Red
            $script:FailureCount++
            return $null
        }
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        Write-Host "  ❌ Error: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "  Status Code: $statusCode" -ForegroundColor Red
        $script:FailureCount++
        return $null
    }
}

Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  PSY TESTS PLATFORM - DEPLOYMENT VERIFICATION" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "Backend:  $RenderUrl"
Write-Host "User UI:  $UserUrl"
Write-Host "Admin UI: $AdminUrl"
Write-Host ""

# ═══════════════════════════════════════════════════════════
# BACKEND API TESTS
# ═══════════════════════════════════════════════════════════

Write-Host "`n╔══════════════════════════════════════════════════════════╗" -ForegroundColor Yellow
Write-Host "║  BACKEND API TESTS                                       ║" -ForegroundColor Yellow
Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Yellow

# Test 1: Health Check
$health = Test-Endpoint `
    -Name "Health Check" `
    -Url "$RenderUrl/health" `
    -ValidationScript {
        param($data)
        return ($data.status -eq "healthy" -and $data.useSdj -eq $true)
    }

if ($health) {
    Write-Host "  Environment: $($health.environment)" -ForegroundColor Gray
    Write-Host "  USE_SDJ: $($health.useSdj)" -ForegroundColor Gray
}

# Test 2: Start Session
$startBody = @{
    nationalId = "1000000001"
}

$session = Test-Endpoint `
    -Name "Start Session" `
    -Url "$RenderUrl/api/sessions/start" `
    -Method "POST" `
    -Body $startBody `
    -ValidationScript {
        param($data)
        return ($data.sessionId -ne $null -and $data.totalQuestions -gt 0)
    }

if ($session) {
    $sessionId = $session.sessionId
    Write-Host "  Session ID: $sessionId" -ForegroundColor Gray
    Write-Host "  Total Questions: $($session.totalQuestions)" -ForegroundColor Gray
    
    # Test 3: Get Next Question
    Start-Sleep -Seconds 2
    $question = Test-Endpoint `
        -Name "Get Next Question" `
        -Url "$RenderUrl/api/sessions/$sessionId/next" `
        -ValidationScript {
            param($data)
            return ($data.item_id -ne $null -and $data.type -ne $null)
        }
    
    if ($question) {
        Write-Host "  Item ID: $($question.item_id)" -ForegroundColor Gray
        Write-Host "  Type: $($question.type)" -ForegroundColor Gray
        Write-Host "  Arabic Text: $($question.text_ar.Substring(0, [Math]::Min(50, $question.text_ar.Length)))..." -ForegroundColor Gray
        
        # Test 4: Submit Answer
        $answerBody = @{
            ItemId = $question.item_id
            Answer = "محايد"
        }
        
        Start-Sleep -Seconds 1
        $answer = Test-Endpoint `
            -Name "Submit Answer" `
            -Url "$RenderUrl/api/sessions/$sessionId/answer" `
            -Method "POST" `
            -Body $answerBody `
            -ValidationScript {
                param($data)
                return ($data.message -like "*success*")
            }
        
        if ($answer) {
            Write-Host "  ✅ Answer submitted successfully" -ForegroundColor Green
        }
    } else {
        $script:Warnings += "Could not get question - session may be rate-limited"
    }
} else {
    $script:Warnings += "Could not start session - backend may have rate limiting active"
}

# ═══════════════════════════════════════════════════════════
# USER UI TESTS
# ═══════════════════════════════════════════════════════════

Write-Host "`n╔══════════════════════════════════════════════════════════╗" -ForegroundColor Yellow
Write-Host "║  USER UI TESTS                                           ║" -ForegroundColor Yellow
Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Yellow

# Test 5: User UI Loads
try {
    $userResponse = Invoke-WebRequest -Uri $UserUrl -UseBasicParsing
    if ($userResponse.StatusCode -eq 200) {
        Write-Host "`n✅ User UI: Loads successfully" -ForegroundColor Green
        $script:SuccessCount++
        
        # Check for Arabic content
        if ($userResponse.Content -match "[\u0600-\u06FF]") {
            Write-Host "  ✅ Contains Arabic text" -ForegroundColor Green
        } else {
            Write-Host "  ⚠️  No Arabic text detected in HTML" -ForegroundColor Yellow
            $script:Warnings += "User UI may not have Arabic content loaded"
        }
        
        # Check for no demo mode indicators
        if ($userResponse.Content -notmatch "demo|Demo|DEMO") {
            Write-Host "  ✅ No demo mode indicators" -ForegroundColor Green
        } else {
            Write-Host "  ⚠️  Found demo-related text (may be false positive)" -ForegroundColor Yellow
        }
    }
} catch {
    Write-Host "`n❌ User UI: Failed to load" -ForegroundColor Red
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    $script:FailureCount++
}

# ═══════════════════════════════════════════════════════════
# ADMIN UI TESTS
# ═══════════════════════════════════════════════════════════

Write-Host "`n╔══════════════════════════════════════════════════════════╗" -ForegroundColor Yellow
Write-Host "║  ADMIN UI TESTS                                          ║" -ForegroundColor Yellow
Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Yellow

# Test 6: Admin UI Loads
try {
    $adminResponse = Invoke-WebRequest -Uri $AdminUrl -UseBasicParsing
    if ($adminResponse.StatusCode -eq 200) {
        Write-Host "`n✅ Admin UI: Loads successfully" -ForegroundColor Green
        $script:SuccessCount++
        
        # Check for Arabic content
        if ($adminResponse.Content -match "[\u0600-\u06FF]") {
            Write-Host "  ✅ Contains Arabic text" -ForegroundColor Green
        } else {
            Write-Host "  ⚠️  No Arabic text detected in HTML" -ForegroundColor Yellow
            $script:Warnings += "Admin UI may not have Arabic content loaded"
        }
    }
} catch {
    Write-Host "`n❌ Admin UI: Failed to load" -ForegroundColor Red
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    $script:FailureCount++
}

# ═══════════════════════════════════════════════════════════
# CORS VALIDATION
# ═══════════════════════════════════════════════════════════

Write-Host "`n╔══════════════════════════════════════════════════════════╗" -ForegroundColor Yellow
Write-Host "║  CORS VALIDATION                                         ║" -ForegroundColor Yellow
Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Yellow

Write-Host "`nCORS Test: Making preflight request from User UI origin"
try {
    $corsHeaders = @{
        "Origin" = $UserUrl
        "Access-Control-Request-Method" = "POST"
        "Access-Control-Request-Headers" = "Content-Type"
    }
    
    $corsResponse = Invoke-WebRequest `
        -Uri "$RenderUrl/api/sessions/start" `
        -Method OPTIONS `
        -Headers $corsHeaders `
        -UseBasicParsing
    
    $allowedOrigin = $corsResponse.Headers["Access-Control-Allow-Origin"]
    if ($allowedOrigin -eq $UserUrl -or $allowedOrigin -eq "*") {
        Write-Host "  ✅ CORS configured for User UI" -ForegroundColor Green
        $script:SuccessCount++
    } else {
        Write-Host "  ❌ CORS not configured correctly for User UI" -ForegroundColor Red
        Write-Host "  Allowed Origin: $allowedOrigin" -ForegroundColor Red
        $script:FailureCount++
        $script:Warnings += "Update CORS_ALLOWED_ORIGINS in Render to include: $UserUrl"
    }
} catch {
    Write-Host "  ⚠️  Could not verify CORS (OPTIONS may not be supported)" -ForegroundColor Yellow
    $script:Warnings += "Manually verify CORS works by testing from browser"
}

# ═══════════════════════════════════════════════════════════
# SUMMARY
# ═══════════════════════════════════════════════════════════

Write-Host "`n═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  VERIFICATION SUMMARY" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan

Write-Host "`nTests Passed: " -NoNewline
Write-Host $SuccessCount -ForegroundColor Green

Write-Host "Tests Failed: " -NoNewline
if ($FailureCount -gt 0) {
    Write-Host $FailureCount -ForegroundColor Red
} else {
    Write-Host $FailureCount -ForegroundColor Green
}

if ($Warnings.Count -gt 0) {
    Write-Host "`nWarnings:" -ForegroundColor Yellow
    $Warnings | ForEach-Object {
        Write-Host "  ⚠️  $_" -ForegroundColor Yellow
    }
}

Write-Host "`n═══════════════════════════════════════════════════════════" -ForegroundColor Cyan

if ($FailureCount -eq 0) {
    Write-Host "`n✅ DEPLOYMENT VERIFICATION PASSED! 🎉" -ForegroundColor Green
    Write-Host "`nYour application is ready for production use." -ForegroundColor Green
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "  1. Test complete exam flow in User UI" -ForegroundColor Cyan
    Write-Host "  2. Verify results display in Admin UI" -ForegroundColor Cyan
    Write-Host "  3. Test PDF generation" -ForegroundColor Cyan
    Write-Host "  4. Set up monitoring and alerts" -ForegroundColor Cyan
} else {
    Write-Host "`n❌ DEPLOYMENT VERIFICATION FAILED" -ForegroundColor Red
    Write-Host "`nPlease review the errors above and:" -ForegroundColor Yellow
    Write-Host "  1. Check environment variables in Render and Vercel" -ForegroundColor Yellow
    Write-Host "  2. Verify CORS configuration" -ForegroundColor Yellow
    Write-Host "  3. Check backend logs in Render dashboard" -ForegroundColor Yellow
    Write-Host "  4. Review DEPLOYMENT_GUIDE_RENDER_VERCEL.md" -ForegroundColor Yellow
    exit 1
}
