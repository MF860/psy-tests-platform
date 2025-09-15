using psy_tests_platform.Models;
using psy_tests_platform.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using System.Globalization;

namespace psy_tests_platform.Services.Scoring
{
    public class ScoringService : IScoringService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ScoringService> _logger;

        // Dictionary for Arabic character normalization
        private static readonly Dictionary<char, char> ArabicNormalizationMap = new Dictionary<char, char>
        {
            {'أ', 'ا'}, {'إ', 'ا'}, {'آ', 'ا'},
            {'ة', 'ه'}, {'ى', 'ي'}, {'ئ', 'ي'},
            {'ؤ', 'و'}, {'ك', 'ك'}, {'گ', 'ك'}
        };

        public ScoringService(ApplicationDbContext context, ILogger<ScoringService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ScoreSummary> ComputeSessionScores(int sessionId)
        {
            // Get all session items with answers
            var sessionItems = await _context.SessionItems
                .Where(si => si.SessionId == sessionId)
                .Include(si => si.TestItem)
                .Include(si => si.Answer)
                .ToListAsync();

            if (sessionItems == null || !sessionItems.Any())
            {
                throw new ArgumentException($"No session items found for session ID: {sessionId}");
            }

            // Process each item
            var dimensionScores = new ConcurrentDictionary<string, List<double>>();
            var totalRawScore = 0.0;
            var maxPossibleScore = 0.0;

            foreach (var sessionItem in sessionItems)
            {
                var item = sessionItem.TestItem;
                var answer = sessionItem.Answer;
                var scoringParams = item.ScoringParameters ?? new Dictionary<string, object>();

                double itemScore = 0;
                double itemMaxScore = item.MaxScore;

                switch (item.ItemType)
                {
                    case "MCQ":
                        var mcqResult = ScoreMcq(item, answer, scoringParams);
                        itemScore = mcqResult.score;
                        break;
                    case "TimedNumeric":
                        var timedResult = ScoreTimedNumeric(item, answer, sessionItem.ResponseTimeMs ?? 0, scoringParams);
                        itemScore = timedResult.score;
                        break;
                    case "Ordering":
                        var orderingResult = ScoreOrdering(item, answer, scoringParams);
                        itemScore = orderingResult.score;
                        break;
                    case "Likert":
                        var likertResult = ScoreLikert(item, answer, scoringParams);
                        itemScore = likertResult.score;
                        break;
                    case "Text":
                        var textResult = ScoreText(item, answer, scoringParams);
                        itemScore = textResult.score;
                        break;
                    default:
                        _logger.LogWarning($"Unknown item type: {item.ItemType} for item ID: {item.Id}");
                        continue;
                }

                // Add to dimension scores
                if (item.Dimension != null)
                {
                    dimensionScores.AddOrUpdate(
                        item.Dimension,
                        new List<double> { itemScore },
                        (key, existingList) =>
                        {
                            existingList.Add(itemScore);
                            return existingList;
                        });
                }

                totalRawScore += itemScore;
                maxPossibleScore += itemMaxScore;
            }

            // Aggregate by dimension
            var aggregatedDimensions = AggregateByDimension(sessionItems, dimensionScores);

            // Create and return score summary
            var scoreSummary = new ScoreSummary
            {
                DimensionScores = aggregatedDimensions,
                TotalScore = new TotalScore
                {
                    Raw = totalRawScore,
                    MaxPossible = maxPossibleScore,
                    Percentage = maxPossibleScore > 0 ? (totalRawScore / maxPossibleScore) * 100 : 0
                },
                Version = "v1.0"
            };

            return scoreSummary;
        }

        public (bool rawCorrect, double score) ScoreMcq(TestItem item, Answer answer, Dictionary<string, object> parameters)
        {
            // Get correct answer
            var correctAnswer = item.CorrectAnswer?.ToString() ?? string.Empty;
            var userAnswer = answer?.Value?.ToString() ?? string.Empty;

            // For MCQ, check if answer matches exactly
            bool isCorrect = string.Equals(correctAnswer, userAnswer, StringComparison.OrdinalIgnoreCase);

            // Calculate score (full points if correct, 0 otherwise)
            double score = isCorrect ? item.MaxScore : 0;

            return (isCorrect, score);
        }

        public double ScoreTimedNumeric(TestItem item, Answer answer, long responseTimeMs, Dictionary<string, object> parameters)
        {
            // Get correct answer and user answer
            if (!double.TryParse(item.CorrectAnswer?.ToString(), out double correctValue) ||
                !double.TryParse(answer?.Value?.ToString(), out double userValue))
            {
                return 0;
            }

            // Get time limit if available
            int timeLimitMs = parameters.TryGetValue("TimeLimitMs", out object tlObj) && tlObj is int tl ? tl : 30000; // Default 30 seconds

            // Get tolerance based on difficulty (default to 0% if difficulty not in 1-5 range)
            int difficulty = parameters.TryGetValue("Difficulty", out object diffObj) && diffObj is int d ? d : 1;
            double[] tolerancePercentages = { 0, 0, 2, 5, 5 }; // For difficulties 1-5
            double tolerancePercentage = difficulty >= 1 && difficulty <= 5 ? tolerancePercentages[difficulty - 1] : 0;

            // Calculate tolerance range
            double tolerance = correctValue * (tolerancePercentage / 100);
            bool isWithinTolerance = Math.Abs(userValue - correctValue) <= tolerance;

            // Base score (full if within tolerance, 0 otherwise)
            double baseScore = isWithinTolerance ? item.MaxScore : 0;

            // Apply time weighting if response time is available
            if (responseTimeMs > 0 && timeLimitMs > 0)
            {
                // Lambda parameter for time weighting (default 1.0)
                double lambda = parameters.TryGetValue("TimeLambda", out object lambdaObj) && lambdaObj is double l ? l : 1.0;

                // Calculate time weight: w_t = exp(-lambda * max(0, (t - limit)/limit))
                double timeRatio = Math.Max(0, (responseTimeMs - timeLimitMs) / (double)timeLimitMs);
                double timeWeight = Math.Exp(-lambda * timeRatio);

                // Apply time weight to base score
                baseScore *= timeWeight;
            }

            // Clamp score between 0 and MaxScore
            return Math.Clamp(baseScore, 0, item.MaxScore);
        }

        public double ScoreOrdering(TestItem item, Answer answer, Dictionary<string, object> parameters)
        {
            // Get correct order and user order
            var correctOrder = parameters.TryGetValue("CorrectOrder", out object coObj) && coObj is List<object> coList 
                ? coList.Select(x => x.ToString()).ToList() 
                : new List<string>();

            var userOrder = answer?.Value as List<object>?.Select(x => x.ToString()).ToList() ?? new List<string>();

            if (correctOrder.Count == 0 || userOrder.Count == 0 || correctOrder.Count != userOrder.Count)
            {
                return 0;
            }

            // Calculate Kendall tau correlation
            double kendallTau = CalculateKendallTau(correctOrder, userOrder);

            // Normalize Kendall tau to [0, 1] where 1 means perfect ordering
            double normalizedKendall = (kendallTau + 1) / 2;

            // Calculate accuracy as 1 - normalized Kendall tau
            double accuracy = 1 - normalizedKendall;

            // Final score is accuracy * MaxScore
            return accuracy * item.MaxScore;
        }

        public double ScoreLikert(TestItem item, Answer answer, Dictionary<string, object> parameters)
        {
            // Get Likert value (should be 1-5)
            if (!int.TryParse(answer?.Value?.ToString(), out int likertValue) || likertValue < 1 || likertValue > 5)
            {
                return 0;
            }

            // Linear map from [1, 5] to [0, MaxScore]
            double score = ((likertValue - 1) / 4.0) * item.MaxScore;

            // For future GRM implementation, we would store the raw value
            // and compute category probabilities with thresholds if provided

            return score;
        }

        public double ScoreText(TestItem item, Answer answer, Dictionary<string, object> parameters)
        {
            var text = answer?.Value?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(text))
            {
                return 0;
            }

            // Get keyword rubric for each dimension
            var keywordRubrics = parameters.TryGetValue("KeywordRubrics", out object krObj) && krObj is Dictionary<string, object> krDict
                ? krDict.ToDictionary(kvp => kvp.Key, kvp => kvp.Value as List<object>)
                : new Dictionary<string, List<object>>();

            double coverageScore = 0;
            int dimensionsCount = keywordRubrics.Count;

            if (dimensionsCount > 0)
            {
                // Calculate coverage score for each dimension
                double totalCoverage = 0;

                foreach (var dimension in keywordRubrics)
                {
                    var keywords = dimension.Value?.Select(x => x.ToString()).ToList() ?? new List<string>();
                    if (keywords.Count == 0) continue;

                    // Count how many keywords appear in the text (with fuzzy matching)
                    int matchedKeywords = 0;
                    foreach (var keyword in keywords)
                    {
                        if (IsFuzzyMatch(text, keyword))
                        {
                            matchedKeywords++;
                        }
                    }

                    // Calculate coverage for this dimension
                    double dimensionCoverage = keywords.Count > 0 ? (double)matchedKeywords / keywords.Count : 0;
                    totalCoverage += dimensionCoverage;
                }

                // Average coverage across all dimensions
                coverageScore = dimensionsCount > 0 ? totalCoverage / dimensionsCount : 0;
            }

            // Length bonus (simple heuristic: longer answers might be better, up to a point)
            double lengthBonus = Math.Min(1.0, text.Length / 500.0); // Normalize to reasonable length

            // Simple coherence heuristic (count sentences, assume more sentences = more coherent)
            int sentenceCount = Regex.Matches(text, @"[.!?]+").Count;
            double coherenceScore = Math.Min(1.0, sentenceCount / 10.0); // Normalize to reasonable sentence count

            // Combine scores with weights
            double finalScore = (coverageScore * 0.6) + (lengthBonus * 0.2) + (coherenceScore * 0.2);

            // Scale to MaxScore
            return finalScore * item.MaxScore;
        }

