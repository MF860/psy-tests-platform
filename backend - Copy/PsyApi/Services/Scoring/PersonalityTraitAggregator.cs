using System.Text.Json;
using PsyApi.Models;

namespace PsyApi.Services.Scoring
{
    public class TraitScore
    {
        public string Dimension { get; set; } = string.Empty;
        public double Raw { get; set; }
        public double NormalizedScore { get; set; }  // 0-1 scale
        public double ConfidenceLevel { get; set; }  // 0-1 scale
        public double LowerBound { get; set; }       // 95% confidence interval
        public double UpperBound { get; set; }
        public string[] SupportingEvidence { get; set; } = Array.Empty<string>();
        public string[] ConflictingEvidence { get; set; } = Array.Empty<string>();
    }

    public class PersonalityTraitAggregator
    {
        private static readonly Dictionary<string, string[]> TraitCorrelations = new(StringComparer.OrdinalIgnoreCase)
        {
            // Core personality dimensions and their related behavioral indicators
            ["extraversion"] = new[] { "social", "outgoing", "energetic", "assertive", "talkative" },
            ["agreeableness"] = new[] { "cooperative", "compassionate", "helpful", "sympathetic", "kind" },
            ["conscientiousness"] = new[] { "organized", "responsible", "careful", "thorough", "efficient" },
            ["neuroticism"] = new[] { "anxious", "worried", "nervous", "stressed", "moody" },
            ["openness"] = new[] { "curious", "creative", "imaginative", "innovative", "artistic" }
        };

        public static List<TraitScore> AnalyzeTraits(IList<SessionItem> items, ResponseValidityScore validity)
        {
            var traits = new Dictionary<string, List<(double Score, double Weight)>>();
            var evidence = new Dictionary<string, (List<string> Supporting, List<string> Conflicting)>();
            
            foreach (var item in items.Where(i => i.Item.Type is "LikertAgreement" or "Frequency"))
            {
                if (!int.TryParse(item.Answer, out var response))
                    continue;

                var tags = (item.Item.DimensionTags?.Split(',').Select(t => t.Trim().ToLowerInvariant()) ?? Array.Empty<string>())
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .ToList();

                var isReverse = tags.Contains("reverse") || 
                               (item.Item.TextAr?.Contains("لا ") ?? false) ||
                               (item.Item.TextAr?.Contains("عدم ") ?? false);

                var normalizedScore = isReverse ? (6 - response) / 5.0 : response / 5.0;
                var baseWeight = item.ResponseTimeMs.HasValue ? 
                    Math.Min(1.0, item.ResponseTimeMs.Value / 1000.0) : 0.5;  // More weight to thoughtful answers

                foreach (var tag in tags.Where(t => TraitCorrelations.ContainsKey(t)))
                {
                    if (!traits.ContainsKey(tag))
                    {
                        traits[tag] = new List<(double Score, double Weight)>();
                        evidence[tag] = (new List<string>(), new List<string>());
                    }

                    traits[tag].Add((normalizedScore, baseWeight));

                    // Track supporting/conflicting evidence
                    var expectedBehaviors = TraitCorrelations[tag];
                    var questionText = item.Item.TextAr ?? "";
                    var matchingBehaviors = expectedBehaviors.Where(b => questionText.Contains(b, StringComparison.OrdinalIgnoreCase));
                    
                    if (matchingBehaviors.Any())
                    {
                        if ((normalizedScore > 0.7 && !isReverse) || (normalizedScore < 0.3 && isReverse))
                            evidence[tag].Supporting.Add(questionText);
                        else if ((normalizedScore < 0.3 && !isReverse) || (normalizedScore > 0.7 && isReverse))
                            evidence[tag].Conflicting.Add(questionText);
                    }
                }
            }

            // Calculate trait scores with confidence intervals
            var results = new List<TraitScore>();
            foreach (var (trait, scores) in traits)
            {
                if (!scores.Any()) continue;

                var weightedScores = scores.Select(s => s.Score * s.Weight).ToList();
                var weights = scores.Select(s => s.Weight).ToList();
                var mean = weightedScores.Sum() / weights.Sum();

                // Calculate confidence metrics
                var stdDev = Math.Sqrt(
                    weightedScores.Zip(weights, (s, w) => w * Math.Pow(s - mean, 2)).Sum() 
                    / weights.Sum()
                );

                var confidenceLevel = Math.Min(1.0, 
                    (scores.Count / 10.0) *                    // More questions = higher confidence
                    (validity.ConsistencyIndex) *             // Response consistency factor
                    (1 - stdDev) *                           // Lower variance = higher confidence
                    validity.EngagementIndex                 // Engagement quality factor
                );

                // 95% confidence interval
                var marginOfError = 1.96 * stdDev / Math.Sqrt(scores.Count);

                results.Add(new TraitScore
                {
                    Dimension = trait,
                    Raw = mean * 100,  // 0-100 scale
                    NormalizedScore = mean,
                    ConfidenceLevel = confidenceLevel,
                    LowerBound = Math.Max(0, (mean - marginOfError) * 100),
                    UpperBound = Math.Min(100, (mean + marginOfError) * 100),
                    SupportingEvidence = evidence[trait].Supporting.ToArray(),
                    ConflictingEvidence = evidence[trait].Conflicting.ToArray()
                });
            }

            return results.OrderByDescending(t => t.ConfidenceLevel).ToList();
        }
    }
}