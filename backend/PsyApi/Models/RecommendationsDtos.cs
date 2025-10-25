using System.ComponentModel.DataAnnotations;

namespace PsyApi.Models
{
    /// <summary>
    /// Request to get AI-powered recommendations for a specific result
    /// </summary>
    public class GetRecommendationsRequest
    {
        [Required]
        public int ResultId { get; set; }
        
        /// <summary>
        /// Optional context for more personalized recommendations
        /// </summary>
        public string? Context { get; set; }
        
        /// <summary>
        /// Language preference for recommendations (default: Arabic)
        /// </summary>
        public string Language { get; set; } = "ar";
        
        /// <summary>
        /// Force regeneration of recommendations (bypass cache)
        /// </summary>
        public bool ForceRegenerate { get; set; } = false;
    }

    /// <summary>
    /// Complete recommendations response with AI-generated insights
    /// </summary>
    public class RecommendationsResponse
    {
        public int ResultId { get; set; }
        public string ParticipantId { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public string ModelVersion { get; set; } = string.Empty;
        public bool FromCache { get; set; } = false;
        
        /// <summary>
        /// Overall performance summary
        /// </summary>
        public OverallSummary Summary { get; set; } = new();
        
        /// <summary>
        /// Top strengths identified from high-scoring dimensions
        /// </summary>
        public List<StrengthInsight> Strengths { get; set; } = new();
        
        /// <summary>
        /// Areas that need development (low-scoring dimensions)
        /// </summary>
        public List<GrowthArea> GrowthAreas { get; set; } = new();
        
        /// <summary>
        /// Personalized development recommendations
        /// </summary>
        public List<DevelopmentRecommendation> Recommendations { get; set; } = new();
        
        /// <summary>
        /// Suggested courses or training programs
        /// </summary>
        public List<CourseRecommendation> Courses { get; set; } = new();
    }

    /// <summary>
    /// Overall performance summary with key metrics
    /// </summary>
    public class OverallSummary
    {
        public string ProfileType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double OverallScore { get; set; }
        public string PerformanceLevel { get; set; } = string.Empty;
        public List<string> KeyCharacteristics { get; set; } = new();
    }

    /// <summary>
    /// Strength insight for high-performing dimensions
    /// </summary>
    public class StrengthInsight
    {
        public string Dimension { get; set; } = string.Empty;
        public double TScore { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Applications { get; set; } = new();
        public string ReinforcementTip { get; set; } = string.Empty;
    }

    /// <summary>
    /// Growth area for dimensions that need development
    /// </summary>
    public class GrowthArea
    {
        public string Dimension { get; set; } = string.Empty;
        public double TScore { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Challenge { get; set; } = string.Empty;
        public List<string> ImprovementStrategies { get; set; } = new();
        public string Priority { get; set; } = "Medium"; // Low, Medium, High
    }

    /// <summary>
    /// Personalized development recommendation
    /// </summary>
    public class DevelopmentRecommendation
    {
        public string Category { get; set; } = string.Empty; // Leadership, Communication, Technical, etc.
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> ActionSteps { get; set; } = new();
        public string Timeline { get; set; } = string.Empty; // "1-3 months", "3-6 months", etc.
        public string Priority { get; set; } = "Medium";
        public List<string> ExpectedOutcomes { get; set; } = new();
    }

    /// <summary>
    /// Course or training program recommendation
    /// </summary>
    public class CourseRecommendation
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty; // Beginner, Intermediate, Advanced
        public List<string> Topics { get; set; } = new();
        public string RelevancyReason { get; set; } = string.Empty;
        public string? ExternalUrl { get; set; }
    }

    /// <summary>
    /// Cache entry for storing recommendations with metadata
    /// </summary>
    public class RecommendationsCacheEntry
    {
        public int ResultId { get; set; }
        public RecommendationsResponse Data { get; set; } = new();
        public DateTime CachedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string ModelVersion { get; set; } = string.Empty;
        public int TokensUsed { get; set; }
        public TimeSpan GenerationTime { get; set; }
    }

    /// <summary>
    /// Error response for failed recommendation generation
    /// </summary>
    public class RecommendationsErrorResponse
    {
        public string Error { get; set; } = string.Empty;
        public string ErrorCode { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime ErrorAt { get; set; }
        public bool CanRetry { get; set; } = true;
        public int? RetryAfterSeconds { get; set; }
    }

    /// <summary>
    /// OpenAI API configuration and usage tracking
    /// </summary>
    public class OpenAIUsageMetrics
    {
        public int RequestsToday { get; set; }
        public int TokensUsedToday { get; set; }
        public double CostToday { get; set; }
        public int RequestsThisMonth { get; set; }
        public int TokensUsedThisMonth { get; set; }
        public double CostThisMonth { get; set; }
        public DateTime LastRequest { get; set; }
        public double AverageResponseTime { get; set; }
        public int CacheHitRate { get; set; } // Percentage
    }
}