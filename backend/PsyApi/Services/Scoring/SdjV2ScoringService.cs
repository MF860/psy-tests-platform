using Microsoft.EntityFrameworkCore;
using PsyApi.Data;
using PsyApi.Models;

namespace PsyApi.Services.Scoring
{
    /// <summary>
    /// SDJ V2 Seven Patterns Scoring Service
    /// Works with PatternId and SubId fields (P1-P7, P1_S1-P7_S1)
    /// Supports both MCQ and Likert items
    /// </summary>
    public class SdjV2ScoringService : ISdjV2ScoringService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SdjV2ScoringService> _logger;

        // Population norms for T-score calculation
        private const double POPULATION_MEAN = 3.0;  // Middle of 1-5 Likert scale
        private const double POPULATION_SD = 0.8;     // Typical SD for Likert responses
        private const double T_SCORE_MEAN = 50.0;
        private const double T_SCORE_SD = 10.0;

        public SdjV2ScoringService(AppDbContext context, ILogger<SdjV2ScoringService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<SdjV2ScoreSummary> ComputeSdjV2Scores(int sessionId)
        {
            _logger.LogInformation("[SDJ V2] Computing scores for session {SessionId}", sessionId);

            // Get all session items with their items
            var sessionItems = await _context.SessionItems
                .Where(si => si.SessionId == sessionId)
                .Include(si => si.Item)
                .ToListAsync();

            if (sessionItems == null || !sessionItems.Any())
            {
                throw new ArgumentException($"No session items found for session ID: {sessionId}");
            }

            // Filter for V2 items (PatternId is present)
            var v2Items = sessionItems.Where(si => 
                !string.IsNullOrWhiteSpace(si.Item.PatternId) &&
                !string.IsNullOrWhiteSpace(si.Item.SubId) &&
                si.Answer != null).ToList();

            if (!v2Items.Any())
            {
                _logger.LogWarning("[SDJ V2] No V2 items found for session {SessionId}", sessionId);
                throw new InvalidOperationException("No SDJ V2 items found. Ensure PatternId/SubId are populated.");
            }

            _logger.LogInformation("[SDJ V2] Processing {Count} V2 items", v2Items.Count);

            // Step 1: Score each item (MCQ and Likert with reverse scoring)
            var itemScores = new Dictionary<int, double>();
            foreach (var si in v2Items)
            {
                var score = ScoreItem(si.Item, si.Answer ?? "");
                itemScores[si.ItemId] = score;
                _logger.LogDebug("[SDJ V2] Item {ItemCode}: Type={Type}, Answer={Answer}, Reverse={Reverse}, Score={Score}", 
                    si.Item.ItemCode, si.Item.Type, si.Answer, si.Item.Reverse, score);
            }

            // Step 2: Aggregate by sub-dimension (P1_S1, P1_S2, etc.)
            var subDimensionScores = AggregateBySubDimension(v2Items, itemScores);
            _logger.LogInformation("[SDJ V2] Aggregated {Count} sub-dimensions", subDimensionScores.Count);

            // Step 3: Aggregate by pattern (P1-P7)
            var patternScores = AggregateByPattern(v2Items, subDimensionScores);
            _logger.LogInformation("[SDJ V2] Aggregated {Count} patterns", patternScores.Count);

            // Step 4: Compute overall statistics
            var overallScore = new SdjV2OverallScore
            {
                Raw = patternScores.Average(p => p.Raw),
                TScore = patternScores.Average(p => p.TScore),
                Percentile = patternScores.Average(p => p.Percentile),
                Band = GetBand(patternScores.Average(p => p.TScore))
            };

            _logger.LogInformation("[SDJ V2] Overall T-Score: {TScore}, Band: {Band}", 
                overallScore.TScore, overallScore.Band);

            return new SdjV2ScoreSummary
            {
                PatternScores = patternScores,
                SubDimensionScores = subDimensionScores,
                OverallScore = overallScore,
                Version = "SDJ_v2.1_SevenPatterns",
                ItemCount = v2Items.Count,
                McqCount = v2Items.Count(si => si.Item.Type == "MCQ"),
                LikertCount = v2Items.Count(si => si.Item.Type == "LikertAgreement")
            };
        }

        /// <summary>
        /// Scores a single item (MCQ or Likert)
        /// MCQ: A=1, B=2, C=3, D=4 (or correct answer gets max score)
        /// Likert: 1-5 scale
        /// Both support reverse scoring
        /// </summary>
        private double ScoreItem(Item item, string answer)
        {
            double rawValue;

            if (item.Type == "MCQ")
            {
                rawValue = ScoreMcqItem(item, answer);
            }
            else if (item.Type == "LikertAgreement" || item.Type == "Frequency")
            {
                rawValue = ScoreLikertItem(item, answer);
            }
            else
            {
                _logger.LogWarning("[SDJ V2] Unknown item type '{Type}' for {ItemCode}, defaulting to 3", 
                    item.Type, item.ItemCode);
                rawValue = 3.0;
            }

            // Apply reverse scoring if needed
            return item.Reverse ? (6 - rawValue) : rawValue;
        }

        /// <summary>
        /// Scores MCQ item
        /// If CorrectAnswer is specified, correct=5, incorrect=1
        /// Otherwise, A=1, B=2, C=3, D=4
        /// </summary>
        private double ScoreMcqItem(Item item, string answer)
        {
            // Normalize answer to uppercase
            var normalizedAnswer = answer?.Trim().ToUpper();

            // If correct answer is specified, use binary scoring
            if (!string.IsNullOrWhiteSpace(item.CorrectAnswer))
            {
                var correctAnswer = item.CorrectAnswer.Trim().ToUpper();
                return normalizedAnswer == correctAnswer ? 5.0 : 1.0;
            }

            // Otherwise, map A/B/C/D to numeric scale
            return normalizedAnswer switch
            {
                "A" => 1.0,
                "B" => 2.0,
                "C" => 3.0,
                "D" => 4.0,
                _ => 3.0 // Default to middle if invalid
            };
        }

        /// <summary>
        /// Scores Likert item (1-5 scale)
        /// </summary>
        private double ScoreLikertItem(Item item, string answer)
        {
            if (!int.TryParse(answer, out var rawValue) || rawValue < 1 || rawValue > 5)
            {
                _logger.LogWarning("[SDJ V2] Invalid Likert answer '{Answer}' for {ItemCode}, defaulting to 3", 
                    answer, item.ItemCode);
                return 3.0;
            }

            return rawValue;
        }

        /// <summary>
        /// Aggregates item scores by sub-dimension (P1_S1, P2_S1, etc.)
        /// </summary>
        private List<SdjV2SubDimensionScore> AggregateBySubDimension(
            List<SessionItem> sessionItems, 
            Dictionary<int, double> itemScores)
        {
            var subDimGroups = sessionItems
                .Where(si => !string.IsNullOrWhiteSpace(si.Item.SubId))
                .GroupBy(si => new { si.Item.PatternId, si.Item.SubId, si.Item.SubKey, si.Item.SubNameAr });

            var results = new List<SdjV2SubDimensionScore>();

            foreach (var group in subDimGroups)
            {
                var scores = group.Where(si => itemScores.ContainsKey(si.ItemId))
                    .Select(si => itemScores[si.ItemId])
                    .ToList();

                if (!scores.Any()) continue;

                var raw = scores.Average();
                var tScore = ComputeTScore(raw);
                var percentile = ComputePercentile(tScore);
                var band = GetBand(tScore);

                results.Add(new SdjV2SubDimensionScore
                {
                    PatternId = group.Key.PatternId ?? "",
                    SubId = group.Key.SubId ?? "",
                    SubKey = group.Key.SubKey ?? "",
                    SubNameAr = group.Key.SubNameAr ?? "",
                    Raw = raw,
                    TScore = tScore,
                    Percentile = percentile,
                    Band = band,
                    ItemCount = scores.Count
                });

                _logger.LogDebug("[SDJ V2] Sub-dimension {SubId} ({SubNameAr}): Raw={Raw:F2}, T={TScore:F2}, Band={Band}", 
                    group.Key.SubId, group.Key.SubNameAr, raw, tScore, band);
            }

            return results.OrderBy(s => s.PatternId).ThenBy(s => s.SubId).ToList();
        }

        /// <summary>
        /// Aggregates sub-dimension scores by pattern (P1-P7)
        /// </summary>
        private List<SdjV2PatternScore> AggregateByPattern(
            List<SessionItem> sessionItems,
            List<SdjV2SubDimensionScore> subDimensionScores)
        {
            var patternGroups = subDimensionScores
                .GroupBy(sd => sd.PatternId);

            var results = new List<SdjV2PatternScore>();

            foreach (var group in patternGroups)
            {
                var patternId = group.Key;
                var subDimensions = group.ToList();

                // Get pattern metadata from first item in this pattern
                var sampleItem = sessionItems.First(si => si.Item.PatternId == patternId);

                var avgRaw = subDimensions.Average(sd => sd.Raw);
                var avgT = subDimensions.Average(sd => sd.TScore);
                var avgPercentile = subDimensions.Average(sd => sd.Percentile);
                var band = GetBand(avgT);

                results.Add(new SdjV2PatternScore
                {
                    PatternId = patternId,
                    PatternKey = sampleItem.Item.PatternKey ?? "",
                    PatternNameAr = sampleItem.Item.PatternNameAr ?? "",
                    Raw = avgRaw,
                    TScore = avgT,
                    Percentile = avgPercentile,
                    Band = band,
                    SubDimensionCount = subDimensions.Count,
                    SubDimensions = subDimensions
                });

                _logger.LogDebug("[SDJ V2] Pattern {PatternId} ({PatternNameAr}): Raw={Raw:F2}, T={TScore:F2}, Band={Band}", 
                    patternId, sampleItem.Item.PatternNameAr, avgRaw, avgT, band);
            }

            return results.OrderBy(p => p.PatternId).ToList();
        }

        /// <summary>
        /// Computes T-score from raw score
        /// T = 50 + 10 * (raw - μ) / σ
        /// </summary>
        private double ComputeTScore(double raw)
        {
            var tScore = T_SCORE_MEAN + T_SCORE_SD * (raw - POPULATION_MEAN) / POPULATION_SD;
            
            // Clamp between 20 and 80 for practical purposes
            return Math.Clamp(tScore, 20, 80);
        }

        /// <summary>
        /// Computes percentile from T-score (assuming normal distribution)
        /// </summary>
        private double ComputePercentile(double tScore)
        {
            // Simplified percentile approximation based on T-score
            // T=50 → 0.50, T=60 → ~0.84, T=40 → ~0.16
            var z = (tScore - T_SCORE_MEAN) / T_SCORE_SD;
            return NormalCDF(z);
        }

        /// <summary>
        /// Determines performance band based on T-score
        /// </summary>
        private string GetBand(double tScore)
        {
            if (tScore < 40) return "ضعيف";      // Weak
            if (tScore < 55) return "متوسط";     // Average
            return "ممتاز";                       // Excellent
        }

        /// <summary>
        /// Normal cumulative distribution function (CDF)
        /// Approximation for percentile calculation
        /// </summary>
        private double NormalCDF(double z)
        {
            // Using error function approximation
            var t = 1.0 / (1.0 + 0.5 * Math.Abs(z));
            var tau = t * Math.Exp(-z * z - 1.26551223 +
                                    t * (1.00002368 +
                                    t * (0.37409196 +
                                    t * (0.09678418 +
                                    t * (-0.18628806 +
                                    t * (0.27886807 +
                                    t * (-1.13520398 +
                                    t * (1.48851587 +
                                    t * (-0.82215223 +
                                    t * 0.17087277)))))))));
            return z >= 0 ? 1 - tau : tau;
        }
    }

