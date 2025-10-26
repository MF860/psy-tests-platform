using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PsyApi.Data;

namespace PsyApi.Controllers
{
    [ApiController]
    [Route("api/public")]
    public class PublicController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PublicController> _logger;
        private static readonly Dictionary<string, int> RequestCounts = new();
        private static readonly Dictionary<string, DateTime> FirstAttempts = new();
        private static readonly object LoginAttemptsLock = new();
        private const int MaxAttemptsPerMinute = 10;

        public PublicController(AppDbContext context, ILogger<PublicController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public class UserLoginRequest
        {
            public string NationalId { get; set; } = string.Empty;
        }

        [HttpPost("user-login")]
        public async Task<IActionResult> UserLogin([FromBody] UserLoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NationalId))
                return BadRequest(new { error = "National ID is required" });

            // Rate limiting check
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            lock (LoginAttemptsLock)
            {
                if (!FirstAttempts.ContainsKey(clientIp))
                {
                    FirstAttempts[clientIp] = DateTime.UtcNow;
                    RequestCounts[clientIp] = 1;
                }
                else
                {
                    var timeSinceFirst = DateTime.UtcNow - FirstAttempts[clientIp];
                    if (timeSinceFirst.TotalMinutes >= 1)
                    {
                        FirstAttempts[clientIp] = DateTime.UtcNow;
                        RequestCounts[clientIp] = 1;
                    }
                    else if (RequestCounts[clientIp] >= MaxAttemptsPerMinute)
                    {
                        return StatusCode(429, new { error = "Too many login attempts. Please wait a minute." });
                    }
                    else
                    {
                        RequestCounts[clientIp] = RequestCounts[clientIp] + 1;
                    }
                }
            }

            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.NationalId == request.NationalId);
            if (user == null)
            {
                return StatusCode(401, new { error = "INVALID_NATIONAL_ID" });
            }

            // Check for existing session
            var existingSession = await _context.Sessions
                .OrderByDescending(s => s.StartedAt)
                .FirstOrDefaultAsync(s => s.UserId == user.Id && s.Status == "in_progress");

            if (existingSession != null)
            {
                return Ok(new
                {
                    sessionId = existingSession.Id,
                    totalQuestions = 80,
                    resume = true,
                    user = new
                    {
                        user.NationalId,
                        user.FullName
                    }
                });
            }

            // Create new session
            var session = new PsyApi.Models.Session
            {
                UserId = user.Id,
                StartedAt = DateTime.UtcNow,
                Status = "in_progress"
            };
            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();

            // Select 80 random items
            var items = await _context.Items.OrderBy(i => Guid.NewGuid()).Take(80).ToListAsync();
            foreach (var item in items)
            {
                _context.SessionItems.Add(new PsyApi.Models.SessionItem
                {
                    SessionId = session.Id,
                    ItemId = item.Id
                });
            }
            await _context.SaveChangesAsync();

            var totalQuestions = 80;
            return Ok(new
            {
                sessionId = session.Id,
                totalQuestions,
                resume = false,
                user = new
                {
                    user.NationalId,
                    user.FullName
                }
            });
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users.Select(u => new { u.Id, u.NationalId, u.FullName }).ToListAsync();
            return Ok(users);
        }
    }
}


