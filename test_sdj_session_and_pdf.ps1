# ============================================================================
# SDJ V2 Session Simulator + PDF Download Test
# ============================================================================
# This script:
# 1. Creates a test user (or reuses existing)
# 2. Starts an SDJ session
# 3. Submits 80 realistic answers (MCQ + Likert)
# 4. Completes the session
# 5. Downloads the PDF report via Admin API
# 6. Opens the PDF automatically
# ============================================================================

param(
    [string]$BaseUrl = "http://localhost:5124",
    [string]$AdminUsername = "root",
    [string]$AdminPassword = "StrongAdmin!23!",
    [string]$TestUserNationalId = "",  # Leave empty to create new user, or use 1000000001-1000000009 for existing
    [switch]$UseRenderUrl
)

if ($UseRenderUrl) {
    $BaseUrl = "https://psy-api-backend.onrender.com"
}

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# ============================================================================
# Helper Functions
# ============================================================================

function Write-Step {
    param([string]$Message)
    Write-Host "`n$('=' * 80)" -ForegroundColor Cyan
    Write-Host "  $Message" -ForegroundColor Yellow
    Write-Host "$('=' * 80)" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "✅ $Message" -ForegroundColor Green
}

function Write-Info {
    param([string]$Message)
    Write-Host "ℹ️  $Message" -ForegroundColor Blue
}

function Write-Error-Custom {
    param([string]$Message)
    Write-Host "❌ $Message" -ForegroundColor Red
}

function Invoke-ApiRequest {
    param(
        [string]$Method,
        [string]$Endpoint,
        [object]$Body = $null,
        [string]$Token = $null,
        [switch]$IgnoreError
    )
    
    $headers = @{
        "Content-Type" = "application/json"
    }
    
    if ($Token) {
        $headers["Authorization"] = "Bearer $Token"
    }
    
    $params = @{
        Method = $Method
        Uri = "$BaseUrl$Endpoint"
        Headers = $headers
        TimeoutSec = 30
    }
    
    if ($Body) {
        $params["Body"] = ($Body | ConvertTo-Json -Depth 10)
    }
    
    try {
        $response = Invoke-RestMethod @params
        return $response
    }
    catch {
        if (-not $IgnoreError) {
            Write-Error-Custom "API Error: $($_.Exception.Message)"
            if ($_.ErrorDetails.Message) {
                Write-Host $_.ErrorDetails.Message -ForegroundColor Red
            }
            throw
        }
        return $null
    }
}

# ============================================================================
# Step 1: Admin Login
# ============================================================================

Write-Step "STEP 1: Admin Authentication"

$adminLoginBody = @{
    username = $AdminUsername
    password = $AdminPassword
}

try {
    $adminAuth = Invoke-ApiRequest -Method POST -Endpoint "/api/auth/admin/login" -Body $adminLoginBody
    $adminToken = $adminAuth.token
    
    if (-not $adminToken) {
        Write-Error-Custom "Admin login failed - no token received"
        Write-Host "Response: $($adminAuth | ConvertTo-Json -Depth 3)" -ForegroundColor Red
        exit 1
    }
    
    Write-Success "Admin logged in successfully"
    Write-Info "Admin Token: $($adminToken.Substring(0, [Math]::Min(20, $adminToken.Length)))..."
}
catch {
    Write-Error-Custom "Admin login failed. Make sure admin credentials are correct."
    Write-Host "Error: $_" -ForegroundColor Red
    exit 1
}

# ============================================================================
# Step 2: Create or Use Test User
# ============================================================================

Write-Step "STEP 2: Get Test User"

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$testUserNationalId = ""
$testUserPassword = ""

if ($TestUserNationalId) {
    # Use existing test user
    Write-Info "Using existing test user with National ID: $TestUserNationalId"
    $testUserNationalId = $TestUserNationalId
    $testUserPassword = "User@123"  # Default password for seeded users
}
else {
    # Create new test user
    $testUserNationalId = "TEST$timestamp"
    $testUserPassword = "Test@123456"
    
    $testUser = @{
        nationalId = $testUserNationalId
        fullName = "محمد أحمد التجريبي"
        email = "test_$timestamp@example.com"
        phoneNumber = "+966500000000"
        dateOfBirth = "1995-01-01"
        gender = "M"
        educationLevel = "Bachelor"
        password = $testUserPassword
    }

    try {
        $userResponse = Invoke-ApiRequest -Method POST -Endpoint "/api/auth/register" -Body $testUser
        Write-Success "Test user created: $testUserNationalId"
        Write-Info "User ID: $($userResponse.user.id)"
    }
    catch {
        Write-Info "User might already exist, trying to login instead..."
    }
}

