using PsyApi.Models;

namespace PsyApi.Services.AI
{
    /// <summary>
    /// AI Analyzer Service using OpenRouter/DeepSeek with fallback to mock data
    /// </summary>
    public class AiAnalyzerService : IAiAnalyzerService
    {
        private readonly IOpenRouterClient _openRouterClient;
        private readonly ILogger<AiAnalyzerService> _logger;

        public AiAnalyzerService(IOpenRouterClient openRouterClient, ILogger<AiAnalyzerService> logger)
        {
            _openRouterClient = openRouterClient;
            _logger = logger;
        }

        public async Task<AiAnalysisResponse> AnalyzeAsync(Result result, IEnumerable<DimensionScore> dimensions, CancellationToken ct = default)
        {
            try 
            {
                _logger.LogInformation("[AI] Starting analysis for resultId={ResultId}", result.Id);
                return await _openRouterClient.AnalyzeAsync(result, dimensions, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AI] OpenRouter analysis failed for resultId={ResultId}, falling back to mock", result.Id);
                
                // Fallback to mock analysis
                var mockAnalysis = GenerateFallbackAnalysis(result, dimensions);
                return new AiAnalysisResponse
                {
                    Analysis = mockAnalysis,
                    Model = "fallback-mock",
                    Usage = null,
                    GeneratedAt = DateTime.UtcNow
                };
            }
        }

        /// <summary>
        /// Generate basic fallback analysis when OpenRouter is unavailable
        /// </summary>
        private static AiAnalysisResult GenerateFallbackAnalysis(Result result, IEnumerable<DimensionScore> dimensions)
        {
            var dims = dimensions?.ToList() ?? new List<DimensionScore>();
            var strengths = dims.Where(d => d.T >= 60).Select(d => $"قوة في {d.Dimension}").ToList();
            var weaknesses = dims.Where(d => d.T < 40).Select(d => $"مجال للتطوير في {d.Dimension}").ToList();

            var recs = new List<string>();
            if (strengths.Any()) 
                recs.Add("ركز على تطوير نقاط القوة هذه في مجالات حياتك المختلفة.");
            if (weaknesses.Any()) 
                recs.Add("اعمل على تحسين مجالات الضعف من خلال التدريب المناسب.");
            recs.Add("استشر متخصصاً للحصول على خطة علاجية متكاملة.");

            // Ensure we have at least some content
            if (!strengths.Any()) 
                strengths.Add("نتائج متوازنة بشكل عام");
            if (!weaknesses.Any()) 
                weaknesses.Add("لا توجد مجالات ضعف واضحة");
            if (!recs.Any())
                recs.Add("مراجعة النتائج مع متخصص لوضع خطة تطوير مناسبة");

            return new AiAnalysisResult
            {
                Strengths = strengths.Take(6).ToList(),
                Weaknesses = weaknesses.Take(6).ToList(), 
                Recommendations = recs.Take(6).ToList(),
                Rationale = "تحليل أساسي مبني على النتائج الإحصائية (خدمة الذكاء الاصطناعي غير متوفرة)"
            };
        }
    }
}
