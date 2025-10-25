using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PsyApi.Data;
using PsyApi.Models;

using System.Text.Encodings.Web;
using System.Text.Json;
using Serilog;

namespace PsyApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/admin/analytics")]
    public class AdminAnalyticsController : ControllerBase
    {

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private readonly AppDbContext _context;

        public AdminAnalyticsController(AppDbContext context)
        {
            _context = context;
        }

        private readonly Serilog.ILogger _logger = Log.ForContext<AdminAnalyticsController>();

        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview()
        {
            try
            {
                var totalUsers = await _context.Users.CountAsync();
                var totalSessions = await _context.Sessions.CountAsync();
                var totalResults = await _context.Results.CountAsync();
                var resultsToday = await _context.Results
                    .AsNoTracking()
                    .Where(r => r.CreatedAt.Date == DateTime.UtcNow.Date)
                    .CountAsync();
                var results = await _context.Results
                    .AsNoTracking()
                    .Include(r => r.Session)
                    .ThenInclude(s => s.User)
                    .Select(r => new { r.TotalScore, r.DimensionScoresJson, r.Session.User.NationalId, r.CreatedAt })
                    .ToListAsync();
                var sessionItems = await _context.SessionItems
                    .AsNoTracking()
                    .Include(si => si.Item)
                    .Select(si => new { si.Item.Type, si.RawCorrect, si.ResponseTimeMs })
                    .ToListAsync();
                var recentSessions = await _context.Sessions
                    .AsNoTracking()
                    .Include(s => s.User)
                    .Include(s => s.Result)
                    .OrderByDescending(s => s.StartedAt)
                    .Take(5)
                    .Select(s => new
                    {
                        SessionId = s.Id,
                        NationalId = s.User != null ? s.User.NationalId : string.Empty,
                        Score = s.Result != null ? s.Result.TotalScore : 0,
                        CreatedAt = s.StartedAt
                    })
                    .ToListAsync();

                // Activity for the last 30 days
                var activityLast30d = await _context.Sessions
                    .AsNoTracking()
                    .Where(s => s.StartedAt >= DateTime.UtcNow.AddDays(-30))
                    .GroupBy(s => s.StartedAt.Date)
                    .Select(g => new { Date = g.Key, Count = g.Count() })
                    .OrderBy(g => g.Date)
                    .ToListAsync();

                var avgTotalScore = results.Any() ? results.Average(r => r.TotalScore) : 0;

                var dimensionData = new List<DimensionScore>();

                // Process dimension scores
                foreach (var r in results)
                {
                    if (!string.IsNullOrEmpty(r.DimensionScoresJson))
                    {
                        try
                        {
                            var scores = JsonSerializer.Deserialize<List<DimensionScore>>(r.DimensionScoresJson);
                            if (scores != null)
                            {
                                dimensionData.AddRange(scores);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Warning(ex, "Failed to deserialize dimension scores for result {ResultId}", r.TotalScore);
                        }
                    }
                }

                // Group by dimension and calculate averages
                var dimensionAvgs = dimensionData
                    .GroupBy(d => d.Dimension)
                    .Select(g => new
                    {
                        Dimension = g.Key,
                        AvgT = g.Average(d => d.T),
                        AvgPercentile = g.Average(d => d.Percentile)
                    })
                    .OrderBy(d => d.Dimension)
                    .ToList();

                // Item type accuracy
                var typeAccuracy = sessionItems
                    .GroupBy(si => si.Type)
                    .Select(g => new
                    {
                        Type = g.Key,
                        Count = g.Count(),
                        AvgCorrect = g.Average(si => si.RawCorrect.HasValue && si.RawCorrect.Value ? 1 : 0),
                        AvgResponseTime = g.Average(si => si.ResponseTimeMs)
                    })
                    .OrderBy(t => t.Type)
                    .ToList();

                // Format activity data
                var activityData = activityLast30d
                    .Select(a => new
                    {
                        Date = a.Date.ToString("yyyy-MM-dd"),
                        Count = a.Count
                    })
                    .ToList();

                // Fill in missing dates with 0
                var startDate = DateTime.UtcNow.AddDays(-30).Date;
                var endDate = DateTime.UtcNow.Date;
                var allDates = new List<DateTime>();
                for (var date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    allDates.Add(date);
                }

                var completeActivityData = allDates
                    .Select(date => new
                    {
                        Date = date.ToString("yyyy-MM-dd"),
                        Count = activityData.FirstOrDefault(a => a.Date == date.ToString("yyyy-MM-dd"))?.Count ?? 0
                    })
                    .ToList();

                return Ok(new
                {
                    totalUsers = totalUsers,
                    totalSessions = totalSessions,
                    totalResults = totalResults,
                    resultsToday = resultsToday,
                    avgTotalScore = Math.Round(avgTotalScore, 2),
                    byDimension = dimensionAvgs.Select(d => new { dimension = d.Dimension, avg = d.AvgT, tScore = d.AvgT }).ToList(),
                    byTypeAccuracy = typeAccuracy.Select(t => new { type = t.Type, accuracy = t.AvgCorrect, avgTimeMs = t.AvgResponseTime }).ToList(),
                    activityLast30d = completeActivityData.Select(a => new { date = a.Date, count = a.Count }).ToList(),
                    recentSessions = recentSessions.Select(s => new { sessionId = s.SessionId, nationalId = s.NationalId, score = s.Score, createdAt = s.CreatedAt }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error getting analytics overview");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}
