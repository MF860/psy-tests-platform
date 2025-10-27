using Xunit;
using PsyApi.Services.Scoring;
using System.Collections.Generic;
using System.Linq;

namespace PsyApi.Tests
{
    /// <summary>
    /// Unit tests for SevenPatternsMapper
    /// Tests mapping coverage, aggregation logic, and banding thresholds
    /// </summary>
    public class SevenPatternsMapperTests
    {
        [Fact]
        public void MapToSevenPatterns_ShouldReturn7Patterns()
        {
            // Arrange
            var sampleData = CreateSampleSdjData();

            // Act
            var patterns = SevenPatternsMapper.MapToSevenPatterns(sampleData);

            // Assert
            Assert.NotNull(patterns);
            Assert.Equal(7, patterns.Count);
            Assert.All(patterns, p => Assert.False(string.IsNullOrEmpty(p.PatternNameAr)));
        }

        [Fact]
        public void MapToSevenPatterns_ShouldCorrectlyAggregateTScores()
        {
            // Arrange
            var sampleData = CreateSampleSdjData();

            // Act
            var patterns = SevenPatternsMapper.MapToSevenPatterns(sampleData);
            var personalityPattern = patterns.First(p => p.PatternKey == "personality_patterns");

            // Assert
            // Personality pattern should have mapped sub-dimensions with T-scores
            Assert.True(personalityPattern.TScore > 0);
            Assert.True(personalityPattern.Percentile >= 0 && personalityPattern.Percentile <= 1);
        }

        [Theory]
        [InlineData(35.0, "Weak")]
        [InlineData(39.9, "Weak")]
        [InlineData(40.0, "Average")]
        [InlineData(45.0, "Average")]
        [InlineData(54.9, "Average")]
        [InlineData(55.0, "Excellent")]
        [InlineData(70.0, "Excellent")]
        [InlineData(80.0, "Excellent")]
        public void BandingLogic_ShouldFollowCorrectThresholds(double tScore, string expectedBand)
        {
            // Arrange
            var sampleData = CreateSampleSdjDataWithSpecificTScore(tScore);

            // Act
            var patterns = SevenPatternsMapper.MapToSevenPatterns(sampleData);
            var firstPattern = patterns.First();

            // Assert
            Assert.Equal(expectedBand, firstPattern.Band);
        }

        [Fact]
        public void SubDimensionMapping_ShouldCoverAllDefinedSubDimensions()
        {
            // Arrange
            var allSubDimensions = new List<string>
            {
                "الوعي الذاتي", "الثقة بالنفس", "التنظيم الذاتي", "التعلم المستمر", "المرونة النفسية",
                "الذكاء العاطفي", "التواصل الفعال", "التعاون", "حل النزاعات", "بناء العلاقات",
                "القيادة", "حل المشكلات", "الإبداع والابتكار", "إدارة الوقت", "التخطيط الاستراتيجي",
                "الوعي المجتمعي", "الأخلاق المهنية", "الاستدامة", "العمل التطوعي", "المواطنة الفاعلة",
                "الصحة النفسية", "الصحة الجسدية", "إدارة الضغوط", "التوازن بين العمل والحياة"
            };

            // Act
            var mapped = 0;
            foreach (var pattern in SevenPatternsMapper.Patterns)
            {
                var subDims = SevenPatternsMapper.GetSubDimensionsForPattern(pattern.Key);
                mapped += subDims.Count;
                
                // Assert: All returned sub-dimensions should be in the master list
                Assert.All(subDims, sd => Assert.Contains(sd, allSubDimensions));
            }

            // Assert: All 24 sub-dimensions should be mapped
            Assert.Equal(24, mapped);
        }

        [Fact]
        public void GetPattern_ShouldReturnCorrectPatternByKey()
        {
            // Act
            var cognitivePattern = SevenPatternsMapper.GetPattern("cognitive_mental");
            var invalidPattern = SevenPatternsMapper.GetPattern("invalid_key");

            // Assert
            Assert.NotNull(cognitivePattern);
            Assert.Equal("القدرات المعرفية والعقلية", cognitivePattern.NameAr);
            Assert.Null(invalidPattern);
        }

