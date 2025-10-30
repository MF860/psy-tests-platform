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
    public interface IDeepSeekClient
    {
        Task<AiAnalysisResponse> AnalyzeAsync(Result result, IEnumerable<DimensionScore> dimensions, CancellationToken cancellationToken = default);
    }

    public class DeepSeekClient : IDeepSeekClient
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<DeepSeekClient> _logger;
        private readonly DeepSeekConfiguration _config;
        
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public DeepSeekClient(
            HttpClient httpClient,
            IMemoryCache cache, 
            IOptions<DeepSeekConfiguration> config,
            ILogger<DeepSeekClient> logger)
        {
            _httpClient = httpClient;
            _cache = cache;
            _logger = logger;
            _config = config.Value;
            
            // Validate API key (never log full key)
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
            // Throw early if no API key
            if (string.IsNullOrWhiteSpace(_config.ApiKey))
            {
                throw new InvalidOperationException("DEEPSEEK_API_KEY not configured");
            }

            var cacheKey = $"ai_sdj_analysis_{result.Id}_{GetDimensionsHash(dimensions)}";
            
            // Check cache first
            if (_cache.TryGetValue(cacheKey, out AiAnalysisResponse? cachedResult) && cachedResult != null)
            {
                _logger.LogInformation("[AI] Cache hit for resultId={ResultId}", result.Id);
                return cachedResult;
            }

            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var prompt = BuildSdjPrompt(result, dimensions);
                
                // Log prompt length (not content - PII safety)
                _logger.LogInformation("[AI] Building prompt for resultId={ResultId}, length={Length} chars", 
                    result.Id, prompt.Length);
                
                var requestPayload = new
                {
                    model = _config.Model,
                    temperature = _config.Temperature,
                    max_tokens = _config.MaxTokens,
                    messages = new[]
                    {
                        new { role = "system", content = GetSdjSystemPrompt() },
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

                _logger.LogInformation("[AI] Analyze resultId={ResultId} model={Model} latencyMs={Latency} tokens={Tokens} status=OK", 
                    result.Id, _config.Model, stopwatch.Elapsed.TotalMilliseconds, result_response.Usage?.TotalTokens ?? 0);

                return result_response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "[AI] Analyze resultId={ResultId} model={Model} latencyMs={Latency} status=FAIL error={Error}", 
                    result.Id, _config.Model, stopwatch.Elapsed.TotalMilliseconds, ex.Message);
                throw;
            }
        }

        private async Task<DeepSeekResponse> SendWithRetryAsync(object payload, CancellationToken cancellationToken)
        {
            var json = JsonSerializer.Serialize(payload, JsonOptions);
            
            Exception? lastException = null;
            
            for (int attempt = 1; attempt <= _config.MaxRetries; attempt++)
            {
                try
                {
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync("/chat/completions", content, cancellationToken);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                        var deepSeekResponse = JsonSerializer.Deserialize<DeepSeekResponse>(responseJson, JsonOptions);
                        
                        if (deepSeekResponse != null)
                        {
                            return deepSeekResponse;
                        }
                        throw new InvalidOperationException("Failed to deserialize DeepSeek response");
                    }
                    
                    // Handle rate limiting and server errors with exponential backoff
                    if (response.StatusCode == HttpStatusCode.TooManyRequests ||
                        response.StatusCode >= HttpStatusCode.InternalServerError)
                    {
                        if (attempt < _config.MaxRetries)
                        {
                            var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // 2s, 4s, 8s
                            _logger.LogWarning("[AI] HTTP {StatusCode} on attempt {Attempt}/{MaxRetries}, retrying after {Delay}s", 
                                response.StatusCode, attempt, _config.MaxRetries, delay.TotalSeconds);
                            await Task.Delay(delay, cancellationToken);
                            continue;
                        }
                    }
                    
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("[AI] DeepSeek API returned {StatusCode}: {ErrorContent}", 
                        response.StatusCode, errorContent);
                    throw new HttpRequestException($"DeepSeek API error: {response.StatusCode} - {errorContent}");
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    lastException = ex;
                    
                    if (attempt < _config.MaxRetries)
                    {
                        var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                        _logger.LogWarning(ex, "[AI] Request failed on attempt {Attempt}/{MaxRetries}, retrying after {Delay}s", 
                            attempt, _config.MaxRetries, delay.TotalSeconds);
                        await Task.Delay(delay, cancellationToken);
                        continue;
                    }
                }
            }
            
            throw lastException ?? new InvalidOperationException("All retry attempts failed");
        }

        private static string GetSdjSystemPrompt()
        {
            return """
أنت محلّل سيكومتري محترف متخصص في تحليل نتائج إطار SDJ (الأبعاد السبعة للتنمية المستدامة).

**المهمة:**
حلّل نتائج الاختبار وقدم تحليلاً عربياً مبنياً على الـ T-Scores للأبعاد السبعة.

**القواعد الصارمة:**
1. أعد الإجابة بصيغة JSON فقط - بدون أي شرح أو نص إضافي
2. استخدم العربية الفصحى الواضحة في كل الحقول
3. لا تذكر أبداً أي بيانات شخصية (اسم، رقم وطني، عمر، إلخ)
4. اجعل كل توصية عملية وقابلة للتنفيذ خلال 4-6 أسابيع
5. استخدم معايير T-Score: فوق 60 = قوة، أقل من 40 = ضعف
6. ركز فقط على الأبعاد السبعة الرئيسية في التحليل

**البنية المطلوبة:**
{
  "strengths": ["...", "...", "..."],
  "weaknesses": ["...", "...", "..."],
  "recommendations": ["...", "...", "..."],
  "summary": "فقرة ملخص قصيرة (2-3 جمل)",
  "methodology": "جملة توضح أن التحليل مبني على SDJ T-scores",
  "categories": [
    {
      "name": "اسم البعد بالعربية",
      "t": 60.5,
      "note": "ملاحظة قصيرة عن هذا البعد"
    }
  ]
}

**الأبعاد السبعة (SDJ):**
1. الأنماط الشخصية
2. القدرات المعرفية والعقلية
3. الاتجاهات والقيم
4. المهارات العملية والتطبيقية
5. السلوك التكيفي والاجتماعي
6. الصحة النفسية والانفعالية
7. التوافق المهني والوظيفي

أعد JSON فقط - لا تضف تفسيرات قبل أو بعد JSON.
""";
        }

        private static string BuildSdjPrompt(Result result, IEnumerable<DimensionScore> dimensions)
        {
            var dims = dimensions.ToList();
            
            if (!dims.Any())
            {
                throw new InvalidOperationException("No dimension scores available for analysis");
            }

            var totalScore = dims.Sum(d => d.Raw);
            var avgT = dims.Average(d => d.T);
            
            // Top strengths (T >= 55)
            var topDims = dims.Where(d => d.T >= 55).OrderByDescending(d => d.T).Take(5)
                .Select(d => $"• {d.Dimension}: T={d.T:F1}").ToList();
                
            // Low areas (T < 45)
            var lowDims = dims.Where(d => d.T < 45).OrderBy(d => d.T).Take(5)
                .Select(d => $"• {d.Dimension}: T={d.T:F1}").ToList();

            var prompt = new StringBuilder();
            prompt.AppendLine("**تحليل نتائج SDJ (مجهول الهوية):**");
            prompt.AppendLine();
            prompt.AppendLine($"معرّف النتيجة: {result.Id}");
            prompt.AppendLine($"مجموع الدرجات الخام: {totalScore}");
            prompt.AppendLine($"متوسط T-Score: {avgT:F1}");
            prompt.AppendLine($"عدد الأبعاد: {dims.Count}");
            prompt.AppendLine();
            
            if (topDims.Any())
            {
                prompt.AppendLine("**أعلى الأبعاد (نقاط القوة):**");
                foreach (var dim in topDims)
                {
                    prompt.AppendLine(dim);
                }
                prompt.AppendLine();
            }
            
            if (lowDims.Any())
            {
                prompt.AppendLine("**أضعف الأبعاد (مجالات التطوير):**");
                foreach (var dim in lowDims)
                {
                    prompt.AppendLine(dim);
                }
                prompt.AppendLine();
            }

            prompt.AppendLine("**جميع الأبعاد السبعة:**");
            foreach (var dim in dims.OrderByDescending(d => d.T))
            {
                prompt.AppendLine($"• {dim.Dimension}: Raw={dim.Raw}, T={dim.T:F1}");
            }
            prompt.AppendLine();
            prompt.AppendLine("قدّم تحليلاً شاملاً بصيغة JSON حسب البنية المطلوبة.");

            return prompt.ToString();
        }

        private static AiAnalysisResult ParseResponse(DeepSeekResponse response)
        {
            if (response.Choices == null || !response.Choices.Any())
            {
                throw new InvalidOperationException("No choices in DeepSeek response");
            }

            var content = response.Choices[0].Message?.Content?.Trim();
            if (string.IsNullOrEmpty(content))
            {
                throw new InvalidOperationException("Empty content in DeepSeek response");
            }

            // Try to extract JSON if wrapped in markdown code blocks
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

                var result = new AiAnalysisResult
                {
                    Strengths = ExtractStringArray(root, "strengths"),
                    Weaknesses = ExtractStringArray(root, "weaknesses"), 
                    Recommendations = ExtractStringArray(root, "recommendations"),
                    Summary = root.TryGetProperty("summary", out var summary) && summary.ValueKind == JsonValueKind.String
                        ? summary.GetString() : null,
                    Methodology = root.TryGetProperty("methodology", out var methodology) && methodology.ValueKind == JsonValueKind.String
                        ? methodology.GetString() : null,
                    Rationale = root.TryGetProperty("rationale", out var rationale) && rationale.ValueKind == JsonValueKind.String
                        ? rationale.GetString() : null
                };

                // Parse categories array
                if (root.TryGetProperty("categories", out var categoriesElement) && categoriesElement.ValueKind == JsonValueKind.Array)
                {
                    var categories = new List<CategoryAnalysis>();
                    foreach (var cat in categoriesElement.EnumerateArray())
                    {
                        if (cat.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String &&
                            cat.TryGetProperty("t", out var t) && t.ValueKind == JsonValueKind.Number &&
                            cat.TryGetProperty("note", out var note) && note.ValueKind == JsonValueKind.String)
                        {
                            categories.Add(new CategoryAnalysis
                            {
                                Name = name.GetString() ?? "",
                                TScore = t.GetDouble(),
                                Note = note.GetString() ?? ""
                            });
                        }
                    }
                    result.Categories = categories;
                }

                return result;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to parse JSON from AI response: {ex.Message}. Content length: {content.Length}");
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
            return hashInput.GetHashCode().ToString("X8");
        }
    }

    // DeepSeek API response models (same structure as OpenAI)
    internal class DeepSeekResponse
    {
        public string? Id { get; set; }
        public string? Object { get; set; }
        public long Created { get; set; }
        public string? Model { get; set; }
        public List<DeepSeekChoice>? Choices { get; set; }
        public DeepSeekUsage? Usage { get; set; }
    }

    internal class DeepSeekChoice
    {
        public int Index { get; set; }
        public DeepSeekMessage? Message { get; set; }
        public string? FinishReason { get; set; }
    }

    internal class DeepSeekMessage
    {
        public string? Role { get; set; }
        public string? Content { get; set; }
    }

    internal class DeepSeekUsage
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
    }
}
