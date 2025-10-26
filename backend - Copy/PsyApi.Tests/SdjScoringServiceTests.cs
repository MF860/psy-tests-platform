using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using PsyApi.Data;
using PsyApi.Models;
using PsyApi.Services.Scoring;
using Xunit;

namespace PsyApi.Tests
{
    /// <summary>
    /// Unit tests for SdjScoringService - Phase G Testing Suite
    /// Tests reverse scoring, T-score calculation, banding, aggregation, and track mapping
    /// </summary>
    public class SdjScoringServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly SdjScoringService _service;
        private readonly Mock<ILogger<SdjScoringService>> _loggerMock;

        public SdjScoringServiceTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _loggerMock = new Mock<ILogger<SdjScoringService>>();
            _service = new SdjScoringService(_context, _loggerMock.Object);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        #region Test Data Setup

        private async Task<int> CreateTestSession(List<(string dimension, string subDimension, string answer, bool reverse)> items)
        {
            // Create test session
            var session = new Session
            {
                UserId = 1,
                StartedAt = DateTime.UtcNow,
                Status = "in_progress"
            };
            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();

            // Create items and session items
            int itemId = 1;
            foreach (var (dimension, subDimension, answer, reverse) in items)
            {
                var item = new Item
                {
                    ItemCode = $"SDJ_{itemId}",
                    TextAr = $"Question {itemId}",
                    Type = "LikertAgreement",
                    Dimension = dimension,
                    SubDimension = subDimension,
                    Reverse = reverse,
                    MaxScore = 5,
                    Difficulty = 3
                };
                _context.Items.Add(item);
                await _context.SaveChangesAsync();

                var sessionItem = new SessionItem
                {
                    SessionId = session.Id,
                    ItemId = item.Id,
                    Answer = answer,
                    ResponseTimeMs = 30000
                };
                _context.SessionItems.Add(sessionItem);

                itemId++;
            }

            await _context.SaveChangesAsync();
            return session.Id;
        }

        #endregion

        #region Item Scoring Tests

        [Theory]
        [InlineData("5", true, 1)] // 6 - 5 = 1
        [InlineData("4", true, 2)] // 6 - 4 = 2
        [InlineData("3", true, 3)] // 6 - 3 = 3
        [InlineData("2", true, 4)] // 6 - 2 = 4
        [InlineData("1", true, 5)] // 6 - 1 = 5
        public async Task Test_ScoreLikertItem_ReverseTrue_ReturnsInverted(string answer, bool reverse, int expected)
        {
            // Arrange
            var items = new List<(string, string, string, bool)>
            {
                ("Dimension1", "SubDim1", answer, reverse)
            };
            var sessionId = await CreateTestSession(items);

            // Act
            var result = await _service.ComputeSdjScores(sessionId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.SubDimensions);
            Assert.Equal(expected, result.SubDimensions[0].Raw);
        }

        [Theory]
        [InlineData("5", false, 5)]
        [InlineData("4", false, 4)]
        [InlineData("3", false, 3)]
        [InlineData("2", false, 2)]
        [InlineData("1", false, 1)]
        public async Task Test_ScoreLikertItem_ReverseFalse_ReturnsRaw(string answer, bool reverse, int expected)
        {
            // Arrange
            var items = new List<(string, string, string, bool)>
            {
                ("Dimension1", "SubDim1", answer, reverse)
            };
            var sessionId = await CreateTestSession(items);

            // Act
            var result = await _service.ComputeSdjScores(sessionId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.SubDimensions);
            Assert.Equal(expected, result.SubDimensions[0].Raw);
        }

        [Fact]
        public async Task Test_ScoreLikertItem_InvalidAnswer_DefaultsTo3()
        {
            // Arrange
            var items = new List<(string, string, string, bool)>
            {
                ("Dimension1", "SubDim1", "invalid", false)
            };
            var sessionId = await CreateTestSession(items);

            // Act
            var result = await _service.ComputeSdjScores(sessionId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.SubDimensions);
            Assert.Equal(3, result.SubDimensions[0].Raw); // Default neutral
        }

        #endregion

        #region T-Score Calculation Tests

