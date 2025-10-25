using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PsyApi.Data;
using PsyApi.Models;

namespace PsyApi.Services.Testing
{
    public class SessionPersistenceTester
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SessionPersistenceTester> _logger;

        public SessionPersistenceTester(AppDbContext context, ILogger<SessionPersistenceTester> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Dictionary<string, bool>> ValidateSessionPersistence(int sessionId)
        {
            var results = new Dictionary<string, bool>();
            
            try
            {
                var session = await _context.Sessions
                    .Include(s => s.SessionItems)
                    .ThenInclude(si => si.Item)
                    .FirstOrDefaultAsync(s => s.Id == sessionId);

                if (session == null)
                {
                    results["session_exists"] = false;
                    return results;
                }

                results["session_exists"] = true;
                results["has_start_time"] = session.StartedAt != default;
                results["has_valid_status"] = !string.IsNullOrEmpty(session.Status);
                
                if (!string.IsNullOrEmpty(session.Payload))
                {
                    try
                    {
                        var payload = JsonSerializer.Deserialize<JsonDocument>(session.Payload);
                        results["valid_payload_json"] = true;
                    }
                    catch
                    {
                        results["valid_payload_json"] = false;
                    }
                }

                // Check session items
                if (session.SessionItems != null)
                {
                    results["has_session_items"] = session.SessionItems.Any();
                    
                    var answeredItems = session.SessionItems.Where(si => !string.IsNullOrEmpty(si.Answer)).ToList();
                    results["has_answered_items"] = answeredItems.Any();
                    
                    // Verify each answered item
                    foreach (var item in answeredItems)
                    {
                        results[$"item_{item.Id}_has_answer"] = true;
                        results[$"item_{item.Id}_has_score"] = item.Score.HasValue;
                        results[$"item_{item.Id}_has_answer_time"] = item.AnsweredAt.HasValue;
                    }
                }
                else
                {
                    results["has_session_items"] = false;
                }

                _logger.LogInformation("Session persistence validation complete for session {SessionId}. " + 
                    "Results: {Results}", sessionId, 
                    string.Join(", ", results.Select(r => $"{r.Key}={r.Value}")));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating session persistence for session {SessionId}", sessionId);
                results["error"] = true;
            }

            return results;
        }
    }
}