# Login as test user
$userLoginBody = @{
    nationalId = $testUserNationalId
    password = $testUserPassword
}

try {
    $userAuth = Invoke-ApiRequest -Method POST -Endpoint "/api/auth/login" -Body $userLoginBody
    $userToken = $userAuth.token
    $userId = $userAuth.user.id
    
    if (-not $userToken) {
        Write-Error-Custom "Failed to get user token"
        exit 1
    }
    
    Write-Success "User logged in successfully"
    Write-Info "User Token: $($userToken.Substring(0, [Math]::Min(20, $userToken.Length)))..."
    Write-Info "User ID: $userId"
}
catch {
    Write-Error-Custom "Failed to login as test user"
    Write-Host "Error: $_" -ForegroundColor Red
    exit 1
}

# ============================================================================
# Step 3: Get SDJ Test Configuration
# ============================================================================

Write-Step "STEP 3: Fetch SDJ Test Configuration"

try {
    $tests = Invoke-ApiRequest -Method GET -Endpoint "/api/tests"
    $sdjTest = $tests | Where-Object { $_.code -eq "SDJ" -or $_.name -like "*SDJ*" } | Select-Object -First 1
    
    if (-not $sdjTest) {
        Write-Error-Custom "SDJ test not found in database"
        exit 1
    }
    
    $testId = $sdjTest.id
    Write-Success "Found SDJ test: $($sdjTest.name)"
    Write-Info "Test ID: $testId"
    Write-Info "Test Code: $($sdjTest.code)"
}
catch {
    Write-Error-Custom "Failed to fetch tests"
    exit 1
}

# ============================================================================
# Step 4: Start SDJ Session
# ============================================================================

Write-Step "STEP 4: Start SDJ Session"

$sessionBody = @{
    testId = $testId
}

try {
    $session = Invoke-ApiRequest -Method POST -Endpoint "/api/sessions/start" -Body $sessionBody -Token $userToken
    $sessionId = $session.id
    Write-Success "Session started successfully"
    Write-Info "Session ID: $sessionId"
}
catch {
    Write-Error-Custom "Failed to start session"
    exit 1
}

# ============================================================================
# Step 5: Fetch Questions
# ============================================================================

Write-Step "STEP 5: Fetch SDJ Questions"

try {
    $questionsResponse = Invoke-ApiRequest -Method GET -Endpoint "/api/sessions/$sessionId/questions" -Token $userToken
    $questions = $questionsResponse.questions
    
    if (-not $questions -or $questions.Count -eq 0) {
        Write-Error-Custom "No questions returned from API"
        exit 1
    }
    
    Write-Success "Fetched $($questions.Count) questions"
    $mcqCount = ($questions | Where-Object { $_.type -eq 'MCQ' }).Count
    $likertCount = ($questions | Where-Object { $_.type -eq 'Likert' }).Count
    Write-Info "MCQ Questions: $mcqCount"
    Write-Info "Likert Questions: $likertCount"
}
catch {
    Write-Error-Custom "Failed to fetch questions"
    exit 1
}

# ============================================================================
# Step 6: Submit Realistic Answers
# ============================================================================

Write-Step "STEP 6: Submit 80 SDJ Answers (Realistic Distribution)"

# Define realistic answer patterns
# SDJ V2 uses 7 patterns, we'll create varied performance across patterns
$answerPatterns = @{
    # Pattern 1: High performance (mostly correct)
    "Pattern1" = @{
        MCQ_Correct_Rate = 0.85
        Likert_Avg = 4.2  # High agreement
    }
    # Pattern 2: Good performance
    "Pattern2" = @{
        MCQ_Correct_Rate = 0.75
        Likert_Avg = 3.8
    }
    # Pattern 3: Average performance
    "Pattern3" = @{
        MCQ_Correct_Rate = 0.65
        Likert_Avg = 3.2
    }
    # Pattern 4: Below average
    "Pattern4" = @{
        MCQ_Correct_Rate = 0.55
        Likert_Avg = 2.8
    }
    # Pattern 5: Good performance
    "Pattern5" = @{
        MCQ_Correct_Rate = 0.70
        Likert_Avg = 3.5
    }
    # Pattern 6: Excellent
    "Pattern6" = @{
        MCQ_Correct_Rate = 0.90
        Likert_Avg = 4.5
    }
    # Pattern 7: Average
    "Pattern7" = @{
        MCQ_Correct_Rate = 0.60
        Likert_Avg = 3.0
    }
}

