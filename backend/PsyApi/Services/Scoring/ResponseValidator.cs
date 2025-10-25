using System.Text.Json;
using PsyApi.Models;

namespace PsyApi.Services.Scoring
{
    public class ResponseValidator
    {
        private static readonly TimeSpan MinExpectedTime = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan MaxExpectedTime = TimeSpan.FromMinutes(5);
        
        public static ResponseValidityScore ValidateResponses(IList<SessionItem> items)
        {
            var score = new ResponseValidityScore();
            var warnings = new List<string>();

            // 1. Check response time patterns
            var validTimings = items
                .Where(i => i.ResponseTimeMs.HasValue)
                .Select(i => i.ResponseTimeMs!.Value)
                .ToList();

            if (validTimings.Any())
            {
                var tooFast = validTimings.Count(t => t < MinExpectedTime.TotalMilliseconds);
                var tooSlow = validTimings.Count(t => t > MaxExpectedTime.TotalMilliseconds);
                var timeEngagement = 1.0 - ((double)(tooFast + tooSlow) / validTimings.Count);
                score.EngagementIndex = timeEngagement;

                if (tooFast > validTimings.Count * 0.2)
                    warnings.Add("Unusually fast responses detected - potential random clicking");
            }

            // 2. Check for response patterns (especially in Likert/Frequency)
            var likertResponses = items
                .Where(i => i.Item.Type is "LikertAgreement" or "Frequency")
                .Select(i => int.TryParse(i.Answer, out var v) ? v : 0)
                .ToList();

            if (likertResponses.Any())
            {
                // Check for straight-lining (same answer repeatedly)
                var distinctAnswers = likertResponses.Distinct().Count();
                var varietyScore = Math.Min(distinctAnswers / 5.0, 1.0); // We expect use of different points on 1-5 scale

                // Check for zigzag patterns
                var transitions = likertResponses
                    .Skip(1)
                    .Zip(likertResponses, (curr, prev) => Math.Abs(curr - prev))
                    .ToList();
                
                var hasZigzag = transitions.Count > 4 && 
                    transitions.All(t => t >= 2) && 
                    transitions.Count(t => t == transitions[0]) > transitions.Count * 0.8;

                var randomnessScore = (varietyScore + (hasZigzag ? 0 : 1)) / 2.0;
                score.RandomnessIndex = randomnessScore;

                if (distinctAnswers == 1)
                    warnings.Add("All answers are identical - potential non-engagement");
                if (hasZigzag)
                    warnings.Add("Repetitive pattern detected - potential non-thoughtful responses");
            }

            // 3. Check answer consistency for similar trait questions
            var traitResponses = items
                .Where(i => i.Item.Type is "LikertAgreement" or "Frequency")
                .Select(i => new
                {
                    Tags = i.Item.DimensionTags?.Split(',').Select(t => t.Trim()).ToList() ?? new List<string>(),
                    Value = int.TryParse(i.Answer, out var v) ? v : 3,
                    IsReverse = (i.Item.DimensionTags?.Contains("reverse") ?? false) || 
                               (i.Item.TextAr?.Contains("لا ") ?? false) || 
                               (i.Item.TextAr?.Contains("عدم ") ?? false)
                })
                .ToList();

            if (traitResponses.Any())
            {
                var traitGroups = traitResponses
                    .SelectMany(r => r.Tags.Select(t => new { Tag = t, Response = r }))
                    .GroupBy(x => x.Tag)
                    .Where(g => g.Count() > 1)
                    .ToList();

                var consistencyScores = new List<double>();
                foreach (var group in traitGroups)
                {
                    var responses = group.Select(x => x.Response).ToList();
                    for (int i = 0; i < responses.Count - 1; i++)
                    {
                        for (int j = i + 1; j < responses.Count; j++)
                        {
                            var a = responses[i];
                            var b = responses[j];
                            var expectedDirection = (a.IsReverse == b.IsReverse) ? 1 : -1;
                            var actualCorrelation = Math.Sign(a.Value - b.Value) * expectedDirection;
                            consistencyScores.Add(actualCorrelation > 0 ? 1.0 : 0.0);
                        }
                    }
                }

                if (consistencyScores.Any())
                {
                    score.ConsistencyIndex = consistencyScores.Average();
                    if (score.ConsistencyIndex < 0.5)
                        warnings.Add("Inconsistent answers detected for similar questions");
                }
            }

            score.Warnings = warnings.ToArray();
            return score;
        }
    }
}