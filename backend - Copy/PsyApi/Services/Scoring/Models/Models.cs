using System.Text.Json.Serialization;

namespace PsyApi.Services.Scoring.Models
{
    public class DimensionScore
    {
        public string Dimension { get; set; } = string.Empty;
        public double Raw { get; set; }
        public double Z { get; set; }
        public double T { get; set; }
        public double Percentile { get; set; }
    }

    public class ScoreSummary
    {
        public List<DimensionScore> DimensionScores { get; set; } = new();
        public double TotalScore { get; set; }
        public ResponseValidityScore Validity { get; set; } = new();
        public Dictionary<string, double> CognitiveLoadMetrics { get; set; } = new();
        public List<string> CognitiveLoadObservations { get; set; } = new();
        public Dictionary<string, double> DetailedMetrics { get; set; } = new();
        public string Version { get; set; } = "v2.0";

        [JsonIgnore]
        public virtual bool IsEnhanced => false;
    }

    public class ResponseValidityScore
    {
        public double ConsistencyIndex { get; set; }  // 0-1: How consistent are answers to similar questions
        public double EngagementIndex { get; set; }   // 0-1: Based on response times and patterns
        public double RandomnessIndex { get; set; }   // 0-1: Likelihood of non-random responding
        public bool IsValid => ConsistencyIndex >= 0.5 && EngagementIndex >= 0.4 && RandomnessIndex >= 0.6;
        public string[] Warnings { get; set; } = Array.Empty<string>();
    }

    public class TraitScore
    {
        public string Dimension { get; set; } = string.Empty;
        public double Value { get; set; }
        public double ConfidenceLevel { get; set; }
        public string[] SupportingEvidence { get; set; } = Array.Empty<string>();
        public string[] ConflictingEvidence { get; set; } = Array.Empty<string>();
    }

    public class PersonalityAssessment
    {
        public ResponseValidityScore Validity { get; set; } = new();
        public List<TraitScore> Traits { get; set; } = new();
        public List<string> QualitativeInsights { get; set; } = new();
        public double OverallConfidence { get; set; }
    }

    public class EnhancedScoreSummary : ScoreSummary
    {
        public PersonalityAssessment? PersonalityProfile { get; set; }

        [JsonIgnore]
        public override bool IsEnhanced => true;
    }
}