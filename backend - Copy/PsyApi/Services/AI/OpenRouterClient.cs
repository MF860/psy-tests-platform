using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using PsyApi.Models;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;

namespace PsyApi.Services.AI
{
    /// <summary>
    /// DeepSeek API client for SDJ Analysis
    /// Direct integration (not via OpenRouter)
    /// </summary>
    public interface IOpenRouterClient
    {
        Task<AiAnalysisResponse> AnalyzeAsync(Result result, IEnumerable<DimensionScore> dimensions, CancellationToken cancellationToken = default);
    }

    public class OpenRouterClient : IOpenRouterClient
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<OpenRouterClient> _logger;
        private readonly DeepSeekConfiguration _config;
        
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public OpenRouterClient(
            HttpClient httpClient,
            IMemoryCache cache, 
            IOptions<OpenRouterConfiguration> config,
            ILogger<OpenRouterClient> logger)
        {
            _httpClient = httpClient;
            _cache = cache;
            _logger = logger;
            _config = config.Value;
            
            // Validate API key
            if (string.IsNullOrWhiteSpace(_config.ApiKey))
            {
                _logger.LogWarning("[AI] DEEPSEEK_API_KEY not configured - will use fallback only");
            }
            else
            {
                // Redact key in logs - show only first 10 and last 4 chars
                var redacted = _config.ApiKey.Length > 14 
                    ? $"{_config.ApiKey[..10]}...{_config.ApiKey[^4..]}"
                    : "***REDACTED***";
                _logger.LogInformation("[AI] DeepSeek client initialized with key: {RedactedKey}", redacted);
            }
            
            // Configure HttpClient
            _httpClient.BaseAddress = new Uri(_config.BaseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiKey}");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        public async Task<AiAnalysisResponse> AnalyzeAsync(Result result, IEnumerable<DimensionScore> dimensions, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"ai_analysis_{result.Id}_{GetDimensionsHash(dimensions)}";
            
            // Check cache first
            if (_cache.TryGetValue(cacheKey, out AiAnalysisResponse? cachedResult) && cachedResult != null)
            {
                _logger.LogInformation("[AI] Cache hit for resultId={ResultId}", result.Id);
                return cachedResult;
            }

            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var prompt = BuildPrompt(result, dimensions);
                var requestPayload = new
                {
                    model = _config.Model,
                    temperature = _config.Temperature,
                    max_tokens = _config.MaxTokens,
                    messages = new[]
                    {
                        new { role = "system", content = GetSystemPrompt() },
                        new { role = "user", content = prompt }
                    },
                    response_format = new { type = "json_object" }
                };

                var response = await SendWithRetryAsync(requestPayload, cancellationToken);
                var analysisResult = ParseResponse(response);
                
                stopwatch.Stop();
                
                var result_response = new AiAnalysisResponse
                {
                    Analysis = analysisResult,
                    Model = _config.Model,
                    Usage = response.Usage != null ? new AiAnalysisUsage
                    {
                        PromptTokens = response.Usage.PromptTokens,
                        CompletionTokens = response.Usage.CompletionTokens,
                        TotalTokens = response.Usage.TotalTokens,
                        LatencyMs = stopwatch.Elapsed.TotalMilliseconds
                    } : null
                };

                // Cache the result
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(_config.CacheDurationHours),
                    Priority = CacheItemPriority.Normal
                };
                _cache.Set(cacheKey, result_response, cacheOptions);

                _logger.LogInformation("[AI] Analyze resultId={ResultId} model={Model} latencyMs={Latency} status=OK", 
                    result.Id, _config.Model, stopwatch.Elapsed.TotalMilliseconds);