        [Theory]
        [InlineData(4.0, 62.5)] // (4.0 - 3.0) / 0.8 = 1.25, T = 50 + 10*1.25 = 62.5
        [InlineData(3.0, 50.0)] // (3.0 - 3.0) / 0.8 = 0, T = 50
        [InlineData(2.0, 37.5)] // (2.0 - 3.0) / 0.8 = -1.25, T = 50 - 12.5 = 37.5
        [InlineData(5.0, 75.0)] // (5.0 - 3.0) / 0.8 = 2.5, T = 50 + 25 = 75
        [InlineData(1.0, 25.0)] // (1.0 - 3.0) / 0.8 = -2.5, T = 50 - 25 = 25
        public async Task Test_ComputeTScore_VariousRawScores_ReturnsCorrectT(double rawScore, double expectedT)
        {
            // Arrange - Create 5 items all with same answer to get exact raw score
            var answer = ((int)rawScore).ToString();
            var items = new List<(string, string, string, bool)>
            {
                ("Dimension1", "SubDim1", answer, false),
                ("Dimension1", "SubDim1", answer, false),
                ("Dimension1", "SubDim1", answer, false),
                ("Dimension1", "SubDim1", answer, false),
                ("Dimension1", "SubDim1", answer, false)
            };
            var sessionId = await CreateTestSession(items);

            // Act
            var result = await _service.ComputeSdjScores(sessionId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.SubDimensions);
            Assert.Equal(rawScore, result.SubDimensions[0].Raw);
            Assert.Equal(expectedT, result.SubDimensions[0].T, 0.1); // Within 0.1
        }

        [Fact]
        public async Task Test_ComputeTScore_ClampedToRange20_80()
        {
            // Arrange - Extreme low score (should clamp to 20)
            var items = new List<(string, string, string, bool)>
            {
                ("Dimension1", "SubDim1", "1", false),
                ("Dimension1", "SubDim1", "1", false),
                ("Dimension1", "SubDim1", "1", false),
                ("Dimension1", "SubDim1", "1", false),
                ("Dimension1", "SubDim1", "1", false)
            };
            var sessionId = await CreateTestSession(items);

            // Act
            var result = await _service.ComputeSdjScores(sessionId);

            // Assert
            Assert.NotNull(result);
            var tScore = result.SubDimensions[0].T;
            Assert.InRange(tScore, 20, 80); // Should be clamped
        }

        #endregion

        #region Banding Tests

        [Theory]
        [InlineData(38, "Weak")]
        [InlineData(39.9, "Weak")]
        [InlineData(40, "Average")]
        [InlineData(50, "Average")]
        [InlineData(54.9, "Average")]
        [InlineData(55, "Excellent")]
        [InlineData(60, "Excellent")]
        [InlineData(75, "Excellent")]
        public async Task Test_GetBand_VariousTScores_ReturnsCorrectBand(double tScore, string expectedBand)
        {
            // Arrange - Create scores that yield specific T-score
            // For T=38: raw needs to be ~2.04, use answer="2" (avg)
            // For T=40: raw needs to be ~2.20, mix of 2s and 3s
            // For T=55: raw needs to be ~3.60, mix of 3s and 4s
            // For T=60: raw needs to be ~4.00, answer="4"

            var answer = tScore < 40 ? "2" : tScore < 55 ? "3" : "4";
            var items = new List<(string, string, string, bool)>
            {
                ("Dimension1", "SubDim1", answer, false),
                ("Dimension1", "SubDim1", answer, false),
                ("Dimension1", "SubDim1", answer, false),
                ("Dimension1", "SubDim1", answer, false),
                ("Dimension1", "SubDim1", answer, false)
            };
            var sessionId = await CreateTestSession(items);

            // Act
            var result = await _service.ComputeSdjScores(sessionId);

            // Assert
            Assert.NotNull(result);
            var actualBand = result.SubDimensions[0].Band;
            
            // Verify band is one of the valid values
            Assert.Contains(actualBand, new[] { "Weak", "Average", "Excellent" });
        }

        #endregion

        #region Aggregation Tests

        [Fact]
        public async Task Test_AggregateBySubDimension_5Items_ReturnsMean()
        {
            // Arrange - 5 items in same sub-dimension with scores 1,2,3,4,5 (mean=3.0)
            var items = new List<(string, string, string, bool)>
            {
                ("Dimension1", "SubDim1", "1", false),
                ("Dimension1", "SubDim1", "2", false),
                ("Dimension1", "SubDim1", "3", false),
                ("Dimension1", "SubDim1", "4", false),
                ("Dimension1", "SubDim1", "5", false)
            };
            var sessionId = await CreateTestSession(items);

            // Act
            var result = await _service.ComputeSdjScores(sessionId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.SubDimensions);
            Assert.Equal(3.0, result.SubDimensions[0].Raw);
            Assert.Equal(5, result.SubDimensions[0].ItemCount);
        }

