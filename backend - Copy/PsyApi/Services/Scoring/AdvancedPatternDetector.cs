using System.Text.RegularExpressions;
using PsyApi.Models;

namespace PsyApi.Services.Scoring;

public class AdvancedPatternDetector
{
    // Time thresholds for suspicious patterns
    private const int MinThoughtfulResponseMs = 1500;  // 1.5 seconds minimum for real thought
    private const int MaxReasonableResponseMs = 300000; // 5 minutes max for one question
    private const int FastPatternThresholdMs = 2000;   // Pattern of responses under 2 seconds is suspicious
    
    // Sequence patterns that might indicate non-genuine responses
    private static readonly int[][] SuspiciousPatterns = new[]
    {
        new[] { 1,2,3,4,5 },    // Sequential
        new[] { 5,4,3,2,1 },    // Reverse sequential
        new[] { 1,1,1,1,1 },    // All same
        new[] { 5,5,5,5,5 },    // All same extreme
        new[] { 1,5,1,5,1 },    // Alternating extremes
        new[] { 3,3,3,3,3 }     // All neutral
    };

    public class PatternAnalysisResult
    {
        public bool HasTimingAnomalies { get; set; }
        public bool HasResponsePatterns { get; set; }
        public bool HasStyleInconsistencies { get; set; }
        public bool HasContextualContradictions { get; set; }
        public double AnomalyScore { get; set; }
        public List<string> DetailedFindings { get; set; } = new();
        public Dictionary<string, double> StyleMetrics { get; set; } = new();
    }

    public static PatternAnalysisResult AnalyzeResponsePatterns(IList<SessionItem> items)
    {
        var result = new PatternAnalysisResult();
        var findings = new List<string>();
        
        // 1. Timing Analysis
        var responseTimes = items
            .Where(i => i.ResponseTimeMs.HasValue)
            .Select(i => (long)i.ResponseTimeMs!.Value)
            .ToList();

        if (responseTimes.Any())
        {
            var tooFastCount = responseTimes.Count(t => t < (long)MinThoughtfulResponseMs);
            var tooSlowCount = responseTimes.Count(t => t > (long)MaxReasonableResponseMs);
            var fastSequences = CountConsecutiveFastResponses(responseTimes);

            if (tooFastCount > responseTimes.Count * 0.2)
            {
                findings.Add($"Suspicious: {tooFastCount} responses were unusually fast (< {MinThoughtfulResponseMs}ms)");
                result.HasTimingAnomalies = true;
            }

            if (fastSequences > 3)
            {
                findings.Add($"Detected {fastSequences} sequences of rapid responses - possible automatic clicking");
                result.HasTimingAnomalies = true;
            }
        }

        // 2. Response Pattern Analysis
        var likertResponses = items
            .Where(i => i.Item.Type is "LikertAgreement" or "Frequency")
            .Select(i => int.TryParse(i.Answer, out var v) ? v : 0)
            .ToList();

        if (likertResponses.Count >= 5)
        {
            foreach (var pattern in SuspiciousPatterns)
            {
                for (int i = 0; i <= likertResponses.Count - pattern.Length; i++)
                {
                    var segment = likertResponses.Skip(i).Take(pattern.Length).ToArray();
                    if (segment.SequenceEqual(pattern))
                    {
                        findings.Add($"Found suspicious response pattern at position {i + 1}");
                        result.HasResponsePatterns = true;
                        break;
                    }
                }
            }
        }

        // 3. Response Style Analysis
        var styleMetrics = AnalyzeResponseStyle(items);
        result.StyleMetrics = styleMetrics;

        if (styleMetrics["extremityBias"] > 0.7)
        {
            findings.Add("High tendency toward extreme responses");
            result.HasStyleInconsistencies = true;
        }

        if (styleMetrics["neutralBias"] > 0.7)
        {
            findings.Add("Strong bias toward neutral responses");
            result.HasStyleInconsistencies = true;
        }

        // 4. Contextual Contradiction Analysis
        var contradictions = FindContextualContradictions(items);
        foreach (var contradiction in contradictions)
        {
            findings.Add($"Contextual contradiction: {contradiction}");
            result.HasContextualContradictions = true;
        }

        // Calculate overall anomaly score
        result.AnomalyScore = CalculateAnomalyScore(
            result.HasTimingAnomalies,
            result.HasResponsePatterns,
            result.HasStyleInconsistencies,
            result.HasContextualContradictions,
            styleMetrics
        );

        result.DetailedFindings = findings;
        return result;
    }

    private static int CountConsecutiveFastResponses(List<long> responseTimes)
    {
        int maxSequence = 0;
        int currentSequence = 0;

        foreach (var time in responseTimes)
        {
            if (time < FastPatternThresholdMs)
            {
                currentSequence++;
                maxSequence = Math.Max(maxSequence, currentSequence);
            }
            else
            {
                currentSequence = 0;
            }
        }

        return maxSequence;
    }

