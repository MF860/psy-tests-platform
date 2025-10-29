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

                // Process SDJ V2 results (7 patterns)
                var patternScores = new Dictionary<string, List<double>>();
                var allTScores = new List<double>();

                foreach (var r in results)
                {
                    if (string.IsNullOrWhiteSpace(r.DimensionScoresJson)) continue;
                    
                    try
                    {
                        // Parse SDJ V2 structure
                        var sdjData = TryParseSdjV2Data(r.DimensionScoresJson);
                        if (sdjData != null && sdjData.PatternScores != null)
                        {
                            // Aggregate pattern scores (P1-P7)
                            foreach (var pattern in sdjData.PatternScores)
                            {
                                if (!patternScores.ContainsKey(pattern.PatternNameAr))
                                {
                                    patternScores[pattern.PatternNameAr] = new List<double>();
                                }
                                patternScores[pattern.PatternNameAr].Add(pattern.TScore);
                                allTScores.Add(pattern.TScore);
                            }
                        }
                        else
                        {
                            // Try legacy SDJ V1 format
                            var legacyData = TryParseSdjData(r.DimensionScoresJson);
                            if (legacyData != null && legacyData.Dimensions != null)
                            {
                                foreach (var dim in legacyData.Dimensions)
                                {
                                    if (!patternScores.ContainsKey(dim.Dimension))
                                    {
                                        patternScores[dim.Dimension] = new List<double>();
                                    }
                                    patternScores[dim.Dimension].Add(dim.T);
                                    allTScores.Add(dim.T);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning(ex, "Failed to parse dimension scores for result, skipping");
                        continue;
                    }
                }

                // Calculate averages by dimension/pattern
                var dimensionAvgs = patternScores
                    .Where(p => p.Value.Any())
                    .Select(p => new
                    {
                        Dimension = p.Key,
                        AvgT = p.Value.Average(),
                        AvgPercentile = 0.0 // Not used in charts
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

                // Score Distribution - bucket all T-scores from all dimensions
                var scoreDistribution = new List<object>
                {
                    new { score = "يحتاج التقييم (< 40)", range = "< 40", count = allTScores.Count(t => t < 40) },
                    new { score = "أقل من المتوسط (40-44)", range = "40-44", count = allTScores.Count(t => t >= 40 && t < 45) },
                    new { score = "متوسط (45-54)", range = "45-54", count = allTScores.Count(t => t >= 45 && t < 55) },
                    new { score = "أعلى من المتوسط (55-59)", range = "55-59", count = allTScores.Count(t => t >= 55 && t < 60) },
                    new { score = "ممتاز (≥ 60)", range = "≥ 60", count = allTScores.Count(t => t >= 60) }
                };

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
                    recentSessions = recentSessions.Select(s => new { sessionId = s.SessionId, nationalId = s.NationalId, score = s.Score, createdAt = s.CreatedAt }).ToList(),
                    scoreDistribution = scoreDistribution
                });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error getting analytics overview");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        // SDJ Data Parsing Helpers
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

        // SDJ V2 Data Parsing (7 Patterns)
        private static SdjV2DataWrapper? TryParseSdjV2Data(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                var options = new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                var data = JsonSerializer.Deserialize<SdjV2DataWrapper>(json, options);
                // Check if it has SDJ V2 structure (PatternScores array)
                if (data?.PatternScores != null && data.PatternScores.Any())
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

        private class SdjV2DataWrapper
        {
            public List<SdjV2Pattern>? PatternScores { get; set; }
            public List<SdjV2SubDim>? SubDimensionScores { get; set; }
        }

        private class SdjV2Pattern
        {
            public string PatternId { get; set; } = string.Empty;
            public string PatternNameAr { get; set; } = string.Empty;
            public double TScore { get; set; }
            public string Band { get; set; } = string.Empty;
        }

        private class SdjV2SubDim
        {
            public string SubId { get; set; } = string.Empty;
            public string SubNameAr { get; set; } = string.Empty;
            public double TScore { get; set; }
            public string Band { get; set; } = string.Empty;
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
    }
}
