namespace PsyApi.Services.Scoring.Models
{
    /// <summary>
    /// Metrics related to answer complexity analysis
    /// </summary>
    public class ComplexityMetrics
    {
        /// <summary>
        /// Overall complexity score of responses
        /// </summary>
        public double ComplexityScore { get; set; }

        /// <summary>
        /// Score measuring vocabulary diversity
        /// </summary>
        public double VocabularyScore { get; set; }
    }
}