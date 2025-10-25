namespace PsyApi.Services.Scoring.Models
{
    /// <summary>
    /// Metrics measuring consistency in responses
    /// </summary>
    public class ConsistencyMetrics
    {
        /// <summary>
        /// Overall consistency score combining internal and temporal
        /// </summary>
        public double OverallConsistency { get; set; }

        /// <summary>
        /// Consistency score across related questions
        /// </summary>
        public double InternalConsistency { get; set; }

        /// <summary>
        /// Consistency score measuring stability over time
        /// </summary>
        public double TemporalConsistency { get; set; }
    }
}