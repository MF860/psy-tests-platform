using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PsyApi.Data;
using PsyApi.Models;
using PsyApi.Services.Scoring;
using PsyApi.Services.Reports;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace PsyApi.Controllers
{
    [ApiController]
    [Route("api/results")]
    [Authorize]
    public class ResultsController : ControllerBase
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private readonly AppDbContext _context;
        private readonly ILogger<ResultsController> _logger;

        public ResultsController(AppDbContext context, ILogger<ResultsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetResults()
        {
            try
            {
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

        [HttpGet("{id}/birkman")]
        public async Task<IActionResult> GetBirkmanReport(int id)
        {
            try
            {
                var result = await _context.Results
                    .Include(r => r.Session)
                    .ThenInclude(s => s.User)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (result == null)
                {
                    return NotFound(new { error = "Result not found" });
                }

                var dimensionScores = ParseDimensionScores(result.DimensionScoresJson);

                // Check if SDJ data is present
                var sdjData = TryParseSdjData(result.DimensionScoresJson);

                var sessionItems = await _context.SessionItems
                    .Include(si => si.Item)
                    .Where(si => si.SessionId == result.SessionId)
                    .AsNoTracking()
                    .ToListAsync();

                var birkmanAxes = BirkmanMapper.MapToBirkmanAxes(dimensionScores);
                var strengths = dimensionScores.Where(d => d.T > 60).Select(d => d.Dimension).ToList();
                var risks = dimensionScores.Where(d => d.T < 40).Select(d => d.Dimension).ToList();
                var recommendations = BirkmanMapper.GenerateRecommendations(birkmanAxes, strengths, risks);
                var courses = BirkmanMapper.GenerateCourses(birkmanAxes);

                var byType = sessionItems
                    .Where(si => si.Item != null)
                    .GroupBy(si => si.Item.Type)
                    .Select(g =>
                    {
                        var total = g.Count();
                        var correct = g.Count(si => si.RawCorrect == true);
                        var responseTimes = g.Where(si => si.ResponseTimeMs.HasValue)
                            .Select(si => si.ResponseTimeMs!.Value)
                            .ToList();
                        var avgTime = responseTimes.Any() ? responseTimes.Average() : (double?)null;

                        return new
                        {
                            Type = g.Key,
                            Correct = correct,
                            Total = total,
                            Accuracy = total > 0 ? (double)correct / total : 0,
                            AvgTimeMs = avgTime
                        };
                    })
                    .ToList();

                var orderedResponseTimes = sessionItems
                    .Where(si => si.ResponseTimeMs.HasValue)
                    .Select(si => si.ResponseTimeMs!.Value)
                    .OrderBy(x => x)
                    .ToList();

                double? averageResponseMs = orderedResponseTimes.Any()
                    ? orderedResponseTimes.Average()
                    : null;

                int? p90Ms = null;
                if (orderedResponseTimes.Any())
                {
                    var index = (int)Math.Min(orderedResponseTimes.Count - 1, Math.Floor(orderedResponseTimes.Count * 0.9));
                    p90Ms = orderedResponseTimes[index];
                }

                var timing = new
                {
                    AvgResponseTimeMs = averageResponseMs,
                    P90Ms = p90Ms
                };

                return Ok(new
                {
                    Session = new
                    {
                        Id = result.SessionId,
                        Status = result.Session.Status,
                        StartedAt = result.Session.StartedAt,
                        EndedAt = result.Session.EndedAt,
                        DurationSec = result.Session.EndedAt.HasValue
                            ? (int)(result.Session.EndedAt.Value - result.Session.StartedAt).TotalSeconds
                            : 0,
                        AnsweredCount = sessionItems.Count
                    },
                    User = new
                    {
                        NationalId = result.Session.User.NationalId,
                        FullName = result.Session.User.FullName
                    },
                    Totals = new
                    {
                        Score = result.TotalScore,
                        TScore = dimensionScores.Any() ? dimensionScores.Average(d => d.T) : 0,
                        Percentile = dimensionScores.Any() ? dimensionScores.Average(d => d.Percentile) : 0
                    },
                    Dimensions = dimensionScores,
                    ByType = byType,
                    Timing = timing,
                    BirkmanAxes = birkmanAxes,
                    Recommendations = recommendations,
                    Courses = courses,
                    // Include SDJ data if available
                    SdjData = sdjData != null ? new
                    {
                        Dimensions = sdjData.Dimensions,
                        SubDimensions = sdjData.SubDimensions,
                        TrackFits = sdjData.TrackFits
                    } : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Birkman report for result {Id}", id);
                return StatusCode(500, new { error = "An error occurred while retrieving the Birkman report" });
            }
        }

        [HttpGet("{id}/pdf")]
        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> GetPdfReport(int id)
        {
            try
            {
                var result = await _context.Results
                    .Include(r => r.Session)
                    .ThenInclude(s => s.User)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (result == null)
                {
                    return NotFound(new { error = "Result not found" });
                }

                // SECURITY: Per-user authorization check
                // Only the result owner or admins can access the PDF
                var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var isAdmin = User.IsInRole("Admin") || User.IsInRole("admin");
                
                if (!isAdmin && result.Session.User.Id.ToString() != currentUserId)
                {
                    _logger.LogWarning("Unauthorized PDF access attempt: User {UserId} tried to access result {ResultId} owned by {OwnerId}", 
                        currentUserId, id, result.Session.User.Id);
                    return Forbid();
                }

                var etag = BuildEtag(result);
                if (Request.Headers.IfNoneMatch.Contains(etag))
                {
                    return StatusCode(304);
                }

                // Detect SDJ data
                var hasSdjData = !string.IsNullOrWhiteSpace(result.DimensionScoresJson) && 
                                 result.DimensionScoresJson.Contains("\"SubDimensions\"");

                var pdfService = HttpContext.RequestServices.GetRequiredService<IPdfReportService>();
                byte[] pdfBytes;

                if (hasSdjData)
                {
                    _logger.LogInformation("Generating SDJ PDF report for result {Id}", id);
                    pdfBytes = await pdfService.RenderSdjResultPdfAsync(result, result.Session.User);
                }
                else
                {
                    _logger.LogInformation("Generating legacy PDF report for result {Id}", id);
                    var dimensionScores = ParseDimensionScores(result.DimensionScoresJson);
                    pdfBytes = await pdfService.RenderResultPdfAsync(result, result.Session.User, dimensionScores);
                }

                Response.Headers.ETag = etag;
                return File(pdfBytes, "application/pdf", $"PsyReport_{result.Session.User.NationalId}_{result.SessionId}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF report for result {Id}", id);
                return StatusCode(500, new { error = "An error occurred while generating the PDF report" });
            }
        }

        private static List<DimensionScore> ParseDimensionScores(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<DimensionScore>();
            }

            try
            {
                // Try SDJ format first
                var sdjData = TryParseSdjData(json);
                if (sdjData != null && sdjData.Dimensions != null)
                {
                    // Convert SDJ dimensions to DimensionScore format
                    return sdjData.Dimensions.Select(d => new DimensionScore
                    {
                        Dimension = d.Dimension,
                        Raw = d.Raw,
                        T = d.T,
                        Percentile = d.Percentile,
                        Z = (d.T - 50) / 10
                    }).ToList();
                }

                // Fall back to legacy format
                return JsonSerializer.Deserialize<List<DimensionScore>>(json, JsonOptions) ?? new List<DimensionScore>();
            }
            catch (JsonException)
            {
                return new List<DimensionScore>();
            }
        }

        private static SdjDataWrapper? TryParseSdjData(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                var data = JsonSerializer.Deserialize<SdjDataWrapper>(json, JsonOptions);
                // Check if it has SDJ structure
                if (data?.Dimensions != null && data.Dimensions.Any())
                {
                    return data;
                }
                return null;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private class SdjDataWrapper
        {
            public List<SdjDimension>? Dimensions { get; set; }
            public List<SdjSubDimension>? SubDimensions { get; set; }
            public List<SdjTrack>? TrackFits { get; set; }
        }

        private class SdjDimension
        {
            public string Dimension { get; set; } = string.Empty;
            public double Raw { get; set; }
            public double T { get; set; }
            public double Percentile { get; set; }
            public string Band { get; set; } = string.Empty;
        }

        private class SdjSubDimension
        {
            public string Dimension { get; set; } = string.Empty;
            public string SubDimension { get; set; } = string.Empty;
            public double T { get; set; }
            public string Band { get; set; } = string.Empty;
        }

        private class SdjTrack
        {
            public string TrackNameAr { get; set; } = string.Empty;
            public string FitLevel { get; set; } = string.Empty;
            public double FitScore { get; set; }
        }

        private static string BuildEtag(Result result)
        {
            var payload = $"{result.Id}:{result.CreatedAt.Ticks}:{result.DimensionScoresJson ?? string.Empty}";
            var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
            return $"W/\"{hash}\"";
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
        
        // SDJ fields (nullable for backward compatibility)
        public string? SdjProfile { get; set; }
        public string? TopStrength { get; set; }
        public string? TopDevelopmentArea { get; set; }
    }
}



