using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PsyApi.Data;
using PsyApi.Models;
using PsyApi.Services.Scoring;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;

namespace PsyApi.Controllers
{
    [ApiController]
    [Route("api/sessions")]
    public class SessionsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SessionsController> _logger;
        private readonly IScoringService _scoringService;

        public SessionsController(AppDbContext context, ILogger<SessionsController> logger, IScoringService scoringService)
        {
            _context = context;
            _logger = logger;
            _scoringService = scoringService;
        }

        [HttpPost("start")]
        public async Task<IActionResult> StartSession([FromBody] StartSessionRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.NationalId))
                {
                    return BadRequest(new { error = "National ID is required" });
                }

                // Find or create user
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.NationalId == request.NationalId);

                if (user == null)
                {
                    user = new User
                    {
                        NationalId = request.NationalId,
                        FullName = "", // Will be filled later
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                }

                // Create a new session
                var session = new Session
                {
                    UserId = user.Id,
                    StartedAt = DateTime.UtcNow,
                    Status = "in_progress"
                };
                _context.Sessions.Add(session);
                await _context.SaveChangesAsync();

                // Randomly select 80 items
                var items = await _context.Items
                    .OrderBy(i => Guid.NewGuid())
                    .Take(80)
                    .ToListAsync();

                // Insert them into SessionItems
                foreach (var item in items)
                {
                    var sessionItem = new SessionItem
                    {
                        SessionId = session.Id,
                        ItemId = item.Id,
                        Answer = null,
                        Score = null,
                        AnsweredAt = null
                    };
                    _context.SessionItems.Add(sessionItem);
                }
                await _context.SaveChangesAsync();

                return Ok(new { sessionId = session.Id, totalQuestions = 80 });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting session");
                return StatusCode(500, new { error = "An error occurred while starting the session" });
            }
        }

        [HttpGet("{id}/next")]
        public async Task<IActionResult> GetNextQuestion(int id)
        {
            try
            {
                // Check if session exists and is in progress
                var session = await _context.Sessions
                    .FirstOrDefaultAsync(s => s.Id == id && s.Status == "in_progress");

                if (session == null)
                {
                    return NotFound(new { error = "Session not found or not in progress" });
                }

                // Get the next unanswered item
                var nextItem = await _context.SessionItems
                    .Include(si => si.Item)
                    .Where(si => si.SessionId == id && si.Answer == null)
                    .OrderBy(si => si.Id)
                    .FirstOrDefaultAsync();

                if (nextItem == null)
                {
                    return Ok(new { message = "completed" });
                }

                // Prepare the response based on item type
                var response = new
                {
                    id = nextItem.Item.Id,
                    text_ar = nextItem.Item.TextAr,
                    type = nextItem.Item.Type,
                    dimension_tags = nextItem.Item.DimensionTags,
                    difficulty = nextItem.Item.Difficulty,
                    time_limit_seconds = nextItem.Item.TimeLimitSeconds,
                    max_score = nextItem.Item.MaxScore,
                    // For MCQ type, we would parse the correct_answer to get options
                    // This is a simplified version
                    options = nextItem.Item.Type == "MCQ" ? ParseOptions(nextItem.Item.CorrectAnswer) : null
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting next question for session {SessionId}", id);
                return StatusCode(500, new { error = "An error occurred while getting the next question" });
            }
        }

        [HttpPost("{id}/answer")]
        public async Task<IActionResult> SubmitAnswer(int id, [FromBody] SubmitAnswerRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.ItemId) || string.IsNullOrWhiteSpace(request.Answer))
                {
                    return BadRequest(new { error = "Item ID and answer are required" });
                }

                // Check if session exists and is in progress
                var session = await _context.Sessions
                    .FirstOrDefaultAsync(s => s.Id == id && s.Status == "in_progress");

                if (session == null)
                {
                    return NotFound(new { error = "Session not found or not in progress" });
                }

                // Parse the item_id to get the integer value
                if (!int.TryParse(request.ItemId.Replace("I", ""), out var itemIdInt))
                {
                    return BadRequest(new { error = "Invalid Item ID format" });
                }

                // Find the session item
                var sessionItem = await _context.SessionItems
                    .Include(si => si.Item)
                    .FirstOrDefaultAsync(si => si.SessionId == id && si.ItemId == itemIdInt);

                if (sessionItem == null)
                {
                    return NotFound(new { error = "Question not found in this session" });
                }

                // Update the answer
                sessionItem.Answer = request.Answer;
                sessionItem.AnsweredAt = DateTime.UtcNow;
                if (request.ResponseTimeMs.HasValue)
                {
                    sessionItem.ResponseTimeMs = request.ResponseTimeMs;
                }
                _logger.LogDebug("Session {SessionId}, Item {ItemId}, ResponseTimeMs: {ResponseTimeMs}ms", id, request.ItemId, request.ResponseTimeMs);

                // Calculate score if possible
                if (sessionItem.Item.Type == "MCQ" && sessionItem.Item.CorrectAnswer == request.Answer)
                {
                    sessionItem.Score = sessionItem.Item.MaxScore;
                }
                else if (sessionItem.Item.Type == "TIMED_NUMERIC")
                {
                    // Try to parse both the correct answer and the user's answer as numbers
                    if (decimal.TryParse(sessionItem.Item.CorrectAnswer, out var correctValue) &&
                        decimal.TryParse(request.Answer, out var userValue))
                    {
                        // Calculate score based on how close the answer is
                        var difference = Math.Abs(correctValue - userValue);
                        var maxDifference = correctValue * 0.2m; // 20% tolerance
                        sessionItem.Score = difference <= maxDifference ? sessionItem.Item.MaxScore : 0;
                    }
                }
                // Other question types would need different scoring logic

                await _context.SaveChangesAsync();

                return Ok(new { message = "Answer submitted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting answer for session {SessionId}", id);
                return StatusCode(500, new { error = "An error occurred while submitting the answer" });
            }
        }

        [HttpPost("{id}/submit")]
        public async Task<IActionResult> SubmitSession(int id)
        {
            try
            {
                // Check if session exists and is in progress
                var session = await _context.Sessions
                    .FirstOrDefaultAsync(s => s.Id == id && s.Status == "in_progress");

                if (session == null)
                {
                    return NotFound(new { error = "Session not found or not in progress" });
                }

                // Mark session as completed
                session.Status = "completed";
                session.EndedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Compute scores via scoring service
                var serviceSummary = await _scoringService.ComputeSessionScores(session.Id);

                // Map to Models DTO for consistent API/storage
                var modelSummary = new PsyApi.Models.ScoreSummary
                {
                    Dimensions = serviceSummary.DimensionScores?.Select(ds => new PsyApi.Models.DimensionScore
                    {
                        Dimension = ds.Dimension,
                        Raw = ds.Raw,
                        Z = ds.Z,
                        T = ds.T,
                        Percentile = ds.Percentile
                    }).ToList() ?? new List<PsyApi.Models.DimensionScore>(),
                    TotalScore = serviceSummary.TotalScore,
                    Version = serviceSummary.Version
                };

                // Upsert result
                var result = await _context.Results.FirstOrDefaultAsync(r => r.SessionId == session.Id);
                if (result == null)
                {
                    result = new Result
                    {
                        SessionId = session.Id,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Results.Add(result);
                }

                result.TotalScore = modelSummary.TotalScore.HasValue ? (int)System.Math.Round(modelSummary.TotalScore.Value) : 0;
                result.ScoringModelVersion = modelSummary.Version;

                var jsonOptions = new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    WriteIndented = false
                };

                result.DimensionScoresJson = JsonSerializer.Serialize(modelSummary.Dimensions, jsonOptions);
                result.CompositeScoresJson = JsonSerializer.Serialize(new { TotalScore = modelSummary.TotalScore }, jsonOptions);

                await _context.SaveChangesAsync();

                return Ok(new { message = "submitted", sessionId = session.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting session {SessionId}", id);
                return StatusCode(500, new { error = "An error occurred while submitting the session" });
            }
        }

        // Helper method to parse options from correct_answer for MCQ questions
        private List<string> ParseOptions(string correctAnswer)
        {
            // This is a simplified version
            // In a real implementation, you would have a more sophisticated way to store and retrieve options
            return new List<string> { "Option 1", "Option 2", "Option 3", "Option 4" };
        }
    }

    public class StartSessionRequest
    {
        [Required]
        public string NationalId { get; set; } = string.Empty;
    }

    public class SubmitAnswerRequest
    {
        [Required]
        public string ItemId { get; set; } = string.Empty;

        [Required]
        public string Answer { get; set; } = string.Empty;

        public int? ResponseTimeMs { get; set; }
    }
}
