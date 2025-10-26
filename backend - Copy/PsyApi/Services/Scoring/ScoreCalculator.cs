using System.Globalization;
using PsyApi.Models;
using PsyApi.Utils;

namespace PsyApi.Services.Scoring
{
    public class ScoreCalculator
    {
        public static int CalculateScore(SessionItem sessionItem)
        {
            if (string.IsNullOrWhiteSpace(sessionItem.Answer))
                return 0;

            var maxScore = sessionItem.Item?.MaxScore ?? 0;
            if (maxScore <= 0)
                return 0;

            return (sessionItem.Item?.Type ?? string.Empty).Trim() switch
            {
                "MCQ" => CalculateMcqScore(sessionItem),
                "TIMED_NUMERIC" => CalculateTimedNumericScore(sessionItem),
                "Text" => CalculateTextScore(sessionItem),
                "ORDERING" => CalculateOrderingScore(sessionItem),
                "LikertAgreement" or "Frequency" => CalculateLikertScore(sessionItem),
                _ => 0
            };
        }

        private static int CalculateMcqScore(SessionItem item)
        {
            // Validate correct_answer exists in options
            if (item.Item?.Options == null || item.Item?.CorrectAnswer == null) return 0;
            
            var options = item.Item.Options.Split('|', StringSplitOptions.RemoveEmptyEntries)
                .Select(o => o.Trim())
                .ToList();

            var correctAnswer = item.Item.CorrectAnswer.Trim();
            if (!options.Any(opt => ArabicTextNormalizer.AreEqual(opt, correctAnswer)))
                return 0; // Invalid item - correct_answer not in options

            // Score the answer
            return ArabicTextNormalizer.AreEqual(correctAnswer, item.Answer) 
                ? item.Item.MaxScore 
                : 0;
        }

        private static int CalculateTimedNumericScore(SessionItem item)
        {
            if (item.Item?.CorrectAnswer == null || !double.TryParse(item.Item.CorrectAnswer, 
                NumberStyles.Any, CultureInfo.InvariantCulture, out var correctValue))
                return 0;

            var (isValid, value, _) = NumericValidator.ValidateNumeric(item.Answer);
            if (!isValid || !value.HasValue)
                return 0;

            var userValue = value.Value;
            
            // Get difficulty-based tolerance percentage (0%, 0%, 2%, 5%, 5%)
            var difficulty = item.Item.Difficulty;
            var tolerancePercentages = new[] { 0.0, 0.0, 0.02, 0.05, 0.05 };
            var tolerancePercent = difficulty >= 1 && difficulty <= 5 
                ? tolerancePercentages[difficulty - 1] 
                : 0.0;

            var difference = Math.Abs(correctValue - userValue);
            var tolerance = correctValue * tolerancePercent;
            var withinTolerance = difference <= Math.Abs(tolerance);

            // Apply time decay if response time is available
            var score = withinTolerance ? item.Item.MaxScore : 0;
            if (score > 0 && item.ResponseTimeMs.HasValue)
            {
                var maxTime = item.Item.TimeLimitSeconds * 1000.0; // convert to ms
                if (maxTime > 0)
                {
                    var timeRatio = Math.Min(1.0, item.ResponseTimeMs.Value / maxTime);
                    score = (int)Math.Round(score * (1.0 - timeRatio * 0.5)); // Up to 50% penalty for slow answers
                }
            }
            
            return score;
        }

