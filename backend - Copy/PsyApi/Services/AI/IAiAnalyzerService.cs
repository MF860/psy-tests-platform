using System.Text.Json.Serialization;

namespace PsyApi.Services.AI
{
    /// <summary>
    /// Category analysis for SDJ dimensions
    /// </summary>
    public class CategoryAnalysis
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("t")]
        public double TScore { get; set; }
        
        [JsonPropertyName("note")]
        public string Note { get; set; } = string.Empty;
    }

    /// <summary>
    /// AI Analysis result matching frontend AiAnalysis contract
    /// Enhanced for SDJ with categories, summary, and methodology
    /// </summary>
    public class AiAnalysisResult
    {
        [JsonPropertyName("strengths")]
        public List<string> Strengths { get; set; } = new();
        
        [JsonPropertyName("weaknesses")]
        public List<string> Weaknesses { get; set; } = new();
        
        [JsonPropertyName("recommendations")]
        public List<string> Recommendations { get; set; } = new();
        
        [JsonPropertyName("rationale")]
        public string? Rationale { get; set; }
        
        [JsonPropertyName("summary")]
        public string? Summary { get; set; }
        
        [JsonPropertyName("methodology")]
        public string? Methodology { get; set; }
        
        [JsonPropertyName("categories")]
        public List<CategoryAnalysis> Categories { get; set; } = new();
    }

    /// <summary>
    /// Usage metadata returned with analysis
    /// </summary>
    public class AiAnalysisUsage
    {
        [JsonPropertyName("promptTokens")]
        public int PromptTokens { get; set; }
        
        [JsonPropertyName("completionTokens")]
        public int CompletionTokens { get; set; }
        
        [JsonPropertyName("totalTokens")]
        public int TotalTokens { get; set; }
        
        [JsonPropertyName("latencyMs")]
        public double LatencyMs { get; set; }
    }

    /// <summary>
    /// Complete AI analysis response with metadata
    /// </summary>
    public class AiAnalysisResponse
    {
        [JsonPropertyName("analysis")]
        public AiAnalysisResult Analysis { get; set; } = new();
        
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;
        
        [JsonPropertyName("usage")]
        public AiAnalysisUsage? Usage { get; set; }
        
        [JsonPropertyName("generatedAt")]
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    public interface IAiAnalyzerService
    {
        Task<AiAnalysisResponse> AnalyzeAsync(PsyApi.Models.Result result, IEnumerable<PsyApi.Models.DimensionScore> dimensions, CancellationToken ct = default);
    }
}