        // Helper method to calculate Kendall tau correlation
        private double CalculateKendallTau(List<string> correctOrder, List<string> userOrder)
        {
            int n = correctOrder.Count;
            if (n != userOrder.Count || n == 0)
                return 0;

            // Create position mappings
            var correctPositions = new Dictionary<string, int>();
            for (int i = 0; i < n; i++)
            {
                correctPositions[correctOrder[i]] = i;
            }

            // Count concordant and discordant pairs
            int concordant = 0;
            int discordant = 0;

            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    string a = userOrder[i];
                    string b = userOrder[j];

                    if (!correctPositions.ContainsKey(a) || !correctPositions.ContainsKey(b))
                        continue;

                    int aCorrectPos = correctPositions[a];
                    int bCorrectPos = correctPositions[b];

                    if ((aCorrectPos < bCorrectPos && i < j) || (aCorrectPos > bCorrectPos && i > j))
                    {
                        concordant++;
                    }
                    else if ((aCorrectPos < bCorrectPos && i > j) || (aCorrectPos > bCorrectPos && i < j))
                    {
                        discordant++;
                    }
                }
            }

            // Calculate Kendall tau
            int totalPairs = n * (n - 1) / 2;
            if (totalPairs == 0) return 0;

            return (concordant - discordant) / (double)totalPairs;
        }

        // Helper method for fuzzy Arabic text matching
        private bool IsFuzzyMatch(string text, string keyword)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(keyword))
                return false;

            // Normalize Arabic characters
            string normalizedText = NormalizeArabic(text);
            string normalizedKeyword = NormalizeArabic(keyword);

            // Check if normalized keyword is in normalized text
            return normalizedText.Contains(normalizedKeyword, StringComparison.OrdinalIgnoreCase);
        }

        // Helper method to normalize Arabic text
        private string NormalizeArabic(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var result = new StringBuilder(input.Length);
            foreach (char c in input)
            {
                result.Append(ArabicNormalizationMap.TryGetValue(c, out char normalized) ? normalized : c);
            }

            return result.ToString();
        }

        // Helper method to aggregate scores by dimension
        private List<DimensionScore> AggregateByDimension(List<SessionItem> sessionItems, ConcurrentDictionary<string, List<double>> dimensionScores)
        {
            var result = new List<DimensionScore>();

            foreach (var dimension in dimensionScores.Keys)
            {
                var scores = dimensionScores[dimension];
                if (scores.Count == 0)
                    continue;

                // Calculate raw score (mean)
                double rawScore = scores.Average();

                // For Z-score and percentile, we would normally use historical data
                // For now, we'll use placeholder calculations
                double zScore = 0; // Would be (rawScore - historicalMean) / historicalStdDev
                double tScore = 50 + 10 * zScore; // T-score transformation
                double percentile = 0.5; // Would be calculated from empirical distribution

                result.Add(new DimensionScore
                {
                    Dimension = dimension,
                    Raw = rawScore,
                    Z = zScore,
                    T = tScore,
                    Percentile = percentile
                });
            }

            return result;
        }
    }
}
}