    private static Dictionary<string, double> AnalyzeResponseStyle(IList<SessionItem> items)
    {
        var metrics = new Dictionary<string, double>();
        var likertResponses = items
            .Where(i => i.Item.Type is "LikertAgreement" or "Frequency")
            .Select(i => int.TryParse(i.Answer, out var v) ? v : 0)
            .Where(v => v > 0)
            .ToList();

        if (likertResponses.Any())
        {
            // Extremity bias (preference for 1s and 5s)
            var extremeResponses = likertResponses.Count(r => r == 1 || r == 5);
            metrics["extremityBias"] = (double)extremeResponses / likertResponses.Count;

            // Neutral bias (preference for 3s)
            var neutralResponses = likertResponses.Count(r => r == 3);
            metrics["neutralBias"] = (double)neutralResponses / likertResponses.Count;

            // Response variance
            var mean = likertResponses.Average();
            var variance = likertResponses.Average(r => Math.Pow(r - mean, 2));
            metrics["responseVariance"] = variance;

            // First vs Second half consistency
            var midpoint = likertResponses.Count / 2;
            var firstHalf = likertResponses.Take(midpoint);
            var secondHalf = likertResponses.Skip(midpoint);
            metrics["halfConsistency"] = 1 - Math.Abs(firstHalf.Average() - secondHalf.Average()) / 4.0;
        }

        return metrics;
    }

    private static List<string> FindContextualContradictions(IList<SessionItem> items)
    {
        var contradictions = new List<string>();
        var likertItems = items
            .Where(i => i.Item.Type is "LikertAgreement" or "Frequency")
            .Select(i => new
            {
                Question = i.Item.TextAr,
                Value = int.TryParse(i.Answer, out var v) ? v : 3,
                Tags = i.Item.DimensionTags?.Split(',').Select(t => t.Trim()).ToList() ?? new List<string>()
            })
            .ToList();

        // Group by dimension and check for contradictions
        var byDimension = likertItems
            .SelectMany(i => i.Tags.Select(t => new { Tag = t, Item = i }))
            .GroupBy(x => x.Tag)
            .Where(g => g.Count() > 1);

        foreach (var group in byDimension)
        {
            var groupItems = group.Select(x => x.Item).ToList();
            for (int i = 0; i < groupItems.Count - 1; i++)
            {
                for (int j = i + 1; j < groupItems.Count; j++)
                {
                    var a = groupItems[i];
                    var b = groupItems[j];
                    
                    // Check for semantic opposition
                    var isOpposite = IsSemanticOpposition(a.Question!, b.Question!);
                    
                    if (isOpposite && Math.Abs(a.Value - b.Value) < 2)
                    {
                        contradictions.Add(
                            $"Dimension '{group.Key}': Similar responses ({a.Value} vs {b.Value}) " +
                            $"for opposing statements"
                        );
                    }
                    else if (!isOpposite && Math.Abs(a.Value - b.Value) > 3)
                    {
                        contradictions.Add(
                            $"Dimension '{group.Key}': Large disparity ({a.Value} vs {b.Value}) " +
                            $"for similar statements"
                        );
                    }
                }
            }
        }

        return contradictions;
    }

    private static bool IsSemanticOpposition(string q1, string q2)
    {
        var negationMarkers = new[] { "لا ", "عدم ", "غير ", "ليس " };
        var hasNegation = (string s) => negationMarkers.Any(m => s.Contains(m));
        
        // If one has negation and the other doesn't
        if (hasNegation(q1) ^ hasNegation(q2))
        {
            // Remove negation markers and compare remaining text
            var clean1 = negationMarkers.Aggregate(q1, (s, m) => s.Replace(m, "")).Trim();
            var clean2 = negationMarkers.Aggregate(q2, (s, m) => s.Replace(m, "")).Trim();
            
            // Calculate similarity ratio
            return CalculateSimilarity(clean1, clean2) > 0.7;
        }
        
        return false;
    }

    private static double CalculateSimilarity(string s1, string s2)
    {
        var words1 = new HashSet<string>(s1.Split(' '));
        var words2 = new HashSet<string>(s2.Split(' '));
        
        var intersection = words1.Intersect(words2).Count();
        var union = words1.Union(words2).Count();
        
        return union == 0 ? 0 : (double)intersection / union;
    }

    private static double CalculateAnomalyScore(
        bool hasTimingAnomalies,
        bool hasPatterns,
        bool hasStyleIssues,
        bool hasContradictions,
        Dictionary<string, double> metrics)
    {
        double score = 0;
        int factors = 0;

        // Timing anomalies (30% weight)
        if (hasTimingAnomalies)
        {
            score += 0.3;
            factors++;
        }

        // Response patterns (25% weight)
        if (hasPatterns)
        {
            score += 0.25;
            factors++;
        }

        // Style inconsistencies (25% weight)
        if (hasStyleIssues)
        {
            score += 0.25;
            factors++;
        }

        // Contextual contradictions (20% weight)
        if (hasContradictions)
        {
            score += 0.2;
            factors++;
        }

        // Add influence from style metrics
        if (metrics.ContainsKey("extremityBias"))
        {
            score += metrics["extremityBias"] * 0.15;
            factors++;
        }

        if (metrics.ContainsKey("neutralBias"))
        {
            score += metrics["neutralBias"] * 0.15;
            factors++;
        }

        // Normalize to [0,1]
        return factors > 0 ? score / factors : 0;
    }
}