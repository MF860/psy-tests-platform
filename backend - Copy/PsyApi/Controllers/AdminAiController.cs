using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using PsyApi.Data;
using PsyApi.Models;
using PsyApi.Services.AI;
using PsyApi.Services.Audit;
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

        public AdminAiController(AppDbContext db, IAiAnalyzerService ai, IAuditService audit, ILogger<AdminAiController> logger)
        {
            _db = db; _ai = ai; _audit = audit; _logger = logger;
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
                    .FirstOrDefaultAsync(r => r.Id == request.ResultId);

                if (entity == null)
                    return NotFound(new { error = "لم يتم العثور على النتيجة" });

                // Parse SDJ dimensions from DimensionScoresJson
                var dims = new List<PsyApi.Models.DimensionScore>();
                
                // First check if this is an SDJ result by checking the Payload
                try
                {
                    if (!string.IsNullOrWhiteSpace(entity.Session.Payload))
                    {
                        var payload = JsonSerializer.Deserialize<System.Text.Json.JsonElement>(entity.Session.Payload, JsonOptions);
                        
                        // Check if it's SDJ mode
                        bool isSdj = false;
                        if (payload.TryGetProperty("SdjData", out var sdjData) && sdjData.ValueKind != System.Text.Json.JsonValueKind.Null)
                        {
                            isSdj = true;
                            
                            // Try to parse dimensions from sdjData
                            if (sdjData.TryGetProperty("Dimensions", out var dimensions) && dimensions.ValueKind == System.Text.Json.JsonValueKind.Array)
                            {
                                foreach (var dim in dimensions.EnumerateArray())
                                {
                                    if (dim.TryGetProperty("Dimension", out var dimName) &&
                                        dim.TryGetProperty("Raw", out var raw) &&
                                        dim.TryGetProperty("T", out var t))
                                    {
                                        dims.Add(new PsyApi.Models.DimensionScore
                                        {
                                            Dimension = dimName.GetString() ?? "",
                                            Raw = raw.GetDouble(),
                                            T = t.GetDouble()
                                        });
                                    }
                                }
                            }
                        }
                        
                        if (!isSdj)
                        {
                            return BadRequest(new { error = "هذه النتيجة ليست من نوع SDJ - التحليل الذكي متاح فقط لنتائج SDJ" });
                        }
                    }
                    else
                    {
                        return BadRequest(new { error = "لا توجد بيانات جلسة لهذه النتيجة" });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[AI] Failed to parse SDJ data from payload for result {ResultId}", request.ResultId);
                    return BadRequest(new { error = "فشل في قراءة بيانات SDJ لهذه النتيجة" });
                }

                // Validate we have dimension data
                if (!dims.Any())
                {
                    return BadRequest(new { error = "لا توجد بيانات أبعاد SDJ لهذه النتيجة" });
                }

                var response = await _ai.AnalyzeAsync(entity, dims);

                await _audit.LogAsync(null, "AI_ANALYZE", $"ResultId={entity.Id};SessionId={entity.SessionId};Model={response.Model};Tokens={response.Usage?.TotalTokens ?? 0}", HttpContext.Connection.RemoteIpAddress?.ToString());

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AI] Analysis failed for resultId={ResultId}", request.ResultId);
                
                // Return user-friendly error
                var errorMsg = ex.Message.Contains("DEEPSEEK_API_KEY") || ex.Message.Contains("API") ? 
                    "خدمة الذكاء الاصطناعي غير متوفرة حالياً - جاري استخدام التحليل الاحتياطي" :
                    "حدث خطأ في توليد التحليل";
                    
                return StatusCode(503, new { error = errorMsg, details = ex.Message });
            }
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

