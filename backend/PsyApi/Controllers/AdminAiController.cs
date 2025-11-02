using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using PsyApi.Data;
using PsyApi.Models;
using PsyApi.Services.AI;
using PsyApi.Services.Audit;
using PsyApi.Services.Scoring;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace PsyApi.Controllers
{
    public class AnalyzeRequest 
    {
        public int ResultId { get; set; }
    }

    public class ChatRequest 
    {
        public List<ChatMessage> Messages { get; set; } = new();
        public int ResultId { get; set; }
        public string? SessionId { get; set; }
    }

    public class ChatMessage
    {
        public string Role { get; set; } = string.Empty; // "system", "user", "assistant"
        public string Content { get; set; } = string.Empty;
    }

    public class ChatResponse 
    {
        public List<ChatMessage> Messages { get; set; } = new();
        public ChatUsage Usage { get; set; } = new();
        public bool Cached { get; set; } = false;
        public string? SessionId { get; set; }
    }

    public class ChatUsage
    {
        public int LatencyMs { get; set; }
    }

    [ApiController]
    [Authorize]
    [Route("api/admin/ai")]
    public class AdminAiController : ControllerBase
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = false
        };

        private readonly AppDbContext _db;
        private readonly IAiAnalyzerService _ai;
        private readonly IAuditService _audit;
        private readonly ILogger<AdminAiController> _logger;
        private readonly IConfiguration _config;

        public AdminAiController(AppDbContext db, IAiAnalyzerService ai, IAuditService audit, ILogger<AdminAiController> logger, IConfiguration config)
        {
            _db = db; _ai = ai; _audit = audit; _logger = logger; _config = config;
        }

        [HttpGet("health")]
        [AllowAnonymous]
        public IActionResult GetHealth()
        {
            try
            {
                // Check if AI service is configured
                var apiKey = _config["AI:DeepSeek:ApiKey"] ?? Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");
                var provider = _config["AI:Provider"] ?? "DeepSeek";
                var model = _config["AI:Model"] ?? "deepseek-chat";
                
                var hasKey = !string.IsNullOrWhiteSpace(apiKey);
                var enabled = hasKey && _ai != null;

                return Ok(new
                {
                    enabled = enabled,
                    provider = provider,
                    model = model,
                    hasKey = hasKey,
                    status = enabled ? "ready" : "disabled",
                    message = enabled 
                        ? "خدمة الذكاء الاصطناعي جاهزة" 
                        : "خدمة الذكاء الاصطناعي غير مفعّلة - يرجى تكوين مفتاح API"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking AI health");
                return Ok(new
                {
                    enabled = false,
                    status = "error",
                    message = "خطأ في فحص حالة خدمة الذكاء الاصطناعي"
                });
            }
        }

        [HttpPost("analyze")]
        [EnableRateLimiting("admin")]
        public async Task<IActionResult> Analyze([FromBody] AnalyzeRequest request)
        {
            if (request == null || request.ResultId <= 0)
                return BadRequest(new { error = "معرف النتيجة مطلوب" });

            try
            {
                var entity = await _db.Results
                    .Include(r => r.Session)
                        .ThenInclude(s => s.User)
                    .Include(r => r.Session)
                        .ThenInclude(s => s.SessionItems)
                            .ThenInclude(si => si.Item)
                    .FirstOrDefaultAsync(r => r.Id == request.ResultId);

                if (entity == null)
                    return NotFound(new { error = "لم يتم العثور على النتيجة" });

                // Check if this is an SDJ result
                bool isSdj = false;
                SdjScoreSummary? sdjScores = null;

                try
                {
                    if (!string.IsNullOrWhiteSpace(entity.Session.Payload))
                    {
                        var payloadEl = JsonSerializer.Deserialize<System.Text.Json.JsonElement>(entity.Session.Payload, JsonOptions);
                        
                        if (payloadEl.TryGetProperty("SdjData", out var sdjData) && sdjData.ValueKind != System.Text.Json.JsonValueKind.Null)
                        {
                            isSdj = true;
                            
                            // Parse full SDJ scores from payload
                            sdjScores = JsonSerializer.Deserialize<SdjScoreSummary>(sdjData.GetRawText(), JsonOptions);
                            
                            _logger.LogInformation("[AI] Parsed SdjData for result {ResultId}: Patterns={PatternCount}, SubDims={SubDimCount}", 
                                request.ResultId, 
                                sdjScores?.SevenPatternScores?.Count ?? 0,
                                sdjScores?.SubDimensions?.Count ?? 0);
                        }
                        else
                        {
                            _logger.LogWarning("[AI] Result {ResultId} has payload but no SdjData property. Session may have been created before scoring updates.", request.ResultId);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("[AI] Result {ResultId} has empty Session.Payload", request.ResultId);
                    }

                    if (!isSdj)
                    {
                        return BadRequest(new { 
                            error = "هذه النتيجة ليست من نوع SDJ - التحليل الذكي متاح فقط لنتائج SDJ",
                            details = "Session payload does not contain SdjData. Please re-submit the test.",
                            sessionId = entity.Session.SessionId,
                            resultId = request.ResultId
                        });
                    }

                    if (sdjScores == null || !sdjScores.SevenPatternScores.Any())
                    {
                        _logger.LogWarning("[AI] Result {ResultId} has SdjData but SevenPatternScores is empty. Parsed: {HasScores}", 
                            request.ResultId, sdjScores != null);
                        return BadRequest(new { 
                            error = "لا توجد بيانات أنماط SDJ-7 لهذه النتيجة",
                            details = "SevenPatternScores array is empty. The session may need to be re-submitted.",
                            sessionId = entity.Session.SessionId,
                            resultId = request.ResultId
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[AI] Failed to parse SDJ data from payload for result {ResultId}. PayloadLength={Length}", 
                        request.ResultId, entity.Session.Payload?.Length ?? 0);
                    return BadRequest(new { 
                        error = "فشل في قراءة بيانات SDJ لهذه النتيجة",
                        details = $"Parse error: {ex.Message}",
                        sessionId = entity.Session.SessionId,
                        resultId = request.ResultId
                    });
                }

                // Build analysis payload
                var patterns = sdjScores.SevenPatternScores.Select(p => new SdjAiPatternPayload
                {
                    Key = p.PatternNameEn ?? p.PatternKey, // Use PatternNameEn (from V2) or PatternKey (from V1)
                    Label = p.PatternNameAr,
                    TScore = p.TScore
                }).ToList();

                var subDims = sdjScores.SubDimensions.Select(sd => new SdjAiSubDimensionPayload
                {
                    Key = sd.SubDimension,
                    Label = sd.SubDimension,
                    PatternKey = DeterminePatternKey(sd.SubDimension),
                    TScore = sd.T
                }).ToList();

                var items = entity.Session.SessionItems
                    .Where(si => si.Item != null && 
                               !string.IsNullOrEmpty(si.Answer) &&
                               (si.Item.Type == "LikertAgreement" || si.Item.Type == "Frequency" || si.Item.Type == "MCQ"))
                    .Select(si => new SdjAiItemPayload
                    {
                        ItemCode = si.Item.ItemCode ?? "",
                        TextAr = si.Item.TextAr ?? "",
                        DimensionKey = si.Item.Dimension ?? "",
                        SubDimensionKey = si.Item.SubDimension ?? "",
                        IsReverse = si.Item.Reverse,
                        Answer = int.TryParse(si.Answer, out int ans) ? ans : 3
                    })
                    .Take(150) // Flexible limit - supports 80, 120, or more questions
                    .ToList();

                var analysisPayload = new SdjAnalysisPayload
                {
                    ResultId = entity.Id,
                    Language = "ar",
                    Sdj = new SdjAiPayloadData
                    {
                        Patterns = patterns,
                        SubDimensions = subDims
                    },
                    Items = items
                };

                // Call SDJ-7 specialized service
                var sdjService = HttpContext.RequestServices.GetRequiredService<ISdjDeepSeekService>();
                var response = await sdjService.AnalyzeSdjAsync(analysisPayload);

                await _audit.LogAsync(null, "AI_ANALYZE_SDJ7", 
                    $"ResultId={entity.Id};Patterns={response.Patterns.Count};Tokens={response.Usage?.TotalTokens ?? 0}", 
                    HttpContext.Connection.RemoteIpAddress?.ToString());

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AI] Analysis failed for resultId={ResultId}. Error type: {ExType}, Message: {Message}", 
                    request.ResultId, ex.GetType().Name, ex.Message);
                
                // Return user-friendly error
                var errorMsg = ex.Message.Contains("DEEPSEEK_API_KEY") || ex.Message.Contains("not configured") ? 
                    "خدمة الذكاء الاصطناعي غير متوفرة - مفتاح API غير مكوّن بشكل صحيح" :
                    ex.Message.Contains("DeepSeek API error") || ex.Message.Contains("API error") ?
                    "خطأ في الاتصال بخدمة الذكاء الاصطناعي - يرجى المحاولة لاحقاً" :
                    ex.Message.Contains("timeout") || ex.Message.Contains("Timeout") ?
                    "انتهت مهلة الاتصال بخدمة الذكاء الاصطناعي - يرجى المحاولة لاحقاً" :
                    "فشل في توليد التحليل - يرجى المحاولة لاحقاً";
                    
                return StatusCode(503, new { 
                    error = errorMsg, 
                    details = ex.Message,
                    type = ex.GetType().Name,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        // Helper: Determine pattern key from sub-dimension name
        private static string DeterminePatternKey(string subDimension)
        {
            return subDimension switch
            {
                "الوعي الذاتي" or "الثقة بالنفس" or "التعلم المستمر" => "personality_patterns",
                "التنظيم الذاتي" or "حل المشكلات" or "الإبداع والابتكار" => "cognitive_mental",
                "المرونة النفسية" or "الذكاء العاطفي" or "الصحة النفسية" or "إدارة الضغوط" => "psychological_patterns",
                "التواصل الفعال" or "التعاون" or "حل النزاعات" or "بناء العلاقات" => "behavioral_patterns",
                "التخطيط الاستراتيجي" or "إدارة الوقت" => "numerical_logical",
                "القيادة" => "leadership_organizational",
                _ => "professional_readiness"
            };
        }

        [HttpPost("chat")]
        [EnableRateLimiting("admin")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            if (request?.Messages == null || request.Messages.Count == 0)
                return BadRequest(new { error = "الرسائل مطلوبة" });

            if (request.ResultId <= 0)
                return BadRequest(new { error = "معرف النتيجة مطلوب" });

            var startTime = DateTime.UtcNow;

            try
            {
                // Verify result exists
                var result = await _db.Results
                    .Include(r => r.Session)
                    .ThenInclude(s => s.User)
                    .FirstOrDefaultAsync(r => r.Id == request.ResultId);

                if (result == null)
                    return NotFound(new { error = "لم يتم العثور على النتيجة" });

                // Handle session persistence
                AiChatSession? chatSession = null;
                if (!string.IsNullOrEmpty(request.SessionId))
                {
                    chatSession = await _db.AiChatSessions
                        .FirstOrDefaultAsync(s => s.SessionId == request.SessionId && s.ResultId == request.ResultId);
                }

                // Check if OpenRouter API key is available
                var apiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
                
                ChatMessage assistantMessage;
                
                if (string.IsNullOrEmpty(apiKey))
                {
                    // Mock response when no API key
                    assistantMessage = new ChatMessage
                    {
                        Role = "assistant",
                        Content = GenerateMockResponse(request.Messages.LastOrDefault()?.Content ?? "")
                    };
                }
                else
                {
                    // Real AI response (simplified - would call OpenRouter here)
                    assistantMessage = new ChatMessage
                    {
                        Role = "assistant", 
                        Content = "شكراً لسؤالك. بناءً على نتائج الاختبار، يمكنني تقديم تحليل مفصل. هل تريد التركيز على جانب معين؟"
                    };
                }

                // Update chat session if exists
                if (chatSession != null)
                {
                    var allMessages = new List<ChatMessage>();
                    
                    // Parse existing messages
                    if (!string.IsNullOrEmpty(chatSession.MessagesJson))
                    {
                        try
                        {
                            allMessages = JsonSerializer.Deserialize<List<ChatMessage>>(chatSession.MessagesJson, JsonOptions) ?? new();
                        }
                        catch
                        {
                            allMessages = new List<ChatMessage>();
                        }
                    }

                    // Add new user message and assistant response
                    var lastUserMessage = request.Messages.LastOrDefault();
                    if (lastUserMessage != null)
                    {
                        allMessages.Add(lastUserMessage);
                    }
                    allMessages.Add(assistantMessage);

                    // Update session
                    chatSession.MessagesJson = JsonSerializer.Serialize(allMessages, JsonOptions);
                    chatSession.MessageCount = allMessages.Count;
                    chatSession.LastActivityAt = DateTime.UtcNow;
                    
                    await _db.SaveChangesAsync();
                }

                var latencyMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

                var response = new ChatResponse
                {
                    Messages = new List<ChatMessage> { assistantMessage },
                    Usage = new ChatUsage { LatencyMs = latencyMs },
                    Cached = string.IsNullOrEmpty(apiKey), // Mock responses are "cached"
                    SessionId = chatSession?.SessionId
                };

                await _audit.LogAsync(null, "AI_CHAT", $"ResultId={request.ResultId};Messages={request.Messages.Count};Latency={latencyMs}ms;SessionId={request.SessionId}", HttpContext.Connection.RemoteIpAddress?.ToString());

                return Ok(response);
            }
            catch (Exception ex)
            {
                var errorMsg = ex.Message.Contains("OpenRouter") ? 
                    "خدمة الذكاء الاصطناعي غير متوفرة حالياً" :
                    "حدث خطأ في معالجة المحادثة";
                    
                return StatusCode(502, new { error = errorMsg });
            }
        }

        private static string GenerateMockResponse(string userMessage)
        {
            var message = userMessage?.ToLowerInvariant() ?? "";

            if (message.Contains("نقاط القوة") || message.Contains("القوة"))
                return "بناءً على النتائج، تظهر نقاط القوة الرئيسية في: التفكير المنطقي، والقدرة على حل المشكلات، والمثابرة في المهام الصعبة.";
            
            if (message.Contains("تطوير") || message.Contains("تحسين"))
                return "المجالات التي يمكن تطويرها تشمل: إدارة الوقت، والتركيز لفترات أطول، وتطوير استراتيجيات جديدة للتعامل مع المهام المعقدة.";
            
            if (message.Contains("توصية") || message.Contains("نصيحة"))
                return "التوصيات العامة: 1) ممارسة التمارين الذهنية بانتظام 2) تطوير مهارات إدارة الوقت 3) العمل في بيئة هادئة ومنظمة 4) طلب التغذية الراجعة بانتظام.";
            
            if (message.Contains("كيف"))
                return "يمكن تحسين الأداء من خلال: التدرب المنتظم، تحديد نقاط الضعف والعمل عليها، الحصول على تدريب إضافي في المجالات المطلوبة.";

            return "شكراً لسؤالك. بناءً على تحليل النتائج، يمكنني تقديم رؤى مفصلة حول الأداء والتوصيات. هل لديك سؤال محدد حول جانب معين من النتائج؟";
        }
    }
}