$submittedCount = 0
$patternIndex = 1

foreach ($question in $questions) {
    # Rotate through patterns to create varied performance
    $currentPattern = "Pattern$patternIndex"
    $pattern = $answerPatterns[$currentPattern]
    
    $answer = $null
    
    if ($question.type -eq "MCQ") {
        # MCQ: Pick correct answer based on pattern rate
        $random = Get-Random -Minimum 0.0 -Maximum 1.0
        
        if ($random -lt $pattern.MCQ_Correct_Rate) {
            # Pick correct answer (Key field)
            $answer = $question.key
        }
        else {
            # Pick random wrong answer
            $options = @("A", "B", "C", "D") | Where-Object { $_ -ne $question.key }
            $answer = $options | Get-Random
        }
    }
    elseif ($question.type -eq "Likert") {
        # Likert: Generate value around pattern average (1-5 scale)
        $baseValue = $pattern.Likert_Avg
        $variance = Get-Random -Minimum -0.5 -Maximum 0.5
        $likertValue = [Math]::Round($baseValue + $variance)
        $likertValue = [Math]::Max(1, [Math]::Min(5, $likertValue))  # Clamp 1-5
        $answer = $likertValue
    }
    else {
        Write-Info "Unknown question type: $($question.type), skipping..."
        continue
    }
    
    # Submit answer
    $answerBody = @{
        questionId = $question.id
        answer = $answer.ToString()
        timeSpent = Get-Random -Minimum 5 -Maximum 15  # Realistic time: 5-15 seconds
    }
    
    try {
        Invoke-ApiRequest -Method POST -Endpoint "/api/sessions/$sessionId/answer" -Body $answerBody -Token $userToken | Out-Null
        $submittedCount++
        
        if ($submittedCount % 10 -eq 0) {
            Write-Info "Submitted $submittedCount/$($questions.Count) answers..."
        }
    }
    catch {
        Write-Error-Custom "Failed to submit answer for question $($question.id)"
    }
    
    # Rotate pattern every ~11 questions (80 questions / 7 patterns ≈ 11)
    if ($submittedCount % 11 -eq 0) {
        $patternIndex = ($patternIndex % 7) + 1
    }
    
    # Small delay to simulate human behavior
    Start-Sleep -Milliseconds 100
}

Write-Success "Submitted $submittedCount/$($questions.Count) answers"

# ============================================================================
# Step 7: Complete Session
# ============================================================================

Write-Step "STEP 7: Complete Session & Trigger Scoring"

try {
    $completeResponse = Invoke-ApiRequest -Method POST -Endpoint "/api/sessions/$sessionId/complete" -Token $userToken
    Write-Success "Session completed successfully"
    
    if ($completeResponse.result) {
        Write-Info "Result ID: $($completeResponse.result.id)"
        Write-Info "Total Score: $($completeResponse.result.totalScore)"
        Write-Info "Scoring Version: $($completeResponse.result.scoringModelVersion)"
        $resultId = $completeResponse.result.id
    }
}
catch {
    Write-Error-Custom "Failed to complete session"
    exit 1
}

# Wait for scoring to complete
Write-Info "Waiting 2 seconds for scoring to complete..."
Start-Sleep -Seconds 2

# ============================================================================
# Step 8: Verify Result via Admin API
# ============================================================================

Write-Step "STEP 8: Verify Result via Admin API"

try {
    $resultDetails = Invoke-ApiRequest -Method GET -Endpoint "/api/admin/results/$resultId" -Token $adminToken
    Write-Success "Result verified via Admin API"
    Write-Info "Session ID: $($resultDetails.sessionId)"
    Write-Info "User: $($resultDetails.session.user.fullName)"
    Write-Info "Scoring Model: $($resultDetails.scoringModelVersion)"
    Write-Info "Has Dimension Scores: $($resultDetails.dimensionScoresJson -ne $null)"
    
    # Check if it's SDJ V2 format
    if ($resultDetails.dimensionScoresJson -like "*PatternScores*") {
        Write-Success "✅ Result contains SDJ V2 data (PatternScores detected)"
    }
    elseif ($resultDetails.dimensionScoresJson -like "*SevenPatternScores*") {
        Write-Info "Result contains SDJ V1 data (SevenPatternScores detected)"
    }
    else {
        Write-Info "Result contains standard dimension scores"
    }
}
catch {
    Write-Error-Custom "Failed to fetch result details"
}

