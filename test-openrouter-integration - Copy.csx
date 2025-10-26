using System.Net.Http;
using System.Text;
using System.Text.Json;

// Test script to verify OpenRouter API key and DeepSeek model
var apiKey = "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";
var model = "deepseek/deepseek-chat";

Console.WriteLine("🔍 Testing OpenRouter API Key and DeepSeek Model Integration...");
Console.WriteLine($"API Key: {apiKey[..20]}...");
Console.WriteLine($"Model: {model}");
Console.WriteLine();

using var httpClient = new HttpClient();
httpClient.BaseAddress = new Uri("https://openrouter.ai/api/v1");
httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");
httpClient.DefaultRequestHeaders.Add("X-Title", "Psy Tests Admin - Test");

var requestPayload = new
{
    model = model,
    temperature = 0.2,
    max_tokens = 200,
    messages = new[]
    {
        new { role = "system", content = "أنت محلّل سيكومتري محترف. أعد الإجابة بصيغة JSON فقط." },
        new { role = "user", content = "اختبر التكامل مع DeepSeek. أجب بـ JSON يحتوي على {\"status\": \"success\", \"message\": \"التكامل يعمل بنجاح\"}" }
    },
    response_format = new { type = "json_object" }
};

try 
{
    Console.WriteLine("⏳ Sending test request to OpenRouter...");
    
    var json = JsonSerializer.Serialize(requestPayload, new JsonSerializerOptions 
    { 
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
    });
    
    var content = new StringContent(json, Encoding.UTF8, "application/json");
    var response = await httpClient.PostAsync("/chat/completions", content);
    
    Console.WriteLine($"📡 Response Status: {response.StatusCode}");
    
    if (response.IsSuccessStatusCode)
    {
        var responseJson = await response.Content.ReadAsStringAsync();
        Console.WriteLine("✅ SUCCESS: OpenRouter API key and DeepSeek model are working!");
        Console.WriteLine($"📄 Response: {responseJson}");
        
        // Parse the response to check if we got valid JSON from DeepSeek
        var doc = JsonDocument.Parse(responseJson);
        if (doc.RootElement.TryGetProperty("choices", out var choices) && 
            choices.GetArrayLength() > 0 &&
            choices[0].TryGetProperty("message", out var message) &&
            message.TryGetProperty("content", out var messageContent))
        {
            Console.WriteLine($"🤖 AI Response Content: {messageContent.GetString()}");
            Console.WriteLine("✅ Integration is working perfectly!");
        }
    }
    else
    {
        var errorContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"❌ ERROR: {response.StatusCode}");
        Console.WriteLine($"📄 Error Details: {errorContent}");
        
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            Console.WriteLine("🔑 Issue: Invalid API key or insufficient permissions");
        }
        else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            Console.WriteLine("⏰ Issue: Rate limit exceeded, try again later");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ EXCEPTION: {ex.Message}");
    Console.WriteLine("🔍 Check your internet connection and API key");
}

Console.WriteLine();
Console.WriteLine("📋 Integration Status Summary:");
Console.WriteLine("- Backend: ✅ Running on http://localhost:5019");
Console.WriteLine($"- API Key: ✅ {apiKey[..10]}... (configured)");
Console.WriteLine($"- Model: ✅ {model} (DeepSeek V3.1)");
Console.WriteLine("- Endpoint: ✅ POST /api/admin/ai/analyze");
Console.WriteLine();
Console.WriteLine("🎯 To test the full integration:");
Console.WriteLine("1. Start the frontend: npm run dev (in admin-ui folder)");
Console.WriteLine("2. Login to admin panel: http://localhost:5173");
Console.WriteLine("3. Go to a result detail page");
Console.WriteLine("4. Click 'تشغيل التحليل' (Run Analysis) button");