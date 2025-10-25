using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using PsyApi.Models;

namespace PsyApi.Services.AI
{
    /// <summary>
    /// OpenAI service configuration
    /// </summary>
    public class OpenAIConfiguration
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "gpt-4o-mini";
        public string BaseUrl { get; set; } = "https://api.openai.com/v1";
        public int MaxTokens { get; set; } = 2000;
        public double Temperature { get; set; } = 0.3;
        public int TimeoutSeconds { get; set; } = 30;
        public int MaxRetries { get; set; } = 3;
        public int CacheDurationHours { get; set; } = 24;
        public bool EnableCaching { get; set; } = true;
    }

    /// <summary>
    /// Interface for OpenAI recommendations service
    /// </summary>
    public interface IOpenAIRecommendationsService
    {
        Task<RecommendationsResponse> GenerateRecommendationsAsync(
            int resultId,
            string participantId,
            List<DimensionScore> dimensions,
            double totalScore,
            string? context = null,
            bool forceRegenerate = false,
            CancellationToken cancellationToken = default);
            
        Task<OpenAIUsageMetrics> GetUsageMetricsAsync();
        Task<bool> ValidateApiKeyAsync();
    }

    /// <summary>
    /// OpenAI service for generating personalized recommendations using GPT-4o-mini
    /// </summary>
    public class OpenAIRecommendationsService : IOpenAIRecommendationsService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly OpenAIConfiguration _config;
        private readonly ILogger<OpenAIRecommendationsService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        private static readonly string CACHE_KEY_PREFIX = "openai_recommendations_";
        private static readonly string USAGE_CACHE_KEY = "openai_usage_metrics";

        public OpenAIRecommendationsService(
            HttpClient httpClient,
            IMemoryCache cache,
            IOptions<OpenAIConfiguration> config,
            ILogger<OpenAIRecommendationsService> logger)
        {
            _httpClient = httpClient;
            _cache = cache;
            _config = config.Value;
            _logger = logger;

            // Configure JSON serialization for Arabic support
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            // Configure HTTP client
            var baseUrl = _config.BaseUrl.TrimEnd('/') + "/";
            _httpClient.BaseAddress = new Uri(baseUrl);
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", _config.ApiKey);
            _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);
            
            // Add OpenRouter specific headers if using OpenRouter
            if (_config.BaseUrl.Contains("openrouter.ai"))
            {
                _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://psyapi.local");
                _httpClient.DefaultRequestHeaders.Add("X-Title", "PsyAPI Psychological Testing Platform");
            }
        }

        public async Task<RecommendationsResponse> GenerateRecommendationsAsync(
            int resultId,
            string participantId,
            List<DimensionScore> dimensions,
            double totalScore,
            string? context = null,
            bool forceRegenerate = false,
            CancellationToken cancellationToken = default)
        {
            var cacheKey = $"{CACHE_KEY_PREFIX}{resultId}";
            
            // Check cache first (unless force regenerate is requested)
            if (!forceRegenerate && _config.EnableCaching && 
                _cache.TryGetValue(cacheKey, out RecommendationsCacheEntry? cachedEntry) &&
                cachedEntry != null && cachedEntry.ExpiresAt > DateTime.UtcNow)
            {
                _logger.LogInformation("Returning cached recommendations for Result {ResultId}", resultId);
                cachedEntry.Data.FromCache = true;
                return cachedEntry.Data;
            }

            var startTime = DateTime.UtcNow;
            
            try
            {
                // Generate recommendations using OpenAI
                var recommendations = await GenerateRecommendationsInternalAsync(
                    resultId, participantId, dimensions, totalScore, context, cancellationToken);
                
                var generationTime = DateTime.UtcNow - startTime;
                
                // Cache the results
                if (_config.EnableCaching)
                {
                    var cacheEntry = new RecommendationsCacheEntry
                    {
                        ResultId = resultId,
                        Data = recommendations,
                        CachedAt = DateTime.UtcNow,
                        ExpiresAt = DateTime.UtcNow.AddHours(_config.CacheDurationHours),
                        ModelVersion = _config.Model,
                        TokensUsed = recommendations.Summary?.KeyCharacteristics?.Count ?? 0, // Approximate
                        GenerationTime = generationTime
                    };
                    
                    _cache.Set(cacheKey, cacheEntry, TimeSpan.FromHours(_config.CacheDurationHours));
                }

                // Update usage metrics
                UpdateUsageMetrics(1, recommendations);

                _logger.LogInformation(
                    "Generated recommendations for Result {ResultId} in {Duration}ms", 
                    resultId, generationTime.TotalMilliseconds);

                return recommendations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate recommendations for Result {ResultId}", resultId);
                throw;
            }
        }

        private async Task<RecommendationsResponse> GenerateRecommendationsInternalAsync(
            int resultId,
            string participantId,
            List<DimensionScore> dimensions,
            double totalScore,
            string? context,
            CancellationToken cancellationToken)
        {
            // Prepare the prompt for OpenAI
            var prompt = BuildAnalysisPrompt(dimensions, totalScore, context);
            
            var request = new OpenAIRequest
            {
                Model = _config.Model,
                Messages = new List<OpenAIMessage>
                {
                    new OpenAIMessage 
                    { 
                        Role = "system", 
                        Content = GetSystemPrompt() 
                    },
                    new OpenAIMessage 
                    { 
                        Role = "user", 
                        Content = prompt 
                    }
                },
                MaxTokens = _config.MaxTokens,
                Temperature = _config.Temperature
            };

            var response = await CallOpenAIWithRetryAsync(request, cancellationToken);
            
            // Parse the AI response and structure it
            var recommendations = ParseOpenAIResponse(response, resultId, participantId);
            
            return recommendations;
        }

        private string GetSystemPrompt()
        {
            return @"أنت خبير في علم النفس والتطوير المهني متخصص في تحليل النتائج النفسية وتقديم التوصيات المخصصة باللغة العربية.

مهامك:
1. تحليل نتائج الاختبارات النفسية (T-Scores)
2. تحديد نقاط القوة والضعف
3. تقديم توصيات تطويرية مخصصة
4. اقتراح دورات تدريبية مناسبة

معايير T-Score:
- 70+: استثنائي
- 60-69: فوق المتوسط 
- 40-59: متوسط
- 30-39: دون المتوسط
- أقل من 30: يحتاج متابعة

يجب أن تكون التوصيات:
- باللغة العربية الفصحى
- عملية وقابلة للتطبيق
- مخصصة للسياق العسكري/الأمني
- مركزة على التطوير المهني
- مراعية للثقافة العربية

قدم إجابتك بتنسيق JSON صحيح.";
        }

        private string BuildAnalysisPrompt(List<DimensionScore> dimensions, double totalScore, string? context)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("تحليل نتائج الاختبار النفسي:");
            sb.AppendLine($"النتيجة الإجمالية: {totalScore:F1}");
            sb.AppendLine();
            
            sb.AppendLine("نتائج الأبعاد (T-Scores):");
            foreach (var dim in dimensions.OrderByDescending(d => d.T))
            {
                sb.AppendLine($"- {dim.Dimension}: {dim.T:F1}");
            }
            
            if (!string.IsNullOrEmpty(context))
            {
                sb.AppendLine();
                sb.AppendLine($"السياق الإضافي: {context}");
            }
            
            sb.AppendLine();
            sb.AppendLine(@"المطلوب: تحليل شامل وتوصيات مخصصة بتنسيق JSON التالي:
{
  ""summary"": {
    ""profile_type"": ""نوع الشخصية"",
    ""description"": ""وصف شامل للشخصية"",
    ""overall_score"": النتيجة_الإجمالية,
    ""performance_level"": ""مستوى الأداء"",
    ""key_characteristics"": [""خاصية1"", ""خاصية2"", ""خاصية3""]
  },
  ""strengths"": [
    {
      ""dimension"": ""اسم البعد"",
      ""t_score"": النتيجة,
      ""title"": ""عنوان نقطة القوة"",
      ""description"": ""وصف تفصيلي"",
      ""applications"": [""تطبيق1"", ""تطبيق2""],
      ""reinforcement_tip"": ""نصيحة للتعزيز""
    }
  ],
  ""growth_areas"": [
    {
      ""dimension"": ""اسم البعد"",
      ""t_score"": النتيجة,
      ""title"": ""عنوان مجال التطوير"",
      ""challenge"": ""التحدي الرئيسي"",
      ""improvement_strategies"": [""استراتيجية1"", ""استراتيجية2""],
      ""priority"": ""High/Medium/Low""
    }
  ],
  ""recommendations"": [
    {
      ""category"": ""فئة التوصية"",
      ""title"": ""عنوان التوصية"",
      ""description"": ""وصف التوصية"",
      ""action_steps"": [""خطوة1"", ""خطوة2""],
      ""timeline"": ""المدة الزمنية"",
      ""priority"": ""الأولوية"",
      ""expected_outcomes"": [""نتيجة1"", ""نتيجة2""]
    }
  ],
  ""courses"": [
    {
      ""name"": ""اسم الدورة"",
      ""description"": ""وصف الدورة"",
      ""provider"": ""مقدم الدورة"",
      ""duration"": ""مدة الدورة"",
      ""level"": ""المستوى"",
      ""topics"": [""موضوع1"", ""موضوع2""],
      ""relevancy_reason"": ""سبب الصلة""
    }
  ]
}");

            return sb.ToString();
        }

        private async Task<string> CallOpenAIWithRetryAsync(OpenAIRequest request, CancellationToken cancellationToken)
        {
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var retries = 0;

            while (retries <= _config.MaxRetries)
            {
                try
                {
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync("chat/completions", content, cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                        var openAIResponse = JsonSerializer.Deserialize<OpenAIResponse>(responseContent, _jsonOptions);
                        
                        return openAIResponse?.Choices?.FirstOrDefault()?.Message?.Content ?? 
                               throw new InvalidOperationException("Empty response from OpenAI");
                    }

                    var error = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning("OpenAI API error (attempt {Attempt}): {StatusCode} - {Error}", 
                        retries + 1, response.StatusCode, error);
                    
                    if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        // Rate limiting - wait before retry
                        var delay = TimeSpan.FromSeconds(Math.Pow(2, retries) * 2); // Exponential backoff
                        await Task.Delay(delay, cancellationToken);
                    }
                    else if (!IsRetryableError(response.StatusCode))
                    {
                        throw new HttpRequestException($"OpenAI API error: {response.StatusCode} - {error}");
                    }
                }
                catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex) when (retries < _config.MaxRetries)
                {
                    _logger.LogWarning(ex, "OpenAI request failed (attempt {Attempt}), retrying...", retries + 1);
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retries)), cancellationToken);
                }

                retries++;
            }

            throw new InvalidOperationException($"Failed to get response from OpenAI after {_config.MaxRetries + 1} attempts");
        }

        private static bool IsRetryableError(System.Net.HttpStatusCode statusCode)
        {
            return statusCode == System.Net.HttpStatusCode.TooManyRequests ||
                   statusCode == System.Net.HttpStatusCode.InternalServerError ||
                   statusCode == System.Net.HttpStatusCode.BadGateway ||
                   statusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
                   statusCode == System.Net.HttpStatusCode.GatewayTimeout;
        }

        private RecommendationsResponse ParseOpenAIResponse(string aiResponse, int resultId, string participantId)
        {
            try
            {
                // Try to extract JSON from the response (in case there's extra text)
                var jsonStart = aiResponse.IndexOf('{');
                var jsonEnd = aiResponse.LastIndexOf('}');
                
                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    var jsonContent = aiResponse.Substring(jsonStart, jsonEnd - jsonStart + 1);
                    var aiData = JsonSerializer.Deserialize<OpenAIRecommendationData>(jsonContent, _jsonOptions);
                    
                    if (aiData != null)
                    {
                        return MapToRecommendationsResponse(aiData, resultId, participantId);
                    }
                }
                
                _logger.LogWarning("Could not parse OpenAI response as JSON for Result {ResultId}", resultId);
                return CreateFallbackResponse(resultId, participantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing OpenAI response for Result {ResultId}", resultId);
                return CreateFallbackResponse(resultId, participantId);
            }
        }

        private RecommendationsResponse MapToRecommendationsResponse(
            OpenAIRecommendationData aiData, 
            int resultId, 
            string participantId)
        {
            return new RecommendationsResponse
            {
                ResultId = resultId,
                ParticipantId = participantId,
                GeneratedAt = DateTime.UtcNow,
                ModelVersion = _config.Model,
                FromCache = false,
                Summary = new OverallSummary
                {
                    ProfileType = aiData.Summary?.ProfileType ?? "",
                    Description = aiData.Summary?.Description ?? "",
                    OverallScore = aiData.Summary?.OverallScore ?? 0,
                    PerformanceLevel = aiData.Summary?.PerformanceLevel ?? "",
                    KeyCharacteristics = aiData.Summary?.KeyCharacteristics ?? new List<string>()
                },
                Strengths = aiData.Strengths?.Select(s => new StrengthInsight
                {
                    Dimension = s.Dimension ?? "",
                    TScore = s.TScore,
                    Title = s.Title ?? "",
                    Description = s.Description ?? "",
                    Applications = s.Applications ?? new List<string>(),
                    ReinforcementTip = s.ReinforcementTip ?? ""
                }).ToList() ?? new List<StrengthInsight>(),
                GrowthAreas = aiData.GrowthAreas?.Select(g => new GrowthArea
                {
                    Dimension = g.Dimension ?? "",
                    TScore = g.TScore,
                    Title = g.Title ?? "",
                    Challenge = g.Challenge ?? "",
                    ImprovementStrategies = g.ImprovementStrategies ?? new List<string>(),
                    Priority = g.Priority ?? "Medium"
                }).ToList() ?? new List<GrowthArea>(),
                Recommendations = aiData.Recommendations?.Select(r => new DevelopmentRecommendation
                {
                    Category = r.Category ?? "",
                    Title = r.Title ?? "",
                    Description = r.Description ?? "",
                    ActionSteps = r.ActionSteps ?? new List<string>(),
                    Timeline = r.Timeline ?? "",
                    Priority = r.Priority ?? "Medium",
                    ExpectedOutcomes = r.ExpectedOutcomes ?? new List<string>()
                }).ToList() ?? new List<DevelopmentRecommendation>(),
                Courses = aiData.Courses?.Select(c => new CourseRecommendation
                {
                    Name = c.Name ?? "",
                    Description = c.Description ?? "",
                    Provider = c.Provider ?? "",
                    Duration = c.Duration ?? "",
                    Level = c.Level ?? "",
                    Topics = c.Topics ?? new List<string>(),
                    RelevancyReason = c.RelevancyReason ?? ""
                }).ToList() ?? new List<CourseRecommendation>()
            };
        }

        private RecommendationsResponse CreateFallbackResponse(int resultId, string participantId)
        {
            return new RecommendationsResponse
            {
                ResultId = resultId,
                ParticipantId = participantId,
                GeneratedAt = DateTime.UtcNow,
                ModelVersion = "fallback",
                FromCache = false,
                Summary = new OverallSummary
                {
                    ProfileType = "تحليل أساسي",
                    Description = "لم يتمكن النظام من توليد تحليل مفصل، يُنصح بالمراجعة اليدوية.",
                    PerformanceLevel = "يحتاج مراجعة",
                    KeyCharacteristics = new List<string> { "تحليل غير مكتمل" }
                },
                Recommendations = new List<DevelopmentRecommendation>
                {
                    new DevelopmentRecommendation
                    {
                        Category = "عام",
                        Title = "مراجعة النتائج",
                        Description = "يُنصح بمراجعة النتائج مع مختص للحصول على تحليل أكثر دقة.",
                        ActionSteps = new List<string> { "حجز موعد مع مختص", "مراجعة النتائج التفصيلية" },
                        Timeline = "أسبوع واحد",
                        Priority = "High"
                    }
                }
            };
        }

        private void UpdateUsageMetrics(int requestCount, RecommendationsResponse response)
        {
            // This is a simplified metrics tracking - in production, you'd want more sophisticated tracking
            var metrics = _cache.Get<OpenAIUsageMetrics>(USAGE_CACHE_KEY) ?? new OpenAIUsageMetrics();
            
            metrics.RequestsToday += requestCount;
            metrics.TokensUsedToday += response.Recommendations?.Count * 50 ?? 0; // Rough estimate
            metrics.LastRequest = DateTime.UtcNow;
            
            _cache.Set(USAGE_CACHE_KEY, metrics, TimeSpan.FromDays(1));
        }

        public Task<OpenAIUsageMetrics> GetUsageMetricsAsync()
        {
            var metrics = _cache.Get<OpenAIUsageMetrics>(USAGE_CACHE_KEY) ?? new OpenAIUsageMetrics();
            return Task.FromResult(metrics);
        }

        public async Task<bool> ValidateApiKeyAsync()
        {
            try
            {
                var request = new OpenAIRequest
                {
                    Model = _config.Model,
                    Messages = new List<OpenAIMessage>
                    {
                        new OpenAIMessage { Role = "user", Content = "Hello" }
                    },
                    MaxTokens = 10
                };

                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/chat/completions", content);
                
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }

    // OpenAI API Models
    internal class OpenAIRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;
        
        [JsonPropertyName("messages")]
        public List<OpenAIMessage> Messages { get; set; } = new();
        
        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }
        
        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }
    }

    internal class OpenAIMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;
        
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    internal class OpenAIResponse
    {
        [JsonPropertyName("choices")]
        public List<OpenAIChoice>? Choices { get; set; }
    }

    internal class OpenAIChoice
    {
        [JsonPropertyName("message")]
        public OpenAIMessage? Message { get; set; }
    }

    // Data structure for parsing AI response
    internal class OpenAIRecommendationData
    {
        [JsonPropertyName("summary")]
        public OpenAISummary? Summary { get; set; }
        
        [JsonPropertyName("strengths")]
        public List<OpenAIStrength>? Strengths { get; set; }
        
        [JsonPropertyName("growth_areas")]
        public List<OpenAIGrowthArea>? GrowthAreas { get; set; }
        
        [JsonPropertyName("recommendations")]
        public List<OpenAIRecommendation>? Recommendations { get; set; }
        
        [JsonPropertyName("courses")]
        public List<OpenAICourse>? Courses { get; set; }
    }

    internal class OpenAISummary
    {
        [JsonPropertyName("profile_type")]
        public string? ProfileType { get; set; }
        
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        
        [JsonPropertyName("overall_score")]
        public double OverallScore { get; set; }
        
        [JsonPropertyName("performance_level")]
        public string? PerformanceLevel { get; set; }
        
        [JsonPropertyName("key_characteristics")]
        public List<string>? KeyCharacteristics { get; set; }
    }

    internal class OpenAIStrength
    {
        [JsonPropertyName("dimension")]
        public string? Dimension { get; set; }
        
        [JsonPropertyName("t_score")]
        public double TScore { get; set; }
        
        [JsonPropertyName("title")]
        public string? Title { get; set; }
        
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        
        [JsonPropertyName("applications")]
        public List<string>? Applications { get; set; }
        
        [JsonPropertyName("reinforcement_tip")]
        public string? ReinforcementTip { get; set; }
    }

    internal class OpenAIGrowthArea
    {
        [JsonPropertyName("dimension")]
        public string? Dimension { get; set; }
        
        [JsonPropertyName("t_score")]
        public double TScore { get; set; }
        
        [JsonPropertyName("title")]
        public string? Title { get; set; }
        
        [JsonPropertyName("challenge")]
        public string? Challenge { get; set; }
        
        [JsonPropertyName("improvement_strategies")]
        public List<string>? ImprovementStrategies { get; set; }
        
        [JsonPropertyName("priority")]
        public string? Priority { get; set; }
    }

    internal class OpenAIRecommendation
    {
        [JsonPropertyName("category")]
        public string? Category { get; set; }
        
        [JsonPropertyName("title")]
        public string? Title { get; set; }
        
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        
        [JsonPropertyName("action_steps")]
        public List<string>? ActionSteps { get; set; }
        
        [JsonPropertyName("timeline")]
        public string? Timeline { get; set; }
        
        [JsonPropertyName("priority")]
        public string? Priority { get; set; }
        
        [JsonPropertyName("expected_outcomes")]
        public List<string>? ExpectedOutcomes { get; set; }
    }

    internal class OpenAICourse
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        
        [JsonPropertyName("provider")]
        public string? Provider { get; set; }
        
        [JsonPropertyName("duration")]
        public string? Duration { get; set; }
        
        [JsonPropertyName("level")]
        public string? Level { get; set; }
        
        [JsonPropertyName("topics")]
        public List<string>? Topics { get; set; }
        
        [JsonPropertyName("relevancy_reason")]
        public string? RelevancyReason { get; set; }
    }
}