# ============================================================================
# Step 9: Download PDF Report
# ============================================================================

Write-Step "STEP 9: Download PDF Report (Ultra Hi-Fi)"

$pdfFilename = "SDJ_Report_${testUserNationalId}_$timestamp.pdf"
$pdfPath = Join-Path $PSScriptRoot $pdfFilename

try {
    Write-Info "Downloading PDF from: /api/admin/results/$resultId/pdf"
    
    $pdfHeaders = @{
        "Authorization" = "Bearer $adminToken"
        "Accept" = "application/pdf"
    }
    
    Invoke-RestMethod -Method GET -Uri "$BaseUrl/api/admin/results/$resultId/pdf" -Headers $pdfHeaders -OutFile $pdfPath -TimeoutSec 60
    
    $fileSize = (Get-Item $pdfPath).Length
    $fileSizeKB = [Math]::Round($fileSize / 1KB, 2)
    
    Write-Success "PDF downloaded successfully"
    Write-Info "File: $pdfFilename"
    Write-Info "Size: $fileSizeKB KB"
    Write-Info "Path: $pdfPath"
}
catch {
    Write-Error-Custom "Failed to download PDF"
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

# ============================================================================
# Step 10: Open PDF Automatically
# ============================================================================

Write-Step "STEP 10: Opening PDF Report"

try {
    Start-Process $pdfPath
    Write-Success "PDF opened in default viewer"
}
catch {
    Write-Info "Could not auto-open PDF. Please open manually: $pdfPath"
}

# ============================================================================
# Summary
# ============================================================================

Write-Host "`n"
Write-Host "$('=' * 80)" -ForegroundColor Green
Write-Host "  🎉 SDJ SESSION TEST COMPLETED SUCCESSFULLY" -ForegroundColor Green
Write-Host "$('=' * 80)" -ForegroundColor Green
Write-Host ""
Write-Host "Test Summary:" -ForegroundColor Cyan
Write-Host "  User National ID: $testUserNationalId" -ForegroundColor White
Write-Host "  Session ID: $sessionId" -ForegroundColor White
Write-Host "  Result ID: $resultId" -ForegroundColor White
Write-Host "  Questions Answered: $submittedCount" -ForegroundColor White
Write-Host "  PDF Downloaded: $pdfFilename - Size: ${fileSizeKB} KB" -ForegroundColor White
Write-Host ""
Write-Host "Quick Links:" -ForegroundColor Cyan
Write-Host "  Admin Dashboard: https://admin-ui-lyart-nu.vercel.app/sessions/$sessionId" -ForegroundColor Blue
Write-Host "  PDF Location: $pdfPath" -ForegroundColor Blue
Write-Host ""
Write-Host "Verification Checklist:" -ForegroundColor Yellow
Write-Host "  - PDF opens successfully" -ForegroundColor White
Write-Host "  - Contains 6 pages" -ForegroundColor White
Write-Host "  - Shows 7-pattern heptagon radar chart" -ForegroundColor White
Write-Host "  - Charts are high quality with 450 DPI" -ForegroundColor White
Write-Host "  - Arabic text displays correctly" -ForegroundColor White
Write-Host "  - T-scores displayed for all 7 patterns" -ForegroundColor White
Write-Host "  - Theme colors visible" -ForegroundColor White
Write-Host ""
Write-Host "$('=' * 80)" -ForegroundColor Green
Write-Host ""

# Save session info to file for later reference
$sessionInfo = @{
    Timestamp = $timestamp
    BaseUrl = $BaseUrl
    NationalId = $testUserNationalId
    SessionId = $sessionId
    ResultId = $resultId
    PdfPath = $pdfPath
    QuestionsAnswered = $submittedCount
} | ConvertTo-Json -Depth 3

$infoFilePath = Join-Path $PSScriptRoot "last_test_session.json"
$sessionInfo | Out-File $infoFilePath -Encoding UTF8

Write-Info "Session info saved to: $infoFilePath"
Write-Host ""