    // ============================================================================
    // INTERFACE
    // ============================================================================

    public interface ISdjV2ScoringService
    {
        Task<SdjV2ScoreSummary> ComputeSdjV2Scores(int sessionId);
    }

    // ============================================================================
    // DTOs
    // ============================================================================

    public class SdjV2ScoreSummary
    {
        public List<SdjV2PatternScore> PatternScores { get; set; } = new();
        public List<SdjV2SubDimensionScore> SubDimensionScores { get; set; } = new();
        public SdjV2OverallScore OverallScore { get; set; } = new();
        public string Version { get; set; } = "SDJ_v2.1_SevenPatterns";
        public int ItemCount { get; set; }
        public int McqCount { get; set; }
        public int LikertCount { get; set; }
    }

    public class SdjV2PatternScore
    {
        public string PatternId { get; set; } = string.Empty;        // P1, P2, ..., P7
        public string PatternKey { get; set; } = string.Empty;       // personality_patterns, etc.
        public string PatternNameAr { get; set; } = string.Empty;    // الأنماط الشخصية
        public double Raw { get; set; }                              // Average raw score (1-5)
        public double TScore { get; set; }                           // T-score (μ=50, σ=10)
        public double Percentile { get; set; }                       // 0.0-1.0
        public string Band { get; set; } = string.Empty;             // ضعيف / متوسط / ممتاز
        public int SubDimensionCount { get; set; }
        public List<SdjV2SubDimensionScore> SubDimensions { get; set; } = new();
    }

    public class SdjV2SubDimensionScore
    {
        public string PatternId { get; set; } = string.Empty;        // P1, P2, etc.
        public string SubId { get; set; } = string.Empty;            // P1_S1, P2_S3, etc.
        public string SubKey { get; set; } = string.Empty;           // mbti_personality_type
        public string SubNameAr { get; set; } = string.Empty;        // نمط الشخصية MBTI
        public double Raw { get; set; }                              // Average raw score (1-5)
        public double TScore { get; set; }                           // T-score (μ=50, σ=10)
        public double Percentile { get; set; }                       // 0.0-1.0
        public string Band { get; set; } = string.Empty;             // ضعيف / متوسط / ممتاز
        public int ItemCount { get; set; }
    }

    public class SdjV2OverallScore
    {
        public double Raw { get; set; }
        public double TScore { get; set; }
        public double Percentile { get; set; }
        public string Band { get; set; } = string.Empty;
    }
}
