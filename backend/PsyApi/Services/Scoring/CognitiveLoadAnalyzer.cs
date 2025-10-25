using PsyApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PsyApi.Services.Scoring
{
    public class ResponseTimePatterns
    {
        public double TimeVariability { get; set; }
        public bool HasFatigue { get; set; }
        public double ThoughtfulnessScore { get; set; }
        public double EngagementScore { get; set; }
    }

    public class ComplexityMetrics
    {
        public double VocabularyScore { get; set; }
        public double ComplexityScore { get; set; }
    }

    public class ConsistencyMetrics
    {
        public double InternalConsistency { get; set; }
        public double TemporalConsistency { get; set; }
        public double OverallConsistency { get; set; }
    }

    /// <summary>
    /// Analyzes cognitive load and engagement patterns in test responses
    /// </summary>
    public static class CognitiveLoadAnalyzer
    {

    public static CognitiveLoadMetrics AnalyzeCognitiveLoad(IList<SessionItem> items)
    {
        var metrics = new CognitiveLoadMetrics();
        var observations = new List<string>();
        var detailedMetrics = new Dictionary<string, double>();

        // 1. Analyze response time patterns
        var responseTimePatterns = AnalyzeResponseTimes(items);
        metrics.EngagementScore = responseTimePatterns.EngagementScore;
        detailedMetrics["timeVariability"] = responseTimePatterns.TimeVariability;
        detailedMetrics["thoughtfulness"] = responseTimePatterns.ThoughtfulnessScore;

        if (responseTimePatterns.HasFatigue)
            observations.Add("Response pattern suggests cognitive fatigue");

        // 2. Analyze answer complexity
        var complexityMetrics = AnalyzeAnswerComplexity(items);
        detailedMetrics["answerComplexity"] = complexityMetrics.ComplexityScore;
        detailedMetrics["vocabularyRichness"] = complexityMetrics.VocabularyScore;

        if (complexityMetrics.ComplexityScore > 0.7)
            observations.Add("High level of engagement with complex answers");

        // 3. Analyze response consistency
        var consistencyMetrics = AnalyzeConsistency(items);
        metrics.ConsistencyScore = consistencyMetrics.OverallConsistency;
        detailedMetrics["internalConsistency"] = consistencyMetrics.InternalConsistency;
        detailedMetrics["temporalConsistency"] = consistencyMetrics.TemporalConsistency;

        if (consistencyMetrics.OverallConsistency < 0.5)
            observations.Add("Inconsistent response patterns detected");

        // 4. Calculate cognitive load score
        metrics.CognitiveLoadScore = CalculateCognitiveLoadScore(
            responseTimePatterns,
            complexityMetrics,
            consistencyMetrics
        );

        metrics.Observations = observations;
        metrics.DetailedMetrics = detailedMetrics;

        return metrics;
    }

    private static ResponseTimePatterns AnalyzeResponseTimes(IList<SessionItem> items)
    {
        var result = new ResponseTimePatterns();
        var responseTimes = items
            .Where(i => i.ResponseTimeMs.HasValue)
            .Select(i => i.ResponseTimeMs!.Value)
            .ToList();

        if (!responseTimes.Any())
            return result;

        // Calculate time variability (normalized standard deviation)
        var mean = responseTimes.Average();
        var stdDev = Math.Sqrt(responseTimes.Average(t => Math.Pow(t - mean, 2)));
        result.TimeVariability = stdDev / mean;

        // Detect fatigue through response time trends
        var timeSegments = responseTimes
            .Select((t, i) => new { Time = t, Index = i })
            .GroupBy(x => x.Index / 5)  // Group into segments of 5
            .Select(g => g.Average(x => x.Time))
            .ToList();

        if (timeSegments.Count >= 3)
        {
            // Check if later segments show consistently shorter times
            var earlyAvg = timeSegments.Take(timeSegments.Count / 2).Average();
            var lateAvg = timeSegments.Skip(timeSegments.Count / 2).Average();
            result.HasFatigue = lateAvg < earlyAvg * 0.7;
        }

        // Calculate thoughtfulness score based on time spent
        var thoughtfulResponseCount = responseTimes.Count(t => t >= 3000 && t <= 30000);
        result.ThoughtfulnessScore = (double)thoughtfulResponseCount / responseTimes.Count;

        // Overall engagement score
        result.EngagementScore = (result.ThoughtfulnessScore + (1 - result.TimeVariability)) / 2;

        return result;
    }

    private static ComplexityMetrics AnalyzeAnswerComplexity(IList<SessionItem> items)
    {
        var metrics = new ComplexityMetrics();
        var textAnswers = items
            .Where(i => i.Item.Type == "TEXT")
            .Select(i => i.Answer ?? string.Empty)
            .ToList();

        if (!textAnswers.Any())
            return metrics;

        // Analyze vocabulary richness
        var allWords = textAnswers
            .SelectMany(a => a.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .ToList();
        
        var uniqueWords = new HashSet<string>(allWords);
        metrics.VocabularyScore = allWords.Any() ? 
            (double)uniqueWords.Count / allWords.Count : 0;

        // Analyze answer complexity
        var avgWordLength = allWords.Any() ? 
            allWords.Average(w => w.Length) : 0;
        var avgAnswerLength = textAnswers.Average(a => a.Length);
        
        metrics.ComplexityScore = Math.Min(1.0, 
            (avgWordLength / 10.0 + avgAnswerLength / 200.0) / 2);

        return metrics;
    }

    private static ConsistencyMetrics AnalyzeConsistency(IList<SessionItem> items)
    {
        var metrics = new ConsistencyMetrics();
        var likertResponses = items
            .Where(i => i.Item.Type is "LikertAgreement" or "Frequency")
            .OrderBy(i => i.AnsweredAt)
            .ToList();

        if (!likertResponses.Any())
            return metrics;

        // Internal consistency across related questions
        var byDimension = likertResponses
            .GroupBy(i => i.Item.DimensionTags)
            .Where(g => g.Count() > 1);

        var dimensionConsistencies = new List<double>();
        foreach (var group in byDimension)
        {
            var responses = group
                .Select(i => int.TryParse(i.Answer, out var v) ? v : 3)
                .ToList();
            
            if (responses.Count >= 2)
            {
                var variance = responses
                    .Average(r => Math.Pow(r - responses.Average(), 2));
                dimensionConsistencies.Add(1 - Math.Min(1, variance / 4));
            }
        }

        metrics.InternalConsistency = dimensionConsistencies.Any() ? 
            dimensionConsistencies.Average() : 0;

        // Temporal consistency (response pattern stability over time)
        var timeSegments = likertResponses
            .Select((item, index) => new { 
                Response = int.TryParse(item.Answer, out var v) ? v : 3,
                Index = index
            })
            .GroupBy(x => x.Index / 5)
            .Select(g => g.Average(x => x.Response))
            .ToList();

        if (timeSegments.Count >= 2)
        {
            var segmentVariance = timeSegments
                .Average(s => Math.Pow(s - timeSegments.Average(), 2));
            metrics.TemporalConsistency = 1 - Math.Min(1, segmentVariance / 4);
        }

        metrics.OverallConsistency = (metrics.InternalConsistency + metrics.TemporalConsistency) / 2;
        return metrics;
    }

    private static double CalculateCognitiveLoadScore(
        ResponseTimePatterns timePatterns,
        ComplexityMetrics complexity,
        ConsistencyMetrics consistency)
    {
        var weights = new Dictionary<string, double>
        {
            { "engagement", 0.3 },
            { "complexity", 0.3 },
            { "consistency", 0.4 }
        };

        return (timePatterns.EngagementScore * weights["engagement"]) +
               (complexity.ComplexityScore * weights["complexity"]) +
               (consistency.OverallConsistency * weights["consistency"]);
        }
    }
}