        [Fact]
        public void MapToSevenPatterns_ShouldHandleEmptySubDimensions()
        {
            // Arrange
            var emptyData = new SdjScoreSummary
            {
                Dimensions = new List<SdjDimensionScore>(),
                SubDimensions = new List<SdjSubDimensionScore>()
            };

            // Act
            var patterns = SevenPatternsMapper.MapToSevenPatterns(emptyData);

            // Assert
            Assert.Equal(7, patterns.Count);
            Assert.All(patterns, p =>
            {
                Assert.Equal(50.0, p.TScore); // Default neutral T-score
                Assert.Equal("Average", p.Band);
                Assert.Equal(0, p.SubDimensionCount);
            });
        }

        [Fact]
        public void MapToSevenPatterns_ShouldPreserveSubDimensionDetails()
        {
            // Arrange
            var sampleData = CreateSampleSdjData();

            // Act
            var patterns = SevenPatternsMapper.MapToSevenPatterns(sampleData);
            var psychologicalPattern = patterns.First(p => p.PatternKey == "psychological_patterns");

            // Assert
            Assert.NotEmpty(psychologicalPattern.SubDimensions);
            Assert.All(psychologicalPattern.SubDimensions, sd =>
            {
                Assert.False(string.IsNullOrEmpty(sd.SubDimension));
                Assert.True(sd.T >= 20 && sd.T <= 80);
            });
        }

        #region Helper Methods

