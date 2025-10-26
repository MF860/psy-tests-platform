namespace PsyApi.Services.Scoring.Models
{
    /// <summary>
    /// Analysis of response time patterns for cognitive load assessment
    /// </summary>
    public class ResponseTimePatterns
    {
        /// <summary>
        /// Overall engagement score based on response patterns
        /// </summary>
        public double EngagementScore { get; set; }

        /// <summary>
        /// Measure of how much response times vary
        /// </summary>
        public double TimeVariability { get; set; }

        /// <summary>
        /// Score measuring response thoughtfulness
        /// </summary>
        public double ThoughtfulnessScore { get; set; }

        /// <summary>
        /// Whether cognitive fatigue is detected
        /// </summary>
        public bool HasFatigue { get; set; }
    }
}