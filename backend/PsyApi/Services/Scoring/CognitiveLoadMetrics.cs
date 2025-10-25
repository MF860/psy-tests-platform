using System.Collections.Generic;

namespace PsyApi.Services.Scoring
{
    public class CognitiveLoadMetrics
    {
        public double CognitiveLoadScore { get; set; }
        public double EngagementScore { get; set; }
        public double ConsistencyScore { get; set; }
        public List<string> Observations { get; set; } = new();
        public Dictionary<string, double> DetailedMetrics { get; set; } = new();
    }
}