        private static int CalculateTextScore(SessionItem item)
        {
            if (string.IsNullOrEmpty(item.Answer))
                return 0;

            var text = item.Answer;
            double score = 0.0;

            // 1. Keyword coverage (60%)
            var keywordScore = 0.0;
            if (!string.IsNullOrEmpty(item.Item?.CorrectAnswer))
            {
                var correctWords = new HashSet<string>(
                    item.Item.CorrectAnswer.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(w => ArabicTextNormalizer.Normalize(w)));

                var userWords = text.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(w => ArabicTextNormalizer.Normalize(w))
                    .ToList();

                var matchedWords = correctWords.Count(w => userWords.Contains(w));
                keywordScore = correctWords.Count > 0 ? (double)matchedWords / correctWords.Count : 0;
            }

            // 2. Length bonus (20%) - Scale up to reasonable max length
            var words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var lengthScore = Math.Min(1.0, words.Length / 100.0); // Cap at 100 words

            // 3. Coherence (20%) - Based on sentence structure
            var coherenceScore = 0.0;
            var sentences = text.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            if (sentences.Count > 0)
            {
                // Check for reasonable sentence length
                var avgWordsPerSentence = words.Length / (double)sentences.Count;
                var reasonableLength = avgWordsPerSentence >= 3 && avgWordsPerSentence <= 20;

                // Count common Arabic conjunctions as cohesion markers
                var conjunctionCount = sentences.Count(s => 
                    s.Contains("و") || s.Contains("ف") || s.Contains("ثم") || 
                    s.Contains("لكن") || s.Contains("لأن") || s.Contains("حيث"));

                coherenceScore = Math.Min(1.0, 
                    (reasonableLength ? 0.5 : 0.0) + 
                    (conjunctionCount / (double)Math.Max(1, sentences.Count)) * 0.5);
            }

            // Combine scores with weights
            score = (keywordScore * 0.6) + (lengthScore * 0.2) + (coherenceScore * 0.2);
            
            // Scale to max score and round to integer
            return (int)Math.Round(Math.Clamp(score, 0.0, 1.0) * item.Item?.MaxScore ?? 0);
        }

        private static int CalculateOrderingScore(SessionItem item)
        {
            if (string.IsNullOrEmpty(item.Item?.CorrectAnswer))
                return 0;

            var correctOrder = item.Item.CorrectAnswer
                .Split('|', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .ToList();

            var userOrder = item.Answer?
                .Split('|', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .ToList() ?? new List<string>();

            // Filter to only items present in both lists
            var commonItems = new HashSet<string>(correctOrder);
            commonItems.IntersectWith(userOrder);

            var validUserOrder = userOrder.Where(commonItems.Contains).ToList();
            if (validUserOrder.Count < 2) // Need at least 2 items to compute correlation
                return 0;

            // Build position map for correct order
            var posMap = new Dictionary<string, int>();
            for (int i = 0; i < correctOrder.Count; i++)
                posMap[correctOrder[i]] = i;

            // Count discordant pairs (Kendall tau)
            int discordantPairs = 0;
            int totalPairs = 0;

            for (int i = 0; i < validUserOrder.Count; i++)
            {
                for (int j = i + 1; j < validUserOrder.Count; j++)
                {
                    var a = validUserOrder[i];
                    var b = validUserOrder[j];
                    if (!posMap.ContainsKey(a) || !posMap.ContainsKey(b))
                        continue;

                    totalPairs++;
                    // Check if pair ordering disagrees with correct order
                    if ((posMap[a] < posMap[b] && i > j) || (posMap[a] > posMap[b] && i < j))
                        discordantPairs++;
                }
            }

            if (totalPairs == 0)
                return 0;

            // Calculate normalized Kendall tau correlation
            var kendallTau = 1.0 - (2.0 * discordantPairs) / totalPairs; // Range [-1, 1]
            var normalized = (kendallTau + 1.0) / 2.0; // Range [0, 1]
            
            return (int)Math.Round(normalized * item.Item.MaxScore);
        }

        private static int CalculateLikertScore(SessionItem item)
        {
            if (item.Item == null || string.IsNullOrEmpty(item.Answer))
                return 0;

            // Get canonical options list
            var options = (item.Item.Type == "LikertAgreement")
                ? new[] { "لا أوافق بشدة", "لا أوافق", "محايد", "أوافق", "أوافق بشدة" }
                : new[] { "أبدًا", "نادرًا", "أحيانًا", "غالبًا", "دائمًا" };

            // Try to map answer to numeric value (1-5)
            int value;
            if (int.TryParse(item.Answer, out var numeric) && numeric >= 1 && numeric <= 5)
            {
                value = numeric;
            }
            else
            {
                // Look for exact label match
                var normalizedAnswer = ArabicTextNormalizer.Normalize(item.Answer);
                var idx = Array.FindIndex(options, o => ArabicTextNormalizer.AreEqual(o, normalizedAnswer));
                if (idx < 0) return item.Item.MaxScore / 2; // Invalid answer, return middle score
                value = idx + 1;
            }

            // For GRM model, we have fixed thresholds dividing the latent trait scale
            var thresholds = new[] { -1.5, -0.5, 0.5, 1.5 }; // Standard thresholds
            var scores = new[] { 0.0, 0.25, 0.5, 0.75, 1.0 }; // Score mappings
            
            // Map value to score using GRM thresholds
            var normalized = scores[value - 1];
            return (int)Math.Round(normalized * item.Item.MaxScore);
        }
    }
}