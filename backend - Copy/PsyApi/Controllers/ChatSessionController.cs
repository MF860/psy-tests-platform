using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PsyApi.Data;
using PsyApi.Models;
using PsyApi.Services.Audit;
using System.Security.Claims;
using System.Text.Json;

namespace PsyApi.Controllers
{
    public class CreateSessionRequest
    {
        public int? ResultId { get; set; }
        public string? Title { get; set; }
        public List<ChatMessage>? InitialMessages { get; set; }
    }

    public class AddMessageRequest
    {
        public ChatMessage Message { get; set; } = new();
    }

    public class UpdateSessionRequest
    {
        public string? Title { get; set; }
        public string? Status { get; set; }
    }

    public class ChatSessionResponse
    {
        public int Id { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public int? UserId { get; set; }
        public int? ResultId { get; set; }
        public List<ChatMessage> Messages { get; set; } = new();
        public string? Title { get; set; }
        public string Status { get; set; } = string.Empty;
        public int MessageCount { get; set; }
        public DateTime LastActivityAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    [ApiController]
    [Authorize]
    [Route("api/chat-sessions")]
    public class ChatSessionController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IAuditService _audit;

        public ChatSessionController(AppDbContext db, IAuditService audit)
        {
            _db = db;
            _audit = audit;
        }

        [HttpPost]
        public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequest request)
        {
            try
            {
                var userId = GetUserId();
                var sessionId = Guid.NewGuid().ToString();
                
                var session = new AiChatSession
                {
                    SessionId = sessionId,
                    UserId = userId,
                    ResultId = request?.ResultId,
                    Title = request?.Title ?? "New Chat Session",
                    Status = ChatSessionStatus.Active,
                    MessagesJson = JsonSerializer.Serialize(request?.InitialMessages ?? new List<ChatMessage>()),
                    MessageCount = request?.InitialMessages?.Count ?? 0,
                    LastActivityAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                _db.AiChatSessions.Add(session);
                await _db.SaveChangesAsync();

                await _audit.LogAsync(userId, "CHAT_SESSION_CREATE", $"SessionId={sessionId};ResultId={request?.ResultId}", GetClientIp());

                return Ok(MapToResponse(session));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "حدث خطأ في إنشاء الجلسة" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSessions([FromQuery] int? resultId = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = GetUserId();
                var query = _db.AiChatSessions
                    .Where(s => s.UserId == userId || userId == null); // Allow admin access

                if (resultId.HasValue)
                {
                    query = query.Where(s => s.ResultId == resultId);
                }

                var totalCount = await query.CountAsync();
                var sessions = await query
                    .OrderByDescending(s => s.LastActivityAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var response = new
                {
                    Sessions = sessions.Select(MapToResponse).ToList(),
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "حدث خطأ في استرجاع الجلسات" });
            }
        }

        [HttpGet("{sessionId}")]
        public async Task<IActionResult> GetSession(string sessionId)
        {
            try
            {
                var userId = GetUserId();
                var session = await _db.AiChatSessions
                    .FirstOrDefaultAsync(s => s.SessionId == sessionId && (s.UserId == userId || userId == null));

                if (session == null)
                {
                    return NotFound(new { error = "لم يتم العثور على الجلسة" });
                }

                return Ok(MapToResponse(session));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "حدث خطأ في استرجاع الجلسة" });
            }
        }

