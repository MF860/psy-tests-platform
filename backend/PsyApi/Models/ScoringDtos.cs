using System.Collections.Generic;

namespace PsyApi.Models
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
        public List<DimensionScore> Dimensions { get; set; } = new();
        public double? TotalScore { get; set; }
        public string Version { get; set; } = string.Empty;
    }
}

