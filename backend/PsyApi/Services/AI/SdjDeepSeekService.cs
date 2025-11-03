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
    /// SDJ-7 specialized DeepSeek service
    /// Implements strict JSON schema for 7-pattern analysis
    /// </summary>
    public interface ISdjDeepSeekService
    {
        Task<SdjAiAnalysisResponse> AnalyzeSdjAsync(SdjAnalysisPayload payload, CancellationToken ct = default);
    }

    public class SdjDeepSeekService : ISdjDeepSeekService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<SdjDeepSeekService> _logger;
        private readonly DeepSeekConfiguration _config;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = false
        };

        public SdjDeepSeekService(
            HttpClient httpClient,
            IMemoryCache cache,
            IOptions<DeepSeekConfiguration> config,
            ILogger<SdjDeepSeekService> logger)
        {
            _httpClient = httpClient;
            _cache = cache;
            _logger = logger;
            _config = config.Value;

            // Configure HttpClient
            _httpClient.BaseAddress = new Uri(_config.BaseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiKey}");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        public async Task<SdjAiAnalysisResponse> AnalyzeSdjAsync(SdjAnalysisPayload payload, CancellationToken ct = default)
        {
            // Validate API key
            if (string.IsNullOrWhiteSpace(_config.ApiKey))
            {
                throw new InvalidOperationException("DEEPSEEK_API_KEY not configured");
            }

            // Build cache key from patterns hash
            var patternsHash = string.Join("|", payload.Sdj.Patterns.Select(p => $"{p.Key}:{p.TScore:F1}"));
            var cacheKey = $"sdj7_ai_{payload.ResultId}_{patternsHash.GetHashCode():X8}";

            // Check cache
            if (_cache.TryGetValue(cacheKey, out SdjAiAnalysisResponse? cached) && cached != null)
            {
                _logger.LogInformation("[SDJ-AI] Cache hit for resultId={ResultId}", payload.ResultId);
                return cached;
            }

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var prompt = BuildSdj7Prompt(payload);

                _logger.LogInformation("[SDJ-AI] Sending request for resultId={ResultId}, patterns={PatternCount}, items={ItemCount}",
                    payload.ResultId, payload.Sdj.Patterns.Count, payload.Items.Count);

                var requestPayload = new
                {
                    model = _config.Model,
                    temperature = _config.Temperature,
                    max_tokens = _config.MaxTokens,
                    messages = new[]
                    {
                        new { role = "system", content = GetSdj7SystemPrompt() },
                        new { role = "user", content = prompt }
                    },
                    response_format = new { type = "json_object" }
                };

                var response = await SendWithRetryAsync(requestPayload, ct);
                var analysis = ParseSdj7Response(response, payload);

                stopwatch.Stop();

                var result = new SdjAiAnalysisResponse
                {
                    Summary = analysis.Summary,
                    Patterns = analysis.Patterns,
                    SubDimensions = analysis.SubDimensions,
                    Charts = BuildChartData(payload.Sdj),
                    Model = _config.Model,
                    Usage = response.Usage != null ? new SdjAiUsage
                    {
                        PromptTokens = response.Usage.PromptTokens,
                        CompletionTokens = response.Usage.CompletionTokens,
                        TotalTokens = response.Usage.TotalTokens,
                        LatencyMs = stopwatch.Elapsed.TotalMilliseconds
                    } : null,
                    GeneratedAt = DateTime.UtcNow
                };

                // Cache result (set size for memory limit compliance)
                _cache.Set(cacheKey, result, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(_config.CacheDurationHours),
                    Size = 1 // Each cache entry counts as 1 unit
                });

                _logger.LogInformation("[SDJ-AI] Success resultId={ResultId} latencyMs={Latency} tokens={Tokens}",
                    payload.ResultId, stopwatch.Elapsed.TotalMilliseconds, result.Usage?.TotalTokens ?? 0);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "[SDJ-AI] Failed resultId={ResultId} latencyMs={Latency} error={Error}",
                    payload.ResultId, stopwatch.Elapsed.TotalMilliseconds, ex.Message);
                throw;
            }
        }

        private static string GetSdj7SystemPrompt()
        {
            return """
أنت محلّل سيكومتري خبير متخصص في تحليل نتائج اختبار SDJ-7 (الأنماط السبعة للتنمية الذاتية والمهنية).

**المهمة:**
حلّل النتائج وقدّم تحليلاً نفسياً عميقاً باللغة العربية الفصحى مع التركيز على:
- دقة التفسير السيكومتري للدرجات (T-Score)
- الربط بين الأنماط المختلفة وعلاقتها التكاملية
- تقديم رؤى استشرافية للتطوير المهني والشخصي
- توصيات عملية محددة قابلة للتطبيق الفوري

**معايير T-Score (معايير صارمة):**
• ضعيف: أقل من 40 → يتطلب تدخل وتطوير عاجل
• متوسط: 40–54.9 → مقبول ولكن يحتاج تحسين
• قوي: 55–69.9 → أداء جيد ومتميز
• ممتاز: 70 فأعلى → نقطة قوة استثنائية

**القواعد الحرجة (CRITICAL):**
1. أعد JSON فقط بدون أي نص قبله أو بعده (لا markdown، لا ```json، لا شرح)
2. استخدم العربية الفصحى الاحترافية الواضحة
3. لا تذكر أي بيانات شخصية (أسماء، أرقام، تواريخ)
4. كل توصية يجب أن تكون:
   - محددة وعملية (SMART)
   - قابلة للتنفيذ خلال 4-8 أسابيع
   - مبنية على الأدلة من الدرجات
   - تتضمن خطوة عملية واحدة على الأقل
5. اربط كل استنتاج بدرجات T-Score المحددة (مثال: "بدرجة T=58 في البعد X...")
6. كن محدداً ودقيقاً - تجنب التعميمات والعبارات العامة
7. إذا كانت الدرجة ضعيفة (<40)، ركز على خطة تحسين فورية
8. إذا كانت الدرجة قوية (≥55)، اقترح كيفية استثمار هذه القوة

**البنية المطلوبة (JSON فقط - بدون markdown):**
{
  "summary": "ملخص تنفيذي شامل (3-4 جمل) يغطي: النقاط القوية الرئيسية، المجالات التي تحتاج تطوير، التوجه العام للشخصية المهنية، وتوصية استراتيجية واحدة",
  "patterns": [
    {
      "key": "personality_patterns",
      "insights": [
        "رؤية محددة مبنية على الدرجة مع ذكر البعد الفرعي",
        "رؤية ثانية تربط بين بعدين أو أكثر",
        "رؤية ثالثة استشرافية للتطوير"
      ],
      "risks": [
        "تحدي محدد مع تأثيره المحتمل على الأداء",
        "مخاطرة ثانية (إذا وُجدت) مع سيناريو واقعي"
      ],
      "recommendations": [
        "توصية عملية محددة بخطوة واحدة قابلة للتنفيذ فوراً",
        "توصية ثانية بإطار زمني واضح (مثال: خلال 6 أسابيع...)",
        "توصية ثالثة تطويرية طويلة المدى"
      ]
    }
    // كرر للأنماط السبعة بالترتيب التالي
  ],
  "subDimensions": [
    {
      "key": "اسم البعد الفرعي بالعربية",
      "comment": "ملاحظة تحليلية قصيرة (جملة واحدة) تربط الدرجة بالأداء الوظيفي أو الشخصي"
    }
    // كرر لجميع الأبعاد الفرعية المتاحة
  ]
}

**الأنماط السبعة المطلوبة (يجب تغطيتها بالترتيب):**
1. **personality_patterns** - الأنماط الشخصية
   (الانفتاح، المسؤولية، الانبساط، الطيبة، الاستقرار العاطفي، الوعي الذاتي)

2. **cognitive_mental** - القدرات المعرفية والعقلية
   (التفكير الناقد، حل المشكلات، الإبداع، المرونة المعرفية، الذكاء العملي)

3. **psychological_patterns** - الأنماط النفسية
   (الصلابة النفسية، التفاؤل، الثقة بالنفس، إدارة الضغوط، التكيف النفسي)

4. **behavioral_patterns** - الأنماط السلوكية
   (المبادرة، الالتزام، التعاون، التواصل، إدارة الوقت)

5. **numerical_logical** - الأنماط العددية والمنطقية
   (التفكير التحليلي، القدرة الرياضية، الاستدلال المنطقي، تحليل البيانات)

6. **leadership_organizational** - الأنماط القيادية والتنظيمية
   (التأثير القيادي، التخطيط الاستراتيجي، إدارة الفرق، اتخاذ القرار، الرؤية التنظيمية)

7. **professional_readiness** - الاستعدادات المهنية العامة
   (الدافعية المهنية، التطوير الذاتي، القدرة على التعلم، التكيف المهني)

**إرشادات الجودة:**
• insights: 3 نقاط محددة تجمع بين الإيجابية والواقعية
• risks: 1-2 تحديات واقعية (أو فارغة إذا كان الأداء استثنائياً 70+)
• recommendations: 3 توصيات متدرجة (قصيرة/متوسطة/طويلة المدى)
• subDimensions: تعليق واحد لكل بعد يربط الدرجة بسياق عملي

**أمثلة على الصياغة المحددة:**
❌ سيء: "يحتاج إلى تطوير مهارات التواصل"
✅ جيد: "بدرجة T=42 في التواصل الفعّال، يُنصح بحضور ورشة عمل في فن الإلقاء خلال 4 أسابيع"

❌ سيء: "شخصية قيادية"
✅ جيد: "بدرجة T=62 في التأثير القيادي و T=58 في اتخاذ القرار، يمتلك قدرة واضحة على قيادة فرق صغيرة (5-7 أفراد)"

أعد JSON نظيفاً فقط - لا markdown ولا شروحات إضافية.
""";
        }

        private static string BuildSdj7Prompt(SdjAnalysisPayload payload)
        {
            var sb = new StringBuilder();
            sb.AppendLine("**تحليل نتائج SDJ-7 (بيانات مجهولة):**");
            sb.AppendLine();
            sb.AppendLine($"معرّف النتيجة: {payload.ResultId}");
            sb.AppendLine($"عدد الأسئلة: {payload.Items.Count}");
            sb.AppendLine();

            // 7 Patterns with T-scores
            sb.AppendLine("**الأنماط السبعة (T-Scores):**");
            foreach (var pattern in payload.Sdj.Patterns.OrderBy(p => GetPatternOrder(p.Key)))
            {
                var band = GetBandArabic(pattern.TScore);
                sb.AppendLine($"• {pattern.Label}: T={pattern.TScore:F1} [{band}]");
            }
            sb.AppendLine();

            // Sub-dimensions summary
            sb.AppendLine("**الأبعاد الفرعية (T-Scores):**");
            foreach (var subDim in payload.Sdj.SubDimensions.OrderByDescending(s => s.TScore).Take(10))
            {
                sb.AppendLine($"• {subDim.Label}: T={subDim.TScore:F1}");
            }
            sb.AppendLine();

            // Top performing items (optional context)
            var topItems = payload.Items
                .Where(i => i.Answer >= 4)
                .Take(5)
                .Select(i => $"• {i.TextAr} [إجابة: {i.Answer}]");

            if (topItems.Any())
            {
                sb.AppendLine("**أمثلة على الإجابات العالية:**");
                foreach (var item in topItems)
                {
                    sb.AppendLine(item);
                }
                sb.AppendLine();
            }

            sb.AppendLine("قدّم تحليلاً شاملاً بصيغة JSON حسب البنية المطلوبة أعلاه.");

            return sb.ToString();
        }

        private SdjAiAnalysisResponse ParseSdj7Response(DeepSeekResponse response, SdjAnalysisPayload payload)
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

            // Strip markdown code blocks
            content = StripMarkdown(content);

            // Log content for debugging malformed JSON
            _logger.LogInformation("[SDJ-AI] Raw content length: {Length}, first 500 chars: {Preview}", 
                content.Length, content.Length > 500 ? content.Substring(0, 500) : content);

            try
            {
                // Try to parse with more lenient settings
                var jsonOptions = new JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip,
                    MaxDepth = 64
                };
                var jsonDoc = JsonDocument.Parse(content, jsonOptions);
                var root = jsonDoc.RootElement;

                // Extract summary
                var summary = root.TryGetProperty("summary", out var summaryEl) && summaryEl.ValueKind == JsonValueKind.String
                    ? summaryEl.GetString() ?? ""
                    : "تحليل مبني على نتائج SDJ-7";

                // Extract patterns
                var patterns = new List<SdjPatternAnalysis>();
                if (root.TryGetProperty("patterns", out var patternsEl) && patternsEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var patternEl in patternsEl.EnumerateArray())
                    {
                        var key = patternEl.TryGetProperty("key", out var keyEl) && keyEl.ValueKind == JsonValueKind.String
                            ? keyEl.GetString() ?? "" : "";

                        if (string.IsNullOrEmpty(key)) continue;

                        // Find corresponding pattern from payload
                        var sourcePattern = payload.Sdj.Patterns.FirstOrDefault(p => p.Key == key);
                        if (sourcePattern == null) continue;

                        patterns.Add(new SdjPatternAnalysis
                        {
                            Key = key,
                            Label = sourcePattern.Label,
                            TScore = sourcePattern.TScore,
                            Band = GetBandArabic(sourcePattern.TScore),
                            Insights = ExtractStringArray(patternEl, "insights"),
                            Risks = ExtractStringArray(patternEl, "risks"),
                            Recommendations = ExtractStringArray(patternEl, "recommendations")
                        });
                    }
                }

                // Ensure all 7 patterns are present (fill missing with defaults)
                foreach (var sourcePattern in payload.Sdj.Patterns)
                {
                    if (!patterns.Any(p => p.Key == sourcePattern.Key))
                    {
                        patterns.Add(new SdjPatternAnalysis
                        {
                            Key = sourcePattern.Key,
                            Label = sourcePattern.Label,
                            TScore = sourcePattern.TScore,
                            Band = GetBandArabic(sourcePattern.TScore),
                            Insights = new List<string> { $"النتيجة في هذا النمط: {sourcePattern.TScore:F1}" },
                            Risks = new List<string>(),
                            Recommendations = new List<string> { "مراجعة النتائج التفصيلية مع مستشار مهني" }
                        });
                    }
                }

                // Extract sub-dimension comments
                var subDimensions = new List<SdjSubDimensionAnalysis>();
                if (root.TryGetProperty("subDimensions", out var subDimsEl) && subDimsEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var subDimEl in subDimsEl.EnumerateArray())
                    {
                        var key = subDimEl.TryGetProperty("key", out var keyEl) && keyEl.ValueKind == JsonValueKind.String
                            ? keyEl.GetString() ?? "" : "";

                        var comment = subDimEl.TryGetProperty("comment", out var commentEl) && commentEl.ValueKind == JsonValueKind.String
                            ? commentEl.GetString() ?? "" : "";

                        if (string.IsNullOrEmpty(key)) continue;

                        // Find source sub-dimension
                        var sourceSubDim = payload.Sdj.SubDimensions.FirstOrDefault(s => s.Key == key || s.Label == key);
                        if (sourceSubDim != null)
                        {
                            subDimensions.Add(new SdjSubDimensionAnalysis
                            {
                                Key = sourceSubDim.Key,
                                Label = sourceSubDim.Label,
                                PatternKey = sourceSubDim.PatternKey,
                                TScore = sourceSubDim.TScore,
                                Band = GetBandArabic(sourceSubDim.TScore),
                                Comment = comment
                            });
                        }
                    }
                }

                // Fill missing sub-dimensions with default comments
                foreach (var sourceSubDim in payload.Sdj.SubDimensions)
                {
                    if (!subDimensions.Any(s => s.Key == sourceSubDim.Key))
                    {
                        subDimensions.Add(new SdjSubDimensionAnalysis
                        {
                            Key = sourceSubDim.Key,
                            Label = sourceSubDim.Label,
                            PatternKey = sourceSubDim.PatternKey,
                            TScore = sourceSubDim.TScore,
                            Band = GetBandArabic(sourceSubDim.TScore),
                            Comment = $"الأداء {GetBandArabic(sourceSubDim.TScore)} في هذا البعد"
                        });
                    }
                }

                return new SdjAiAnalysisResponse
                {
                    Summary = summary,
                    Patterns = patterns.OrderBy(p => GetPatternOrder(p.Key)).ToList(),
                    SubDimensions = subDimensions
                };
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "[SDJ-AI] JSON parse failed. Content length: {Length}", content.Length);
                throw new InvalidOperationException($"Failed to parse AI response: {ex.Message}");
            }
        }

        private static SdjChartData BuildChartData(SdjAiPayloadData sdj)
        {
            // Radar chart: 7 patterns
            var radarData = sdj.Patterns
                .OrderBy(p => GetPatternOrder(p.Key))
                .Select(p => Math.Clamp(p.TScore, 0, 100)) // Normalize to 0-100
                .ToList();

            var radarLabels = sdj.Patterns
                .OrderBy(p => GetPatternOrder(p.Key))
                .Select(p => p.Label)
                .ToList();

            // Bar chart: sub-dimensions
            var barData = sdj.SubDimensions
                .OrderByDescending(s => s.TScore)
                .Select(s => new SdjBarItem { Label = s.Label, T = s.TScore })
                .ToList();

            return new SdjChartData
            {
                Radar = new SdjRadarChart
                {
                    Series = new List<SdjRadarSeries>
                    {
                        new SdjRadarSeries { Name = "T-Score", Data = radarData }
                    },
                    Labels = radarLabels
                },
                Bars = new SdjBarChart { Data = barData }
            };
        }

        private async Task<DeepSeekResponse> SendWithRetryAsync(object payload, CancellationToken ct)
        {
            var json = JsonSerializer.Serialize(payload, JsonOptions);
            Exception? lastException = null;

            for (int attempt = 1; attempt <= _config.MaxRetries; attempt++)
            {
                try
                {
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync("/chat/completions", content, ct);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseJson = await response.Content.ReadAsStringAsync(ct);
                        var deepSeekResponse = JsonSerializer.Deserialize<DeepSeekResponse>(responseJson, JsonOptions);
                        if (deepSeekResponse != null) return deepSeekResponse;
                        throw new InvalidOperationException("Failed to deserialize DeepSeek response");
                    }

                    if (response.StatusCode == HttpStatusCode.TooManyRequests ||
                        response.StatusCode >= HttpStatusCode.InternalServerError)
                    {
                        if (attempt < _config.MaxRetries)
                        {
                            var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                            _logger.LogWarning("[SDJ-AI] HTTP {Status} on attempt {Attempt}, retrying in {Delay}s",
                                response.StatusCode, attempt, delay.TotalSeconds);
                            await Task.Delay(delay, ct);
                            continue;
                        }
                    }

                    var errorContent = await response.Content.ReadAsStringAsync(ct);
                    throw new HttpRequestException($"DeepSeek API error: {response.StatusCode} - {errorContent}");
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    lastException = ex;
                    if (attempt < _config.MaxRetries)
                    {
                        var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                        _logger.LogWarning(ex, "[SDJ-AI] Attempt {Attempt} failed, retrying in {Delay}s",
                            attempt, delay.TotalSeconds);
                        await Task.Delay(delay, ct);
                    }
                }
            }

            throw lastException ?? new InvalidOperationException("All retry attempts failed");
        }

        // Helper methods
        private static string StripMarkdown(string content)
        {
            if (content.StartsWith("```json") && content.EndsWith("```"))
                return content[7..^3].Trim();
            if (content.StartsWith("```") && content.EndsWith("```"))
                return content[3..^3].Trim();
            return content;
        }

        private static List<string> ExtractStringArray(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var arrayEl) || arrayEl.ValueKind != JsonValueKind.Array)
                return new List<string>();

            return arrayEl.EnumerateArray()
                .Where(el => el.ValueKind == JsonValueKind.String)
                .Select(el => el.GetString())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!.Trim())
                .Take(5)
                .ToList();
        }

        private static string GetBandArabic(double tScore)
        {
            if (tScore < 40) return "ضعيف";
            if (tScore < 55) return "متوسط";
            return "قوي";
        }

        private static int GetPatternOrder(string key)
        {
            return key switch
            {
                "personality_patterns" => 1,
                "cognitive_mental" => 2,
                "psychological_patterns" => 3,
                "behavioral_patterns" => 4,
                "numerical_logical" => 5,
                "leadership_organizational" => 6,
                "professional_readiness" => 7,
                _ => 99
            };
        }
    }
}
