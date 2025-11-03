using Microsoft.EntityFrameworkCore;
using PsyApi.Data;
using PsyApi.Models;
using System.Collections.Concurrent;

namespace PsyApi.Services.Scoring
{
    /// <summary>
    /// SDJ (Sustainable Development Journey) Scoring Service
    /// Implements reverse scoring, sub-dimension aggregation, T-score transformation, and track mapping
    /// </summary>
    public class SdjScoringService : ISdjScoringService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SdjScoringService> _logger;
        private readonly IConfiguration _configuration;

        // IMPROVED: Population norms for T-score calculation
        // Increased SD from 0.8 to 1.0 to allow more variance in T-scores
        // This prevents artificial clustering of scores in 45-55 range
        private const double POPULATION_MEAN = 3.0;  // Middle of 1-5 Likert scale
        private const double POPULATION_SD = 1.0;    // Increased from 0.8 to widen variance
        private const double POPULATION_SD_MIN = 0.4; // Minimum SD to prevent division by near-zero
        private const double T_SCORE_MEAN = 50.0;
        private const double T_SCORE_SD = 10.0;
        
        // Missing data threshold: if >20% items missing in subdimension, exclude it
        private const double MISSING_DATA_THRESHOLD = 0.2;

        public SdjScoringService(AppDbContext context, ILogger<SdjScoringService> logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<SdjScoreSummary> ComputeSdjScores(int sessionId)
        {
            _logger.LogInformation("[SDJ] Computing scores for session {SessionId}", sessionId);

            // Get all session items with their items
            var sessionItems = await _context.SessionItems
                .Where(si => si.SessionId == sessionId)
                .Include(si => si.Item)
                .ToListAsync();

            if (sessionItems == null || !sessionItems.Any())
            {
                throw new ArgumentException($"No session items found for session ID: {sessionId}");
            }

            // Filter for items with SDJ dimensions (SDJ mode)
            // IMPORTANT: Only include LikertAgreement and Frequency types (exclude MCQ, TIMED_NUMERIC, etc.)
            var sdjItems = sessionItems.Where(si => 
                si.Item.Dimension != null && 
                si.Item.SubDimension != null &&
                si.Answer != null &&
                (si.Item.Type == "LikertAgreement" || si.Item.Type == "Frequency")).ToList();

            if (!sdjItems.Any())
            {
                _logger.LogWarning("[SDJ] No SDJ items found for session {SessionId}, falling back to legacy scoring", sessionId);
                throw new InvalidOperationException("No SDJ items found. Ensure USE_SDJ=1 is set.");
            }

            _logger.LogInformation("[SDJ] Processing {Count} SDJ items", sdjItems.Count);

            // Step 1: Score each item (with reverse scoring)
            var itemScores = new Dictionary<int, double>();
            foreach (var si in sdjItems)
            {
                var score = ScoreLikertItem(si.Item, si.Answer ?? "3");
                itemScores[si.ItemId] = score;
                _logger.LogDebug("[SDJ] Item {ItemCode}: Answer={Answer}, Reverse={Reverse}, Score={Score}", 
                    si.Item.ItemCode, si.Answer, si.Item.Reverse, score);
            }

            // Step 2: Aggregate by sub-dimension
            var subDimensionScores = AggregateBySubDimension(sdjItems, itemScores);
            _logger.LogInformation("[SDJ] Aggregated {Count} sub-dimensions", subDimensionScores.Count);

            // Step 3: Aggregate by parent dimension
            var dimensionScores = AggregateByDimension(sdjItems, subDimensionScores);
            _logger.LogInformation("[SDJ] Aggregated {Count} parent dimensions", dimensionScores.Count);

            // Step 4: Map to SDJ tracks
            var trackFits = MapToSdjTracks(dimensionScores);
            _logger.LogInformation("[SDJ] Mapped to {Count} SDJ tracks", trackFits.Count);

            // Step 5: Compute overall statistics
            var totalScore = new SdjTotalScore
            {
                Raw = dimensionScores.Average(d => d.Raw),
                T = dimensionScores.Average(d => d.T),
                Percentile = dimensionScores.Average(d => d.Percentile)
            };

            // NEW: Compute 7-pattern scores
            var sevenPatternScores = SevenPatternsMapper.MapToSevenPatterns(new SdjScoreSummary
            {
                Dimensions = dimensionScores,
                SubDimensions = subDimensionScores,
                TotalScore = totalScore
            });

            _logger.LogInformation("[SDJ] Computed {Count} seven-pattern scores", sevenPatternScores.Count);

            return new SdjScoreSummary
            {
                Dimensions = dimensionScores,
                SubDimensions = subDimensionScores,
                TotalScore = totalScore,
                TrackFits = trackFits,
                SevenPatternScores = sevenPatternScores,
                Version = "SDJ_v2.0_7Patterns"
            };
        }

        private double ScoreLikertItem(Item item, string answer)
        {
            // Parse Likert answer (stored as "1", "2", "3", "4", or "5")
            if (!int.TryParse(answer, out var rawValue) || rawValue < 1 || rawValue > 5)
            {
                _logger.LogWarning("[SDJ] Invalid Likert answer '{Answer}' for item {ItemCode}, defaulting to 3", 
                    answer, item.ItemCode);
                rawValue = 3; // Neutral default
            }

            // Apply reverse scoring if needed
            var score = item.Reverse ? (6 - rawValue) : rawValue;
            
            return score;
        }

        private List<SdjSubDimensionScore> AggregateBySubDimension(
            List<SessionItem> sessionItems, 
            Dictionary<int, double> itemScores)
        {
            var subDimGroups = sessionItems
                .Where(si => si.Item.SubDimension != null)
                .GroupBy(si => new { si.Item.Dimension, si.Item.SubDimension });

            var results = new List<SdjSubDimensionScore>();

            foreach (var group in subDimGroups)
            {
                var totalItems = group.Count();
                var answeredItems = group.Where(si => itemScores.ContainsKey(si.ItemId)).ToList();
                var scores = answeredItems.Select(si => itemScores[si.ItemId]).ToList();

                if (!scores.Any()) continue;

                // IMPROVED: Check missing data threshold
                var missingRatio = 1.0 - ((double)answeredItems.Count / totalItems);
                
                // If >20% missing, use median imputation for robustness
                double raw;
                if (missingRatio > MISSING_DATA_THRESHOLD)
                {
                    _logger.LogWarning("[SDJ] SubDim {SubDim} has {MissingPct:P0} missing data, using median imputation", 
                        group.Key.SubDimension, missingRatio);
                    
                    // Use median of answered items (more robust than mean for sparse data)
                    var sortedScores = scores.OrderBy(s => s).ToList();
                    raw = sortedScores.Count % 2 == 0 
                        ? (sortedScores[sortedScores.Count / 2 - 1] + sortedScores[sortedScores.Count / 2]) / 2.0
                        : sortedScores[sortedScores.Count / 2];
                }
                else
                {
                    raw = scores.Average();
                }

                // IMPROVED: Use enhanced T-score computation with per-subdimension norms potential
                var tScore = ComputeTScore(raw, group.Key.SubDimension);
                var percentile = ComputePercentile(tScore);
                var band = GetBand(tScore);

                results.Add(new SdjSubDimensionScore
                {
                    Dimension = group.Key.Dimension!,
                    SubDimension = group.Key.SubDimension!,
                    Raw = raw,
                    T = tScore,
                    Percentile = percentile,
                    Band = band,
                    ItemCount = answeredItems.Count
                });
            }

            return results.OrderBy(s => s.T).ToList();
        }

        private List<SdjDimensionScore> AggregateByDimension(
            List<SessionItem> sessionItems,
            List<SdjSubDimensionScore> subDimensionScores)
        {
            var dimGroups = subDimensionScores.GroupBy(s => s.Dimension);
            var results = new List<SdjDimensionScore>();

            foreach (var group in dimGroups)
            {
                var subDims = group.ToList();
                var raw = subDims.Average(s => s.Raw);
                var tScore = ComputeTScore(raw, null); // Dimension-level uses global norms
                var percentile = ComputePercentile(tScore);
                var band = GetBand(tScore);

                results.Add(new SdjDimensionScore
                {
                    Dimension = group.Key,
                    Raw = raw,
                    T = tScore,
                    Percentile = percentile,
                    Band = band,
                    SubDimensions = subDims,
                    ItemCount = subDims.Sum(s => s.ItemCount)
                });
            }

            return results.OrderBy(d => d.T).ToList();
        }

        private double ComputeTScore(double rawScore, string? subDimension = null)
        {
            // IMPROVED: Support per-subdimension norms (future enhancement)
            // For now, use improved global defaults with wider SD for better variance
            
            var mean = POPULATION_MEAN;
            var sd = POPULATION_SD;
            
            // Future: Look up per-subdimension norms from database
            // var norm = await _context.SdjNorms.FirstOrDefaultAsync(n => n.SubDimension == subDimension);
            // if (norm != null) { mean = norm.Mean; sd = Math.Max(norm.StdDev, POPULATION_SD_MIN); }
            
            // Ensure SD is not too small (prevents artificial compression)
            sd = Math.Max(sd, POPULATION_SD_MIN);
            
            // Z-score: (raw - mean) / sd
            var zScore = (rawScore - mean) / sd;
            
            // T-score: 50 + 10*Z
            var tScore = T_SCORE_MEAN + (T_SCORE_SD * zScore);
            
            // Clamp to reasonable range [20, 80]
            return Math.Clamp(tScore, 20, 80);
        }

        private double ComputePercentile(double tScore)
        {
            // Approximate percentile from T-score using normal distribution
            // T=50 → 50th percentile, T=60 → 84th percentile, T=40 → 16th percentile
            
            var zScore = (tScore - T_SCORE_MEAN) / T_SCORE_SD;
            
            // Approximate CDF of standard normal distribution
            var percentile = 0.5 * (1 + Math.Tanh(zScore * Math.Sqrt(2 / Math.PI)));
            
            return Math.Clamp(percentile, 0.01, 0.99);
        }

        private string GetBand(double tScore)
        {
            if (tScore < 40) return "Weak";
            if (tScore < 55) return "Average";
            return "Excellent";
        }

        private List<SdjTrackFit> MapToSdjTracks(List<SdjDimensionScore> dimensions)
        {
            // SDJ Track Mapping based on dimension strengths
            var tracks = new List<SdjTrackFit>();

            // Track 1: مسار التميز الذاتي (Self-Excellence Track)
            var selfExcellence = dimensions.FirstOrDefault(d => d.Dimension == "التميز الذاتي");
            if (selfExcellence != null)
            {
                var fit = DetermineFitLevel(selfExcellence.T);
                var keyCompetencies = selfExcellence.SubDimensions
                    .OrderByDescending(s => s.T)
                    .Take(3)
                    .Select(s => s.SubDimension)
                    .ToList();

                tracks.Add(new SdjTrackFit
                {
                    TrackNameAr = "مسار التميز الذاتي",
                    TrackNameEn = "Self-Excellence Track",
                    FitLevel = fit,
                    FitScore = selfExcellence.T,
                    ReasoningAr = GenerateTrackReasoning("التميز الذاتي", selfExcellence, keyCompetencies),
                    KeyCompetencies = keyCompetencies
                });
            }

            // Track 2: مسار العلاقات المهنية (Professional Relationships Track)
            var relationships = dimensions.FirstOrDefault(d => d.Dimension == "التواصل والعلاقات");
            if (relationships != null)
            {
                var fit = DetermineFitLevel(relationships.T);
                var keyCompetencies = relationships.SubDimensions
                    .OrderByDescending(s => s.T)
                    .Take(3)
                    .Select(s => s.SubDimension)
                    .ToList();

                tracks.Add(new SdjTrackFit
                {
                    TrackNameAr = "مسار العلاقات المهنية",
                    TrackNameEn = "Professional Relationships Track",
                    FitLevel = fit,
                    FitScore = relationships.T,
                    ReasoningAr = GenerateTrackReasoning("التواصل والعلاقات", relationships, keyCompetencies),
                    KeyCompetencies = keyCompetencies
                });
            }

            // Track 3: مسار النجاح الوظيفي (Career Success Track)
            var careerSuccess = dimensions.FirstOrDefault(d => d.Dimension == "النجاح المهني");
            if (careerSuccess != null)
            {
                var fit = DetermineFitLevel(careerSuccess.T);
                var keyCompetencies = careerSuccess.SubDimensions
                    .OrderByDescending(s => s.T)
                    .Take(3)
                    .Select(s => s.SubDimension)
                    .ToList();

                tracks.Add(new SdjTrackFit
                {
                    TrackNameAr = "مسار النجاح الوظيفي",
                    TrackNameEn = "Career Success Track",
                    FitLevel = fit,
                    FitScore = careerSuccess.T,
                    ReasoningAr = GenerateTrackReasoning("النجاح المهني", careerSuccess, keyCompetencies),
                    KeyCompetencies = keyCompetencies
                });
            }

            return tracks.OrderByDescending(t => t.FitScore).ToList();
        }

        private string DetermineFitLevel(double tScore)
        {
            if (tScore >= 55) return "high";
            if (tScore >= 45) return "medium";
            return "low";
        }

        private string GenerateTrackReasoning(string dimension, SdjDimensionScore score, List<string> keyCompetencies)
        {
            var band = score.Band;
            var competenciesList = string.Join("، ", keyCompetencies);

            return band switch
            {
                "Excellent" => $"يظهر المشارك مستوى ممتاز في {dimension} (درجة T: {score.T:F1})، مع قوة خاصة في: {competenciesList}. هذا المسار يناسبه بشكل كبير.",
                "Average" => $"يظهر المشارك مستوى متوسط في {dimension} (درجة T: {score.T:F1})، مع إمكانيات للتطوير في: {competenciesList}. هذا المسار مناسب مع التركيز على التحسين.",
                _ => $"يظهر المشارك مستوى يحتاج للتطوير في {dimension} (درجة T: {score.T:F1})، ويُنصح بالتركيز على تحسين: {competenciesList}. هذا المسار يتطلب جهداً إضافياً."
            };
        }
    }

