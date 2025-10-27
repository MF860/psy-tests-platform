using PsyApi.Models;

namespace PsyApi.Services.Scoring
{
    /// <summary>
    /// Maps existing SDJ dimensions and sub-dimensions to the 7 Major Patterns structure
    /// Deterministic, rule-based mapping - no AI/external calls
    /// </summary>
    public class SevenPatternsMapper
    {
        /// <summary>
        /// The 7 Major Patterns for SDJ reporting
        /// </summary>
        public static readonly List<SevenPattern> Patterns = new()
        {
            new SevenPattern
            {
                Key = "personality_patterns",
                NameAr = "الأنماط الشخصية",
                NameEn = "Personality Patterns (MBTI, Big Five, Learning Style)",
                Description = "MBTI، Big Five، ونمط التعلم",
                Order = 1
            },
            new SevenPattern
            {
                Key = "cognitive_mental",
                NameAr = "القدرات المعرفية والعقلية",
                NameEn = "Cognitive & Mental Abilities",
                Description = "الذكاءات المتعددة، الذاكرة، الانتباه، الإبداع",
                Order = 2
            },
            new SevenPattern
            {
                Key = "psychological_patterns",
                NameAr = "الأنماط النفسية",
                NameEn = "Psychological Patterns",
                Description = "التوتر، القلق، المرونة، الذكاء العاطفي",
                Order = 3
            },
            new SevenPattern
            {
                Key = "behavioral_patterns",
                NameAr = "الأنماط السلوكية",
                NameEn = "Behavioral Patterns",
                Description = "التكيف، القيادة، الغضب",
                Order = 4
            },
            new SevenPattern
            {
                Key = "numerical_logical",
                NameAr = "الأنماط العددية والمنطقية",
                NameEn = "Numerical & Logical Patterns",
                Description = "الحساب، الاستنتاج، معامل الارتباط",
                Order = 5
            },
            new SevenPattern
            {
                Key = "leadership_organizational",
                NameAr = "الأنماط القيادية والتنظيمية",
                NameEn = "Leadership & Organizational Patterns",
                Description = "القيادة، اتخاذ القرار، الثواب والعقاب",
                Order = 6
            },
            new SevenPattern
            {
                Key = "professional_readiness",
                NameAr = "الاستعدادات المهنية العامة",
                NameEn = "General Professional Readiness",
                Description = "التعامل مع مواقف العمل المعقدة",
                Order = 7
            }
        };

        /// <summary>
        /// Maps existing sub-dimensions to the 7 patterns
        /// Key = sub-dimension name (Arabic), Value = pattern key
        /// </summary>
        private static readonly Dictionary<string, string> SubDimensionToPatternMap = new()
        {
            // Pattern 1: Personality Patterns (الأنماط الشخصية)
            {"الوعي الذاتي", "personality_patterns"},
            {"الثقة بالنفس", "personality_patterns"},
            {"التعلم المستمر", "personality_patterns"},

            // Pattern 2: Cognitive & Mental (القدرات المعرفية والعقلية)
            {"التنظيم الذاتي", "cognitive_mental"},
            {"حل المشكلات", "cognitive_mental"},
            {"الإبداع والابتكار", "cognitive_mental"},

            // Pattern 3: Psychological Patterns (الأنماط النفسية)
            {"المرونة النفسية", "psychological_patterns"},
            {"الذكاء العاطفي", "psychological_patterns"},
            {"الصحة النفسية", "psychological_patterns"},
            {"إدارة الضغوط", "psychological_patterns"},

            // Pattern 4: Behavioral Patterns (الأنماط السلوكية)
            {"التواصل الفعال", "behavioral_patterns"},
            {"التعاون", "behavioral_patterns"},
            {"حل النزاعات", "behavioral_patterns"},
            {"بناء العلاقات", "behavioral_patterns"},

            // Pattern 5: Numerical & Logical (الأنماط العددية والمنطقية)
            {"التخطيط الاستراتيجي", "numerical_logical"},
            {"إدارة الوقت", "numerical_logical"},

            // Pattern 6: Leadership & Organizational (الأنماط القيادية والتنظيمية)
            {"القيادة", "leadership_organizational"},

            // Pattern 7: Professional Readiness (الاستعدادات المهنية العامة)
            {"الوعي المجتمعي", "professional_readiness"},
            {"الأخلاق المهنية", "professional_readiness"},
            {"الاستدامة", "professional_readiness"},
            {"العمل التطوعي", "professional_readiness"},
            {"المواطنة الفاعلة", "professional_readiness"},
            {"الصحة الجسدية", "professional_readiness"},
            {"التوازن بين العمل والحياة", "professional_readiness"}
        };