        [HttpPost("{sessionId}/messages")]
        public async Task<IActionResult> AddMessage(string sessionId, [FromBody] AddMessageRequest request)
        {
            try
            {
                var userId = GetUserId();
                var session = await _db.AiChatSessions
                    .FirstOrDefaultAsync(s => s.SessionId == sessionId && (s.UserId == userId || userId == null));

                if (session == null)
                {
                    return NotFound(new { error = "لم يتم العثور على الجلسة" });
                }

                if (session.Status != ChatSessionStatus.Active)
                {
                    return BadRequest(new { error = "الجلسة غير نشطة" });
                }

                // Parse existing messages
                var messages = new List<ChatMessage>();
                if (!string.IsNullOrEmpty(session.MessagesJson))
                {
                    messages = JsonSerializer.Deserialize<List<ChatMessage>>(session.MessagesJson) ?? new();
                }

                // Add new message
                messages.Add(request.Message);

                // Update session
                session.MessagesJson = JsonSerializer.Serialize(messages);
                session.MessageCount = messages.Count;
                session.LastActivityAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                await _audit.LogAsync(userId, "CHAT_MESSAGE_ADD", $"SessionId={sessionId};Role={request.Message.Role}", GetClientIp());

                return Ok(new { success = true, messageCount = session.MessageCount });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "حدث خطأ في إضافة الرسالة" });
            }
        }

        [HttpPut("{sessionId}")]
        public async Task<IActionResult> UpdateSession(string sessionId, [FromBody] UpdateSessionRequest request)
        {
            try
            {
                var userId = GetUserId();
                var session = await _db.AiChatSessions
                    .FirstOrDefaultAsync(s => s.SessionId == sessionId && (s.UserId == userId || userId == null));

                if (session == null)
                {
                    return NotFound(new { error = "لم يتم العثور على الجلسة" });
                }

                bool updated = false;

                if (!string.IsNullOrEmpty(request.Title))
                {
                    session.Title = request.Title;
                    updated = true;
                }

                if (!string.IsNullOrEmpty(request.Status) && 
                    (request.Status == ChatSessionStatus.Active || request.Status == ChatSessionStatus.Archived || request.Status == ChatSessionStatus.Deleted))
                {
                    session.Status = request.Status;
                    updated = true;
                }

                if (updated)
                {
                    session.LastActivityAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync();

                    await _audit.LogAsync(userId, "CHAT_SESSION_UPDATE", $"SessionId={sessionId};Title={request.Title};Status={request.Status}", GetClientIp());
                }

                return Ok(MapToResponse(session));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "حدث خطأ في تحديث الجلسة" });
            }
        }

        [HttpDelete("{sessionId}")]
        public async Task<IActionResult> DeleteSession(string sessionId)
        {
            try
            {
                var userId = GetUserId();
                var session = await _db.AiChatSessions
                    .FirstOrDefaultAsync(s => s.SessionId == sessionId && (s.UserId == userId || userId == null));

                if (session == null)
                {
                    return NotFound(new { error = "لم يتم العثور على الجلسة" });
                }

                // Soft delete by updating status
                session.Status = ChatSessionStatus.Deleted;
                session.LastActivityAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                await _audit.LogAsync(userId, "CHAT_SESSION_DELETE", $"SessionId={sessionId}", GetClientIp());

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "حدث خطأ في حذف الجلسة" });
            }
        }

        [HttpPost("{sessionId}/archive")]
        public async Task<IActionResult> ArchiveSession(string sessionId)
        {
            return await UpdateSession(sessionId, new UpdateSessionRequest { Status = ChatSessionStatus.Archived });
        }

        [HttpPost("{sessionId}/restore")]
        public async Task<IActionResult> RestoreSession(string sessionId)
        {
            return await UpdateSession(sessionId, new UpdateSessionRequest { Status = ChatSessionStatus.Active });
        }

        private int? GetUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }
            return null; // Admin users might not have a user ID
        }

        private string? GetClientIp()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString();
        }

        private static ChatSessionResponse MapToResponse(AiChatSession session)
        {
            var messages = new List<ChatMessage>();
            if (!string.IsNullOrEmpty(session.MessagesJson))
            {
                try
                {
                    messages = JsonSerializer.Deserialize<List<ChatMessage>>(session.MessagesJson) ?? new();
                }
                catch
                {
                    // Handle JSON parsing errors gracefully
                    messages = new List<ChatMessage>();
                }
            }

            return new ChatSessionResponse
            {
                Id = session.Id,
                SessionId = session.SessionId,
                UserId = session.UserId,
                ResultId = session.ResultId,
                Messages = messages,
                Title = session.Title,
                Status = session.Status,
                MessageCount = session.MessageCount,
                LastActivityAt = session.LastActivityAt,
                CreatedAt = session.CreatedAt
            };
        }
    }
}