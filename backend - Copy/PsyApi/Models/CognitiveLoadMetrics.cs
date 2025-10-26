namespace PsyApi.Models
{
    /// <summary>
    /// Contains metrics related to cognitive load analysis of test responses
    /// </summary>
    public class CognitiveLoadMetrics
    {
        /// <summary>
        /// Measures how consistent the responses are across related questions
        /// </summary>
        public double ConsistencyScore { get; set; }

        /// <summary>
        /// Measures how engaged the user was during the test
        /// </summary>
        public double EngagementScore { get; set; }

        /// <summary>
        /// Overall cognitive load score based on multiple factors
        /// </summary>
        public double CognitiveLoadScore { get; set; }

        /// <summary>
        /// List of notable observations about response patterns
        /// </summary>
        public List<string> Observations { get; set; } = new();

        /// <summary>
        /// Detailed breakdown of various metrics used in analysis
        /// </summary>
        public Dictionary<string, double> DetailedMetrics { get; set; } = new();
    }
}