    // Interface definition
    public interface ISdjScoringService
    {
        Task<SdjScoreSummary> ComputeSdjScores(int sessionId);
    }

    // DTOs
    public class SdjScoreSummary
    {
        public List<SdjDimensionScore> Dimensions { get; set; } = new();
        public List<SdjSubDimensionScore> SubDimensions { get; set; } = new();
        public SdjTotalScore TotalScore { get; set; } = new();
        public List<SdjTrackFit> TrackFits { get; set; } = new();
        public List<SevenPatternScore> SevenPatternScores { get; set; } = new();
        public string Version { get; set; } = "SDJ_v2.0_7Patterns";
    }

    public class SdjDimensionScore
    {
        public string Dimension { get; set; } = string.Empty;
        public double Raw { get; set; }
        public double T { get; set; }
        public double Percentile { get; set; }
        public string Band { get; set; } = string.Empty;
        public List<SdjSubDimensionScore> SubDimensions { get; set; } = new();
        public int ItemCount { get; set; }
    }

    public class SdjSubDimensionScore
    {
        public string Dimension { get; set; } = string.Empty;
        public string SubDimension { get; set; } = string.Empty;
        public double Raw { get; set; }
        public double T { get; set; }
        public double Percentile { get; set; }
        public string Band { get; set; } = string.Empty;
        public int ItemCount { get; set; }
    }

    public class SdjTotalScore
    {
        public double Raw { get; set; }
        public double T { get; set; }
        public double Percentile { get; set; }
    }

    public class SdjTrackFit
    {
        public string TrackNameAr { get; set; } = string.Empty;
        public string TrackNameEn { get; set; } = string.Empty;
        public string FitLevel { get; set; } = string.Empty; // "high", "medium", "low"
        public double FitScore { get; set; }
        public string ReasoningAr { get; set; } = string.Empty;
        public List<string> KeyCompetencies { get; set; } = new();
    }
}
