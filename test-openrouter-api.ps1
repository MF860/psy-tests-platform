# Test OpenRouter API Integration
$apiKey = "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"
$model = "deepseek/deepseek-chat"
$baseUrl = "https://openrouter.ai/api/v1"

Write-Host "Testing OpenRouter API with DeepSeek model..." -ForegroundColor Yellow

# Test headers
$headers = @{
    "Authorization" = "Bearer $apiKey"
    "Content-Type" = "application/json"
    "HTTP-Referer" = "http://localhost:5019"
    "X-Title" = "Psy Tests Platform"
}

# Test payload
$payload = @{
    model = $model
    messages = @(
        @{
            role = "user"
            content = "Hello, can you respond in Arabic? Just say 'مرحبا بك' (Hello to you)"
        }
    )
    temperature = 0.7
    max_tokens = 100
} | ConvertTo-Json -Depth 10

Write-Host "Sending request to: $baseUrl/chat/completions" -ForegroundColor Cyan
Write-Host "Model: $model" -ForegroundColor Cyan
Write-Host "API Key (first 20 chars): $($apiKey.Substring(0,20))..." -ForegroundColor Cyan

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/chat/completions" -Method POST -Headers $headers -Body $payload
    
    Write-Host "`nSuccess! API Response:" -ForegroundColor Green
    Write-Host "Model used: $($response.model)" -ForegroundColor Green
    Write-Host "Response: $($response.choices[0].message.content)" -ForegroundColor Green
    
    if ($response.choices[0].message.content -match "مرحبا") {
        Write-Host "`n✅ Arabic response detected - DeepSeek integration working!" -ForegroundColor Green
    }
    
} catch {
    Write-Host "`nError testing OpenRouter API:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    
    if ($_.Exception.Response) {
        $errorStream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($errorStream)
        $errorBody = $reader.ReadToEnd()
        Write-Host "Error details: $errorBody" -ForegroundColor Red
    }
}

Write-Host "`nTest completed." -ForegroundColor Yellow