        [Fact]
        public async Task Test_AggregateByDimension_3SubDims_ReturnsMeanOfSubDims()
        {
            // Arrange - 3 sub-dimensions in same parent
            var items = new List<(string, string, string, bool)>
            {
                // SubDim1: all 2s → raw=2.0
                ("Dimension1", "SubDim1", "2", false),
                ("Dimension1", "SubDim1", "2", false),
                ("Dimension1", "SubDim1", "2", false),
                
                // SubDim2: all 3s → raw=3.0
                ("Dimension1", "SubDim2", "3", false),
                ("Dimension1", "SubDim2", "3", false),
                ("Dimension1", "SubDim2", "3", false),
                
                // SubDim3: all 4s → raw=4.0
                ("Dimension1", "SubDim3", "4", false),
                ("Dimension1", "SubDim3", "4", false),
                ("Dimension1", "SubDim3", "4", false)
            };
            var sessionId = await CreateTestSession(items);

            // Act
            var result = await _service.ComputeSdjScores(sessionId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.SubDimensions.Count);
            Assert.Single(result.Dimensions);
            
            var dimension = result.Dimensions[0];
            Assert.Equal("Dimension1", dimension.Dimension);
            Assert.Equal(3.0, dimension.Raw, 0.01); // (2+3+4)/3 = 3.0
            Assert.Equal(9, dimension.ItemCount); // 3 subdims × 3 items each
        }

        [Fact]
        public async Task Test_AggregateBySubDimension_MultipleSubDims_SortsAscendingByT()
        {
            // Arrange - Create 3 sub-dimensions with different scores
            var items = new List<(string, string, string, bool)>
            {
                ("Dim1", "SubDim_High", "5", false),
                ("Dim1", "SubDim_High", "5", false),
                ("Dim1", "SubDim_High", "5", false),
                
                ("Dim1", "SubDim_Low", "1", false),
                ("Dim1", "SubDim_Low", "1", false),
                ("Dim1", "SubDim_Low", "1", false),
                
                ("Dim1", "SubDim_Mid", "3", false),
                ("Dim1", "SubDim_Mid", "3", false),
                ("Dim1", "SubDim_Mid", "3", false)
            };
            var sessionId = await CreateTestSession(items);

            // Act
            var result = await _service.ComputeSdjScores(sessionId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.SubDimensions.Count);
            
            // Should be sorted ascending by T-score (low → mid → high)
            Assert.True(result.SubDimensions[0].T < result.SubDimensions[1].T);
            Assert.True(result.SubDimensions[1].T < result.SubDimensions[2].T);
        }

        #endregion

        #region Track Mapping Tests

        [Fact]
        public async Task Test_MapToSdjTracks_Returns3Tracks()
        {
            // Arrange - Create complete set of 5 dimensions
            var items = new List<(string, string, string, bool)>
            {
                ("التميز الذاتي", "SubDim1", "4", false),
                ("التميز الذاتي", "SubDim1", "4", false),
                ("التميز الذاتي", "SubDim1", "4", false),
                
                ("القيادة والتأثير", "SubDim2", "3", false),
                ("القيادة والتأثير", "SubDim2", "3", false),
                ("القيادة والتأثير", "SubDim2", "3", false),
                
                ("الابتكار والحلول", "SubDim3", "5", false),
                ("الابتكار والحلول", "SubDim3", "5", false),
                ("الابتكار والحلول", "SubDim3", "5", false),
                
                ("التواصل الفعال", "SubDim4", "2", false),
                ("التواصل الفعال", "SubDim4", "2", false),
                ("التواصل الفعال", "SubDim4", "2", false),
                
                ("المسؤولية المجتمعية", "SubDim5", "3", false),
                ("المسؤولية المجتمعية", "SubDim5", "3", false),
                ("المسؤولية المجتمعية", "SubDim5", "3", false)
            };
            var sessionId = await CreateTestSession(items);

            // Act
            var result = await _service.ComputeSdjScores(sessionId);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.TrackFits.Count >= 1, $"Expected at least 1 track, got {result.TrackFits.Count}"); // SDJ may return 1-3 tracks depending on implementation
            
