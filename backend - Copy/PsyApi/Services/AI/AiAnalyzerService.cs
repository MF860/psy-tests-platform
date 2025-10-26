using PsyApi.Models;

namespace PsyApi.Services.AI
{
    /// <summary>
    /// AI Analyzer Service using DeepSeek with fallback to rule-based analysis
    /// </summary>
    public class AiAnalyzerService : IAiAnalyzerService
    {
        private readonly IDeepSeekClient _deepSeekClient;
        private readonly ILogger<AiAnalyzerService> _logger;

        public AiAnalyzerService(IDeepSeekClient deepSeekClient, ILogger<AiAnalyzerService> logger)
        {
            _deepSeekClient = deepSeekClient;
            _logger = logger;
        }

        public async Task<AiAnalysisResponse> AnalyzeAsync(Result result, IEnumerable<DimensionScore> dimensions, CancellationToken ct = default)
        {
            try 
            {
                _logger.LogInformation("[AI] Starting DeepSeek analysis for resultId={ResultId}", result.Id);
                return await _deepSeekClient.AnalyzeAsync(result, dimensions, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AI] DeepSeek analysis failed for resultId={ResultId}, falling back to rule-based", result.Id);
                
                // Fallback to rule-based analysis
                var fallbackAnalysis = GenerateFallbackAnalysis(result, dimensions);
                return new AiAnalysisResponse
                {
                    Analysis = fallbackAnalysis,
                    Model = "fallback-sdj-rules",
                    Usage = null,
                    GeneratedAt = DateTime.UtcNow
                };
            }
        }

        /// <summary>
        /// Generate rule-based SDJ analysis when DeepSeek is unavailable
        /// </summary>
        private static AiAnalysisResult GenerateFallbackAnalysis(Result result, IEnumerable<DimensionScore> dimensions)
        {
            var dims = dimensions?.ToList() ?? new List<DimensionScore>();
            
            // Strengths: T >= 55
            var strengths = dims.Where(d => d.T >= 55)
                .OrderByDescending(d => d.T)
                .Take(5)
                .Select(d => $"أداء ممتاز في {d.Dimension} (T={d.T:F1})")
                .ToList();
            
            // Weaknesses: T < 45
            var weaknesses = dims.Where(d => d.T < 45)
                .OrderBy(d => d.T)
                .Take(5)
                .Select(d => $"يحتاج إلى تطوير في {d.Dimension} (T={d.T:F1})")
                .ToList();

            var recs = new List<string>();
            if (strengths.Any())
            {
                recs.Add("استثمر في نقاط القوة من خلال التخصص والتركيز على المجالات التي تتفوق فيها");
                recs.Add("شارك خبراتك مع الآخرين في مجالات قوتك لتعزيز الثقة والنمو المهني");
            }
            if (weaknesses.Any())
            {
                recs.Add("ضع خطة تطوير شخصية تركز على تحسين المجالات الأقل أداءً");
                recs.Add("اطلب الدعم أو التدريب في المهارات التي تحتاج إلى تطوير");
            }
            recs.Add("راجع النتائج مع مستشار مهني لوضع خطة تنموية متكاملة");
            recs.Add("حدد أهدافاً قابلة للقياس خلال 4-6 أسابيع وتابع التقدم بانتظام");

            // Ensure minimum content
            if (!strengths.Any()) 
                strengths.Add("نتائج متوازنة بشكل عام في معظم الأبعاد");
            if (!weaknesses.Any()) 
                weaknesses.Add("لا توجد مجالات ضعف واضحة تتطلب تدخلاً فورياً");

            // Build categories summary
            var categories = dims.OrderByDescending(d => d.T)
                .Select(d => new CategoryAnalysis
                {
                    Name = d.Dimension,
                    TScore = d.T,
                    Note = GetCategoryNote(d.T)
                })
                .ToList();

            var avgT = dims.Any() ? dims.Average(d => d.T) : 50.0;
            var summary = avgT >= 55 
                ? $"النتائج تظهر أداءً جيداً بمتوسط T-Score قدره {avgT:F1}، مع تفوق واضح في عدة مجالات"
                : avgT >= 45
                ? $"النتائج تظهر أداءً متوسطاً بمتوسط T-Score قدره {avgT:F1}، مع فرص للتطوير في بعض المجالات"
                : $"النتائج تظهر حاجة إلى تطوير شامل بمتوسط T-Score قدره {avgT:F1}، يُنصح بخطة تدخل منهجية";

            return new AiAnalysisResult
            {
                Strengths = strengths.Take(6).ToList(),
                Weaknesses = weaknesses.Take(6).ToList(), 
                Recommendations = recs.Take(6).ToList(),
                Summary = summary,
                Methodology = "تحليل مبني على قواعد SDJ الإحصائية (خدمة الذكاء الاصطناعي غير متوفرة حالياً)",
                Rationale = "التحليل مستند إلى معايير T-Score حيث القيم أعلى من 55 تمثل نقاط قوة والقيم أقل من 45 تمثل مجالات تحتاج تطوير",
                Categories = categories
            };
        }

        private static string GetCategoryNote(double tScore)
        {
            return tScore >= 65 ? "أداء استثنائي - استثمر في هذا المجال"
                : tScore >= 55 ? "أداء جيد جداً - حافظ على هذا المستوى"
                : tScore >= 45 ? "أداء متوسط - مجال للتحسين التدريجي"
                : tScore >= 35 ? "أداء أقل من المتوسط - يحتاج تطوير مركز"
                : "أداء ضعيف - يحتاج تدخل عاجل وخطة تطوير شاملة";
        }
    }
}
