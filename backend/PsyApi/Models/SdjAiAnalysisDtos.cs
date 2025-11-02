using System.Text.Json.Serialization;

namespace PsyApi.Models
{
    /// <summary>
    /// Request to analyze SDJ results with AI
    /// </summary>
    public class SdjAnalyzeRequest
    {
        [JsonPropertyName("resultId")]
        public int ResultId { get; set; }
        
        [JsonPropertyName("language")]
        public string Language { get; set; } = "ar";
    }

    /// <summary>
    /// Complete SDJ-7 AI Analysis Response
    /// Aligns with frontend contract for pattern cards + charts
    /// </summary>
    public class SdjAiAnalysisResponse
    {
        [JsonPropertyName("summary")]
        public string Summary { get; set; } = string.Empty;

        [JsonPropertyName("patterns")]
        public List<SdjPatternAnalysis> Patterns { get; set; } = new();

        [JsonPropertyName("subDimensions")]
        public List<SdjSubDimensionAnalysis> SubDimensions { get; set; } = new();

        [JsonPropertyName("charts")]
        public SdjChartData Charts { get; set; } = new();

        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("usage")]
        public SdjAiUsage? Usage { get; set; }

        [JsonPropertyName("generatedAt")]
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Analysis for one of the 7 SDJ patterns
    /// </summary>
    public class SdjPatternAnalysis
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        [JsonPropertyName("tScore")]
        public double TScore { get; set; }

        [JsonPropertyName("band")]
        public string Band { get; set; } = string.Empty; // "ضعيف", "متوسط", "قوي"

        [JsonPropertyName("insights")]
        public List<string> Insights { get; set; } = new();

        [JsonPropertyName("risks")]
        public List<string> Risks { get; set; } = new();

        [JsonPropertyName("recommendations")]
        public List<string> Recommendations { get; set; } = new();
    }

    /// <summary>
    /// Analysis for a sub-dimension
    /// </summary>
    public class SdjSubDimensionAnalysis
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        [JsonPropertyName("patternKey")]
        public string PatternKey { get; set; } = string.Empty;

        [JsonPropertyName("tScore")]
        public double TScore { get; set; }

        [JsonPropertyName("band")]
        public string Band { get; set; } = string.Empty;

        [JsonPropertyName("comment")]
        public string Comment { get; set; } = string.Empty;
    }

    /// <summary>
    /// Chart-ready data for visualization
    /// </summary>
    public class SdjChartData
    {
        [JsonPropertyName("radar")]
        public SdjRadarChart Radar { get; set; } = new();

        [JsonPropertyName("bars")]
        public SdjBarChart Bars { get; set; } = new();
    }

    /// <summary>
    /// Radar chart data for 7 patterns
    /// </summary>
    public class SdjRadarChart
    {
        [JsonPropertyName("series")]
        public List<SdjRadarSeries> Series { get; set; } = new();

        [JsonPropertyName("labels")]
        public List<string> Labels { get; set; } = new();
    }

    public class SdjRadarSeries
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "T";

        [JsonPropertyName("data")]
        public List<double> Data { get; set; } = new();
    }

    /// <summary>
    /// Bar chart data for sub-dimensions
    /// </summary>
    public class SdjBarChart
    {
        [JsonPropertyName("data")]
        public List<SdjBarItem> Data { get; set; } = new();
    }

    public class SdjBarItem
    {
        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        [JsonPropertyName("t")]
        public double T { get; set; }
    }

    /// <summary>
    /// Usage metrics for AI analysis
    /// </summary>
    public class SdjAiUsage
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
    /// Internal: Compact data payload sent to DeepSeek for AI analysis
    /// Privacy-aware, no PII
    /// </summary>
    public class SdjAnalysisPayload
    {
        [JsonPropertyName("resultId")]
        public int ResultId { get; set; }

        [JsonPropertyName("language")]
        public string Language { get; set; } = "ar";

        [JsonPropertyName("sdj")]
        public SdjAiPayloadData Sdj { get; set; } = new();

        [JsonPropertyName("items")]
        public List<SdjAiItemPayload> Items { get; set; } = new();
    }

    public class SdjAiPayloadData
    {
        [JsonPropertyName("patterns")]
        public List<SdjAiPatternPayload> Patterns { get; set; } = new();

        [JsonPropertyName("subDimensions")]
        public List<SdjAiSubDimensionPayload> SubDimensions { get; set; } = new();
    }

    public class SdjAiPatternPayload
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        [JsonPropertyName("tScore")]
        public double TScore { get; set; }
    }

    public class SdjAiSubDimensionPayload
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;

        [JsonPropertyName("patternKey")]
        public string PatternKey { get; set; } = string.Empty;

        [JsonPropertyName("tScore")]
        public double TScore { get; set; }
    }

    public class SdjAiItemPayload
    {
        [JsonPropertyName("itemCode")]
        public string ItemCode { get; set; } = string.Empty;

        [JsonPropertyName("textAr")]
        public string TextAr { get; set; } = string.Empty;

        [JsonPropertyName("dimensionKey")]
        public string DimensionKey { get; set; } = string.Empty;

        [JsonPropertyName("subDimensionKey")]
        public string SubDimensionKey { get; set; } = string.Empty;

        [JsonPropertyName("isReverse")]
        public bool IsReverse { get; set; }

        [JsonPropertyName("answer")]
        public int Answer { get; set; }
    }
}
