using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using PsyApi.Models;

namespace PsyApi.Services
{
    public interface IRecommendationService
    {
        List<DimensionRecommendation> GetDimensionRecommendations(List<DimensionScore> dimensionScores);
        List<CourseSuggestion> GetSuggestedCourses(List<DimensionScore> dimensionScores);
    }

    public class RecommendationService : IRecommendationService
    {
        private readonly ILogger<RecommendationService> _logger;
        private readonly List<DimensionRecommendationData> _recommendationsData;

        public RecommendationService(ILogger<RecommendationService> logger)
        {
            _logger = logger;

            // Load recommendations data
            _recommendationsData = LoadRecommendationsData();
        }

        public List<DimensionRecommendation> GetDimensionRecommendations(List<DimensionScore> dimensionScores)
        {
            var recommendations = new List<DimensionRecommendation>();

            foreach (var score in dimensionScores)
            {
                var dimensionData = _recommendationsData.FirstOrDefault(d => 
                    d.Dimension.Equals(score.Dimension, StringComparison.OrdinalIgnoreCase));

                if (dimensionData == null)
                {
                    _logger.LogWarning("No recommendation data found for dimension: {Dimension}", score.Dimension);
                    continue;
                }

                // Determine strengths and weaknesses based on T-score
                var strengths = new List<string>();
                var weaknesses = new List<string>();
                var suggestions = new List<string>();

                if (score.T >= 60) // High score
                {
                    strengths.AddRange(dimensionData.Strengths);
                    suggestions.AddRange(dimensionData.HighScoreSuggestions);
                }
                else if (score.T < 40) // Low score
                {
                    weaknesses.AddRange(dimensionData.Weaknesses);
                    suggestions.AddRange(dimensionData.LowScoreSuggestions);
                }
                else // Average score
                {
                    suggestions.AddRange(dimensionData.AverageScoreSuggestions);
                }

                recommendations.Add(new DimensionRecommendation
                {
                    Dimension = score.Dimension,
                    Strengths = strengths,
                    Weaknesses = weaknesses,
                    Suggestions = suggestions
                });
            }

            return recommendations;
        }

        public List<CourseSuggestion> GetSuggestedCourses(List<DimensionScore> dimensionScores)
        {
            // Get the 3 weakest dimensions
            var weakestDimensions = dimensionScores
                .OrderBy(s => s.T)
                .Take(3)
                .Select(s => s.Dimension)
                .ToList();

            var allCourses = new List<CourseSuggestion>
            {
                new CourseSuggestion
                {
                    Name = "مهارات القيادة المتقدمة",
                    Description = "تطوير مهارات القيادة والإدارة الفعالة",
                    Duration = "6 أسابيع",
                    TargetDimensions = new List<string> { "القيادة" }
                },
                new CourseSuggestion
                {
                    Name = "الذكاء العاطفي في مكان العمل",
                    Description = "فهم وإدارة المشاعر لتحسين الأداء المهني",
                    Duration = "4 أسابيع",
                    TargetDimensions = new List<string> { "الذكاء العاطفي" }
                },
                new CourseSuggestion
                {
                    Name = "حل المشكلات الإبداعي",
                    Description = "تطوير أساليب إبداعية لحل المشكلات المعقدة",
                    Duration = "5 أسابيع",
                    TargetDimensions = new List<string> { "حل المشكلات", "الإبداع" }
                },
                new CourseSuggestion
                {
                    Name = "التواصل الفعال",
                    Description = "تحسين مهارات التواصل اللفظي وغير اللفظي",
                    Duration = "4 أسابيع",
                    TargetDimensions = new List<string> { "التواصل" }
                },
                new CourseSuggestion
                {
                    Name = "إدارة الوقت والإنتاجية",
                    Description = "أساليب عملية لتحسين إدارة الوقت وزيادة الإنتاجية",
                    Duration = "3 أسابيع",
                    TargetDimensions = new List<string> { "التنظيم", "إدارة الوقت" }
                },
                new CourseSuggestion
                {
                    Name = "التفكير النقدي",
                    Description = "تطوير مهارات التفكير النقدي والتحليلي",
                    Duration = "5 أسابيع",
                    TargetDimensions = new List<string> { "التفكير النقدي" }
                },
                new CourseSuggestion
                {
                    Name = "العمل الجماعي والتعاون",
                    Description = "تعزيز مهارات العمل ضمن فرق وتحقيق التعاون الفعال",
                    Duration = "4 أسابيع",
                    TargetDimensions = new List<string> { "العمل الجماعي" }
                },
                new CourseSuggestion
                {
                    Name = "المرونة والتكيف",
                    Description = "تطوير القدرة على التكيف مع التغييرات والتحديات",
                    Duration = "3 أسابيع",
                    TargetDimensions = new List<string> { "المرونة" }
                }
            };

            // Return courses that target the weakest dimensions
            var suggestedCourses = allCourses
                .Where(c => c.TargetDimensions.Any(d => weakestDimensions.Contains(d)))
                .Take(5)
                .ToList();

            return suggestedCourses;
        }

        private List<DimensionRecommendationData> LoadRecommendationsData()
        {
            try
            {
                // Path to the recommendations.json file
                var filePath = Path.Combine(AppContext.BaseDirectory, "Resources", "Recommendations", "recommendations.json");
                
                if (!File.Exists(filePath))
                {
                    _logger.LogError("Recommendations file not found at {Path}", filePath);
                    return new List<DimensionRecommendationData>();
                }
                
                // Read JSON content
                var jsonContent = File.ReadAllText(filePath);
                
                // Configure JSON serializer options to support UTF-8 and Arabic
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Arabic),
                    ReadCommentHandling = JsonCommentHandling.Skip
                };
                
                // Deserialize JSON to list of DimensionRecommendationData
                var recommendations = JsonSerializer.Deserialize<List<DimensionRecommendationData>>(jsonContent, options);
                
                if (recommendations == null)
                {
                    _logger.LogError("Failed to deserialize recommendations data");
                    return new List<DimensionRecommendationData>();
                }
                
                _logger.LogInformation("Successfully loaded {Count} dimension recommendations", recommendations.Count);
                return recommendations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading recommendations data");
                return new List<DimensionRecommendationData>();
            }
        }
    }

    // Data models for recommendations
    public class DimensionRecommendation
    {
        public string Dimension { get; set; } = string.Empty;
        public List<string> Strengths { get; set; } = new();
        public List<string> Weaknesses { get; set; } = new();
        public List<string> Suggestions { get; set; } = new();
    }

    public class CourseSuggestion
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public List<string> TargetDimensions { get; set; } = new();
    }

    internal class DimensionRecommendationData
    {
        public string Dimension { get; set; } = string.Empty;
        public List<string> Strengths { get; set; } = new();
        public List<string> Weaknesses { get; set; } = new();
        public List<string> HighScoreSuggestions { get; set; } = new();
        public List<string> LowScoreSuggestions { get; set; } = new();
        public List<string> AverageScoreSuggestions { get; set; } = new();
    }
}