        /// <summary>
        /// Aggregates existing dimension/sub-dimension scores into 7 pattern scores
        /// Uses T-score averaging within each pattern
        /// </summary>
        public static List<SevenPatternScore> MapToSevenPatterns(SdjScoreSummary sdjScores)
        {
            var patternScores = new List<SevenPatternScore>();

            foreach (var pattern in Patterns.OrderBy(p => p.Order))
            {
                // Find all sub-dimensions that map to this pattern
                var relevantSubDimensions = sdjScores.SubDimensions
                    .Where(sd => SubDimensionToPatternMap.TryGetValue(sd.SubDimension, out var patternKey) 
                                 && patternKey == pattern.Key)
                    .ToList();

                if (relevantSubDimensions.Any())
                {
                    var avgRaw = relevantSubDimensions.Average(sd => sd.Raw);
                    var avgT = relevantSubDimensions.Average(sd => sd.T);
                    var avgPercentile = relevantSubDimensions.Average(sd => sd.Percentile);
                    var band = DetermineBand(avgT);

                    patternScores.Add(new SevenPatternScore
                    {
                        PatternKey = pattern.Key,
                        PatternNameAr = pattern.NameAr,
                        PatternNameEn = pattern.NameEn,
                        Description = pattern.Description,
                        Raw = avgRaw,
                        TScore = avgT,
                        Percentile = avgPercentile,
                        Band = band,
                        SubDimensionCount = relevantSubDimensions.Count,
                        SubDimensions = relevantSubDimensions
                    });
                }
                else
                {
                    // Pattern has no mapped sub-dimensions - default to neutral score
                    patternScores.Add(new SevenPatternScore
                    {
                        PatternKey = pattern.Key,
                        PatternNameAr = pattern.NameAr,
                        PatternNameEn = pattern.NameEn,
                        Description = pattern.Description,
                        Raw = 3.0,
                        TScore = 50.0,
                        Percentile = 0.50,
                        Band = "Average",
                        SubDimensionCount = 0,
                        SubDimensions = new List<SdjSubDimensionScore>()
                    });
                }
            }

            return patternScores;
        }

        /// <summary>
        /// Determines band based on T-score thresholds
        /// Weak: <40, Average: 40-54.9, Excellent: ≥55
        /// </summary>
        private static string DetermineBand(double tScore)
        {
            if (tScore < 40) return "Weak";
            if (tScore < 55) return "Average";
            return "Excellent";
        }

        /// <summary>
        /// Gets pattern details by key
        /// </summary>
        public static SevenPattern? GetPattern(string key)
        {
            return Patterns.FirstOrDefault(p => p.Key == key);
        }

        /// <summary>
        /// Gets all sub-dimensions mapped to a specific pattern
        /// </summary>
        public static List<string> GetSubDimensionsForPattern(string patternKey)
        {
            return SubDimensionToPatternMap
                .Where(kvp => kvp.Value == patternKey)
                .Select(kvp => kvp.Key)
                .ToList();
        }
    }

    /// <summary>
    /// Definition of a single pattern in the 7-pattern model
    /// </summary>
    public class SevenPattern
    {
        public string Key { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Order { get; set; }
    }

    /// <summary>
    /// Score for one of the 7 patterns
    /// </summary>
    public class SevenPatternScore
    {
        public string PatternKey { get; set; } = string.Empty;
        public string PatternNameAr { get; set; } = string.Empty;
        public string PatternNameEn { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Raw { get; set; }
        public double TScore { get; set; }
        public double Percentile { get; set; }
        public string Band { get; set; } = string.Empty; // "Weak", "Average", "Excellent"
        public int SubDimensionCount { get; set; }
        public List<SdjSubDimensionScore> SubDimensions { get; set; } = new();
    }
}
