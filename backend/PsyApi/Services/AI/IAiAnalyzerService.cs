namespace PsyApi.Services.AI
{
    /// <summary>
    /// AI Analysis result matching frontend AiAnalysis contract
    /// </summary>
    public class AiAnalysisResult
    {
        public List<string> Strengths { get; set; } = new();
        public List<string> Weaknesses { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        public string? Rationale { get; set; }
    }

    /// <summary>
    /// Usage metadata returned with analysis
    /// </summary>
    public class AiAnalysisUsage
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
        public double LatencyMs { get; set; }
    }

    /// <summary>
    /// Complete AI analysis response with metadata
    /// </summary>
    public class AiAnalysisResponse
    {
        public AiAnalysisResult Analysis { get; set; } = new();
        public string Model { get; set; } = string.Empty;
        public AiAnalysisUsage? Usage { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    public interface IAiAnalyzerService
    {
        Task<AiAnalysisResponse> AnalyzeAsync(PsyApi.Models.Result result, IEnumerable<PsyApi.Models.DimensionScore> dimensions, CancellationToken ct = default);
    }
}