        private SdjScoreSummary CreateSampleSdjData()
        {
            var subDimensions = new List<SdjSubDimensionScore>
            {
                // Personality patterns
                new SdjSubDimensionScore { Dimension = "التميز الذاتي", SubDimension = "الوعي الذاتي", Raw = 4.2, T = 58.0, Percentile = 0.75, Band = "Excellent", ItemCount = 5 },
                new SdjSubDimensionScore { Dimension = "التميز الذاتي", SubDimension = "الثقة بالنفس", Raw = 3.8, T = 52.0, Percentile = 0.60, Band = "Average", ItemCount = 5 },
                new SdjSubDimensionScore { Dimension = "التميز الذاتي", SubDimension = "التعلم المستمر", Raw = 4.5, T = 62.5, Percentile = 0.82, Band = "Excellent", ItemCount = 5 },
                
                // Cognitive & mental
                new SdjSubDimensionScore { Dimension = "التميز الذاتي", SubDimension = "التنظيم الذاتي", Raw = 3.5, T = 47.5, Percentile = 0.45, Band = "Average", ItemCount = 5 },
                new SdjSubDimensionScore { Dimension = "النجاح المهني", SubDimension = "حل المشكلات", Raw = 4.0, T = 55.0, Percentile = 0.70, Band = "Excellent", ItemCount = 5 },
                new SdjSubDimensionScore { Dimension = "النجاح المهني", SubDimension = "الإبداع والابتكار", Raw = 3.2, T = 42.0, Percentile = 0.35, Band = "Average", ItemCount = 5 },
                
                // Psychological patterns
                new SdjSubDimensionScore { Dimension = "التميز الذاتي", SubDimension = "المرونة النفسية", Raw = 3.0, T = 37.5, Percentile = 0.25, Band = "Weak", ItemCount = 5 },
                new SdjSubDimensionScore { Dimension = "التواصل والعلاقات", SubDimension = "الذكاء العاطفي", Raw = 4.3, T = 60.0, Percentile = 0.78, Band = "Excellent", ItemCount = 5 },
                new SdjSubDimensionScore { Dimension = "الصحة والتوازن", SubDimension = "الصحة النفسية", Raw = 3.6, T = 48.0, Percentile = 0.50, Band = "Average", ItemCount = 5 },
                new SdjSubDimensionScore { Dimension = "الصحة والتوازن", SubDimension = "إدارة الضغوط", Raw = 3.4, T = 45.0, Percentile = 0.42, Band = "Average", ItemCount = 5 },
                
                // Behavioral patterns
                new SdjSubDimensionScore { Dimension = "التواصل والعلاقات", SubDimension = "التواصل الفعال", Raw = 4.1, T = 56.0, Percentile = 0.72, Band = "Excellent", ItemCount = 5 },
                new SdjSubDimensionScore { Dimension = "التواصل والعلاقات", SubDimension = "التعاون", Raw = 4.4, T = 61.0, Percentile = 0.80, Band = "Excellent", ItemCount = 5 },
                new SdjSubDimensionScore { Dimension = "التواصل والعلاقات", SubDimension = "حل النزاعات", Raw = 3.7, T = 50.0, Percentile = 0.55, Band = "Average", ItemCount = 5 },
                new SdjSubDimensionScore { Dimension = "التواصل والعلاقات", SubDimension = "بناء العلاقات", Raw = 3.9, T = 53.0, Percentile = 0.65, Band = "Average", ItemCount = 5 },
                
                // Numerical & logical
                new SdjSubDimensionScore { Dimension = "النجاح المهني", SubDimension = "التخطيط الاستراتيجي", Raw = 3.3, T = 43.0, Percentile = 0.38, Band = "Average", ItemCount = 5 },
                new SdjSubDimensionScore { Dimension = "النجاح المهني", SubDimension = "إدارة الوقت", Raw = 3.1, T = 40.0, Percentile = 0.30, Band = "Average", ItemCount = 5 },
                
                // Leadership & organizational
                new SdjSubDimensionScore { Dimension = "النجاح المهني", SubDimension = "القيادة", Raw = 4.6, T = 65.0, Percentile = 0.85, Band = "Excellent", ItemCount = 5 },
                
                // Professional readiness
                new SdjSubDimensionScore { Dimension = "المسؤولية الاجتماعية", SubDimension = "الوعي المجتمعي", Raw = 3.8, T = 52.0, Percentile = 0.60, Band = "Average", ItemCount = 5 },
                new SdjSubDimensionScore { Dimension = "المسؤولية الاجتماعية", SubDimension = "الأخلاق المهنية", Raw = 4.7, T = 67.0, Percentile = 0.88, Band = "Excellent", ItemCount = 5 },
                new SdjSubDimensionScore { Dimension = "المسؤولية الاجتماعية", SubDimension = "الاستدامة", Raw = 3.5, T = 47.5, Percentile = 0.45, Band = "Average", ItemCount = 5 },
                new SdjSubDimensionScore { Dimension = "المسؤولية الاجتماعية", SubDimension = "العمل التطوعي", Raw = 3.2, T = 42.0, Percentile = 0.35, Band = "Average", ItemCount = 5 },
                new SdjSubDimensionScore { Dimension = "المسؤولية الاجتماعية", SubDimension = "المواطنة الفاعلة", Raw = 3.6, T = 48.0, Percentile = 0.50, Band = "Average", ItemCount = 5 },
                new SdjSubDimensionScore { Dimension = "الصحة والتوازن", SubDimension = "الصحة الجسدية", Raw = 3.9, T = 53.0, Percentile = 0.65, Band = "Average", ItemCount = 5 },
                new SdjSubDimensionScore { Dimension = "الصحة والتوازن", SubDimension = "التوازن بين العمل والحياة", Raw = 3.4, T = 45.0, Percentile = 0.42, Band = "Average", ItemCount = 5 }
            };

            return new SdjScoreSummary
            {
                SubDimensions = subDimensions,
                Dimensions = new List<SdjDimensionScore>(),
                TotalScore = new SdjTotalScore { Raw = 3.75, T = 51.25, Percentile = 0.58 }
            };
        }

        private SdjScoreSummary CreateSampleSdjDataWithSpecificTScore(double tScore)
        {
            var subDimensions = new List<SdjSubDimensionScore>
            {
                new SdjSubDimensionScore 
                { 
                    Dimension = "Test", 
                    SubDimension = "الوعي الذاتي", 
                    Raw = 3.0, 
                    T = tScore, 
                    Percentile = 0.50, 
                    Band = "Test", 
                    ItemCount = 5 
                }
            };

            return new SdjScoreSummary
            {
                SubDimensions = subDimensions,
                Dimensions = new List<SdjDimensionScore>(),
                TotalScore = new SdjTotalScore { Raw = 3.0, T = tScore, Percentile = 0.50 }
            };
        }

        #endregion
    }
}
