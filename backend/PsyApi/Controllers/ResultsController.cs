using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PsyApi.Data;
using PsyApi.Models;

namespace PsyApi.Controllers
{
    [ApiController]
    [Route("api/results")]
    public class ResultsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ResultsController> _logger;

        public ResultsController(AppDbContext context, ILogger<ResultsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetResults([FromQuery] string adminKey)
        {
            try
            {
                // Validate admin key
                if (string.IsNullOrWhiteSpace(adminKey) || adminKey != "StrongAdmin!23")
                {
                    return Unauthorized(new { error = "Invalid or missing admin key" });
                }

                // Query the Results table and join with Sessions and Users
                var results = await _context.Results
                    .Include(r => r.Session)
                    .ThenInclude(s => s.User)
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new ResultDto
                    {
                        ResultId = r.Id,
                        SessionId = r.SessionId,
                        TotalScore = r.TotalScore,
                        CreatedAt = r.CreatedAt,
                        NationalId = r.Session.User.NationalId,
                        FullName = r.Session.User.FullName ?? "N/A"
                    })
                    .ToListAsync();

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving results");
                return StatusCode(500, new { error = "An error occurred while retrieving results" });
            }
        }
    }

    public class ResultDto
    {
        public int ResultId { get; set; }
        public int SessionId { get; set; }
        public int TotalScore { get; set; }
        public DateTime CreatedAt { get; set; }
        public string NationalId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }
}