                return result_response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "[AI] Analyze resultId={ResultId} model={Model} latencyMs={Latency} status=FAIL", 
                    result.Id, _config.Model, stopwatch.Elapsed.TotalMilliseconds);
                throw;
            }
        }

        private async Task<OpenRouterResponse> SendWithRetryAsync(object payload, CancellationToken cancellationToken)
        {
            var json = JsonSerializer.Serialize(payload, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            Exception? lastException = null;
            
            for (int attempt = 1; attempt <= _config.MaxRetries; attempt++)
            {
                try
                {
                    var response = await _httpClient.PostAsync("/chat/completions", content, cancellationToken);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                        var openRouterResponse = JsonSerializer.Deserialize<OpenRouterResponse>(responseJson, JsonOptions);
                        
                        if (openRouterResponse != null)
                        {
                            return openRouterResponse;
                        }
                        throw new InvalidOperationException("Failed to deserialize OpenRouter response");
                    }
                    
                    // Handle rate limiting and server errors
                    if (response.StatusCode == HttpStatusCode.TooManyRequests ||
                        response.StatusCode >= HttpStatusCode.InternalServerError)
                    {
                        if (attempt < _config.MaxRetries)
                        {
                            var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // Exponential backoff
                            _logger.LogWarning("[AI] HTTP {StatusCode} on attempt {Attempt}, retrying after {Delay}s", 
                                response.StatusCode, attempt, delay.TotalSeconds);
                            await Task.Delay(delay, cancellationToken);
                            continue;
                        }
                    }
                    
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    throw new HttpRequestException($"OpenRouter API error: {response.StatusCode} - {errorContent}");
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    lastException = ex;
                    
                    if (attempt < _config.MaxRetries)
                    {
                        var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                        _logger.LogWarning(ex, "[AI] Request failed on attempt {Attempt}, retrying after {Delay}s", 
                            attempt, delay.TotalSeconds);
                        await Task.Delay(delay, cancellationToken);
                        continue;
                    }
                }
            }
            
            throw lastException ?? new InvalidOperationException("All retry attempts failed");
        }

        private static string GetSystemPrompt()
        {
            return """
أنت محلّل سيكومتري محترف. أعد الإجابة بصيغة JSON فقط بدون أي شرح إضافي.
البنية المطلوبة:
{
  "strengths": ["...", "..."],
  "weaknesses": ["...", "..."],
  "recommendations": ["...", "..."],
  "rationale": "..."
}

القواعد:
- العربية الفصحى الواضحة
- 3–6 عناصر في كل قائمة
- لا تذكر أي بيانات تعريفية شخصية أو رقم وطني
- اجعل التوصيات عملية وقابلة للتنفيذ خلال 4–6 أسابيع
- التحليل مبني على درجات T-Score: أعلى من 60 قوة، أقل من 40 ضعف
""";
        }

        private static string BuildPrompt(Result result, IEnumerable<DimensionScore> dimensions)
        {
            var dims = dimensions.ToList();
            var totalScore = dims.Sum(d => d.Raw);
            var avgT = dims.Any() ? dims.Average(d => d.T) : 0;
            
            var topDims = dims.Where(d => d.T >= 60).OrderByDescending(d => d.T).Take(5)
                .Select(d => $"{d.Dimension} ({d.T:F1})").ToList();
            var lowDims = dims.Where(d => d.T < 40).OrderBy(d => d.T).Take(5)
                .Select(d => $"{d.Dimension} ({d.T:F1})").ToList();

            var prompt = new StringBuilder();
            prompt.AppendLine("ملخص النتائج (مجهول الهوية):");
            prompt.AppendLine($"- resultId: {result.Id}");
            prompt.AppendLine($"- المجموع الكلي: {totalScore}");
            prompt.AppendLine($"- متوسط T: {avgT:F1}");
            
            if (topDims.Any())
                prompt.AppendLine($"- أعلى الأبعاد (T): {string.Join(", ", topDims)}");
            
            if (lowDims.Any())
                prompt.AppendLine($"- أضعف الأبعاد (T): {string.Join(", ", lowDims)}");

            prompt.AppendLine();
            prompt.AppendLine("أنتج JSON فقط حسب البنية المطلوبة.");

            return prompt.ToString();
        }

        private static AiAnalysisResult ParseResponse(OpenRouterResponse response)
        {
            if (response.Choices == null || !response.Choices.Any())
            {
                throw new InvalidOperationException("No choices in OpenRouter response");
            }

            var content = response.Choices[0].Message?.Content?.Trim();
            if (string.IsNullOrEmpty(content))
            {
                throw new InvalidOperationException("Empty content in OpenRouter response");
            }

            // Try to extract JSON if wrapped in markdown
            if (content.StartsWith("```json") && content.EndsWith("```"))
            {
                content = content[7..^3].Trim();
            }
            else if (content.StartsWith("```") && content.EndsWith("```"))
            {
                content = content[3..^3].Trim();
            }

            try
            {
                var jsonDoc = JsonDocument.Parse(content);
                var root = jsonDoc.RootElement;

                return new AiAnalysisResult
                {
                    Strengths = ExtractStringArray(root, "strengths"),
                    Weaknesses = ExtractStringArray(root, "weaknesses"), 
                    Recommendations = ExtractStringArray(root, "recommendations"),
                    Rationale = root.TryGetProperty("rationale", out var rationale) && rationale.ValueKind == JsonValueKind.String
                        ? rationale.GetString() : null
                };
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to parse JSON from AI response: {ex.Message}. Content: {content}");
            }
        }

        private static List<string> ExtractStringArray(JsonElement root, string propertyName)
        {
            if (!root.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
                return new List<string>();

            var result = new List<string>();
            foreach (var item in property.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    var value = item.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                        result.Add(value.Trim());
                }
            }
            return result.Take(8).ToList(); // Limit to 8 items for safety
        }

        private static string GetDimensionsHash(IEnumerable<DimensionScore> dimensions)
        {
            var dims = dimensions.OrderBy(d => d.Dimension).ToList();
            var hashInput = string.Join("|", dims.Select(d => $"{d.Dimension}:{d.T:F1}"));
            return hashInput.GetHashCode().ToString();
        }
    }

    // OpenRouter API response models
    internal class OpenRouterResponse
    {
        public string? Id { get; set; }
        public string? Object { get; set; }
        public long Created { get; set; }
        public string? Model { get; set; }
        public List<Choice>? Choices { get; set; }
        public Usage? Usage { get; set; }
    }

    internal class Choice
    {
        public int Index { get; set; }
        public Message? Message { get; set; }
        public string? FinishReason { get; set; }
    }

    internal class Message
    {
        public string? Role { get; set; }
        public string? Content { get; set; }
    }

    internal class Usage
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
    }
}