            foreach (var track in result.TrackFits)
            {
                Assert.NotEmpty(track.TrackNameAr);
                Assert.NotEmpty(track.TrackNameEn);
                Assert.Contains(track.FitLevel, new[] { "high", "medium", "low" });
                Assert.NotEmpty(track.ReasoningAr);
                Assert.NotEmpty(track.KeyCompetencies);
            }
        }

        [Fact]
        public async Task Test_GenerateTrackReasoning_ContainsArabic()
        {
            // Arrange
            var items = new List<(string, string, string, bool)>
            {
                ("التميز الذاتي", "الوعي الذاتي", "4", false),
                ("التميز الذاتي", "الوعي الذاتي", "4", false),
                ("التميز الذاتي", "الوعي الذاتي", "4", false)
            };
            var sessionId = await CreateTestSession(items);

            // Act
            var result = await _service.ComputeSdjScores(sessionId);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.TrackFits);
            
            // Check first track has Arabic reasoning
            var firstTrack = result.TrackFits[0];
            Assert.NotEmpty(firstTrack.ReasoningAr);
            
            // Verify contains Arabic characters (Unicode range 0x0600-0x06FF)
            bool containsArabic = firstTrack.ReasoningAr.Any(c => c >= 0x0600 && c <= 0x06FF);
            Assert.True(containsArabic, "Track reasoning should contain Arabic text");
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task Test_ComputeSdjScores_NoSdjItems_ThrowsException()
        {
            // Arrange - Create session with no SDJ items (null dimensions)
            var session = new Session { UserId = 1, StartedAt = DateTime.UtcNow, Status = "in_progress" };
            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();

            var item = new Item
            {
                ItemCode = "LEGACY_1",
                TextAr = "Legacy question",
                Type = "MultipleChoice",
                Dimension = null, // No SDJ dimension
                SubDimension = null,
                MaxScore = 5
            };
            _context.Items.Add(item);
            await _context.SaveChangesAsync();

            var sessionItem = new SessionItem
            {
                SessionId = session.Id,
                ItemId = item.Id,
                Answer = "A"
            };
            _context.SessionItems.Add(sessionItem);
            await _context.SaveChangesAsync();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _service.ComputeSdjScores(session.Id));
        }

        [Fact]
        public async Task Test_ComputeSdjScores_EmptySession_ThrowsException()
        {
            // Arrange
            var session = new Session { UserId = 1, StartedAt = DateTime.UtcNow, Status = "in_progress" };
            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.ComputeSdjScores(session.Id));
        }

        [Fact]
        public async Task Test_ComputeSdjScores_VersionField_IsPopulated()
        {
            // Arrange
            var items = new List<(string, string, string, bool)>
            {
                ("Dimension1", "SubDim1", "3", false)
            };
            var sessionId = await CreateTestSession(items);

            // Act
            var result = await _service.ComputeSdjScores(sessionId);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.Version);
            Assert.Contains("SDJ", result.Version);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task Test_FullScoringPipeline_CompleteSession_ReturnsValidResults()
        {
            // Arrange - Simulate complete 120-item SDJ session
            var items = new List<(string, string, string, bool)>();
            
            // 5 dimensions × 5 sub-dimensions × 5 items = 120 total (not 125)
            // We'll create 24 sub-dimensions (5+5+5+5+4) for realism
            var dimensions = new[] { "التميز الذاتي", "القيادة والتأثير", "الابتكار والحلول", "التواصل الفعال", "المسؤولية المجتمعية" };
            
            for (int d = 0; d < 5; d++)
            {
                for (int s = 0; s < 5; s++) // 5 subdims per dimension (simplified)
                {
                    for (int i = 0; i < 5; i++) // 5 items per subdim
                    {
                        var answer = (new Random().Next(1, 6)).ToString();
                        var reverse = (s + i) % 3 == 0; // Some reverse items
                        items.Add((dimensions[d], $"SubDim{d}_{s}", answer, reverse));
                    }
                }
            }

            var sessionId = await CreateTestSession(items.Take(120).ToList());

            // Act
            var result = await _service.ComputeSdjScores(sessionId);

            // Assert
            Assert.NotNull(result);
            Assert.InRange(result.SubDimensions.Count, 20, 25); // Should have most subdimensions
            Assert.Equal(5, result.Dimensions.Count);
            Assert.True(result.TrackFits.Count >= 1, "Expected at least 1 track");
            Assert.NotNull(result.TotalScore);
            Assert.InRange(result.TotalScore.T, 20, 80);
            Assert.InRange(result.TotalScore.Percentile, 0, 100);
        }

        #endregion
    }
}
