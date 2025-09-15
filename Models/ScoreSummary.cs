namespace psy_tests_platform.Models
{
    public class ScoreSummary
    {
        public List<DimensionScore> DimensionScores { get; set; } = new();
        public TotalScore? TotalScore { get; set; }
        public string Version { get; set; } = "v1.0";
    }

    public class DimensionScore
    {
        public string Dimension { get; set; } = string.Empty;
        public double Raw { get; set; }
        public double Z { get; set; }
        public double T { get; set; }
        public double Percentile { get; set; }
    }

    public class TotalScore
    {
        public double Raw { get; set; }
        public double MaxPossible { get; set; }
        public double Percentage { get; set; }
    }
}
