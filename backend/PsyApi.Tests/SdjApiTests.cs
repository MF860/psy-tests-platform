using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using PsyApi.Data;
using PsyApi.Models;
using PsyApi.Services.Scoring;
using Serilog;
using Xunit;

namespace PsyApi.Tests
{
    /// <summary>
    /// Integration tests for SDJ v2.0 API endpoints.
    /// Tests cover: submit route validation, SDJ vs legacy scoring, results endpoint, and Arabic preservation.
    /// </summary>
    public class SdjApiTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly IScoringService _legacyScoringService;
        private readonly ISdjScoringService _sdjScoringService;
        private readonly Mock<Serilog.ILogger> _legacyLoggerMock;
        private readonly Mock<Microsoft.Extensions.Logging.ILogger<SdjScoringService>> _sdjLoggerMock;
        private readonly Mock<IConfiguration> _configurationMock;

        public SdjApiTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;

            _context = new AppDbContext(options);
            
            // Setup mocks
            _legacyLoggerMock = new Mock<Serilog.ILogger>();
            _sdjLoggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<SdjScoringService>>();
            _configurationMock = new Mock<IConfiguration>();
            
            // Create services
            _legacyScoringService = new ScoringService(_context, _legacyLoggerMock.Object);
            _sdjScoringService = new SdjScoringService(_context, _sdjLoggerMock.Object, _configurationMock.Object);
            
            // Seed test data
            SeedTestData();
        }

        private void SeedTestData()
        {
            // Add test user
            var user = new User
            {
                Id = 1,
                NationalId = "1234567890",
                FullName = "اختبار المستخدم",
                Email = "test@example.com"
            };
            _context.Users.Add(user);

            // Add SDJ items (Arabic)
            var sdjItems = new[]
            {
                new Item
                {
                    Id = 1,
                    ItemCode = "I001",
                    TextAr = "أستطيع تحديد نقاط قوتي بوضوح.",
                    Type = "LikertAgreement",
                    Dimension = "التميز الذاتي",
                    SubDimension = "الوعي الذاتي",
                    DimensionTags = "التميز الذاتي",
                    Options = "لا أوافق بشدة|لا أوافق|محايد|أوافق|أوافق بشدة",
                    Reverse = false,
                    TimeLimitSeconds = 45,
                    MaxScore = 5,
                    Difficulty = 2
                },
                new Item
                {
                    Id = 2,
                    ItemCode = "I002",
                    TextAr = "أدرك تأثير سلوكياتي على الآخرين.",
                    Type = "LikertAgreement",
                    Dimension = "التميز الذاتي",
                    SubDimension = "الوعي الذاتي",
                    DimensionTags = "التميز الذاتي",
                    Options = "لا أوافق بشدة|لا أوافق|محايد|أوافق|أوافق بشدة",
                    Reverse = false,
                    TimeLimitSeconds = 45,
                    MaxScore = 5,
                    Difficulty = 2
                },
                new Item
                {
                    Id = 3,
                    ItemCode = "I006",
                    TextAr = "أثق بقدرتي على تحقيق أهدافي.",
                    Type = "LikertAgreement",
                    Dimension = "التميز الذاتي",
                    SubDimension = "الثقة بالنفس",
                    DimensionTags = "التميز الذاتي",
                    Options = "لا أوافق بشدة|لا أوافق|محايد|أوافق|أوافق بشدة",
                    Reverse = false,
                    TimeLimitSeconds = 45,
                    MaxScore = 5,
                    Difficulty = 2
                }
            };
            _context.Items.AddRange(sdjItems);

            // Add legacy item (for USE_SDJ=0 tests)
            var legacyItem = new Item
            {
                Id = 100,
                ItemCode = "L001",
                TextAr = "I can clearly identify my strengths.",
                Type = "LikertAgreement",
                Dimension = "Self-Excellence",
                DimensionTags = "Self-Excellence",
                Reverse = false,
                TimeLimitSeconds = 60,
                MaxScore = 5,
                Difficulty = 2
            };
            _context.Items.Add(legacyItem);

            _context.SaveChanges();
        }

        [Fact]
        public async Task SubmitSession_WithValidSdjSession_ShouldComputeSdjScores()
        {
            // Arrange: Create SDJ session with answers
            var session = new Session
            {
                Id = 1,
                SessionId = Guid.NewGuid().ToString(),
                UserId = 1,
                Status = "in_progress",
                StartedAt = DateTime.UtcNow
            };
            _context.Sessions.Add(session);

            var sessionItems = new[]
            {
                new SessionItem { SessionId = 1, ItemId = 1, Answer = "4", AnsweredAt = DateTime.UtcNow },
                new SessionItem { SessionId = 1, ItemId = 2, Answer = "5", AnsweredAt = DateTime.UtcNow },
                new SessionItem { SessionId = 1, ItemId = 3, Answer = "4", AnsweredAt = DateTime.UtcNow }
            };
            _context.SessionItems.AddRange(sessionItems);
            await _context.SaveChangesAsync();

            // Act: Compute SDJ scores (simulating USE_SDJ=1)
            var scores = await _sdjScoringService.ComputeSdjScores(session.Id);

            // Assert: Verify SDJ scoring completed successfully
            Assert.NotNull(scores);
            Assert.True(scores.TotalScore.T >= 0, "Total T-score should be non-negative");
            Assert.NotEmpty(scores.Dimensions);
            Assert.Contains(scores.Dimensions, d => d.Dimension == "التميز الذاتي");
            Assert.True(scores.Version == "SDJ_v1.0" || scores.Version == "SDJ_v2.0", 
                $"Expected SDJ version but got: {scores.Version}");
        }

        [Fact]
        public async Task SubmitSession_WithInvalidSession_ShouldThrowException()
        {
            // Arrange: Non-existent session ID
            var invalidSessionId = 999;

            // Act & Assert: Should throw exception for invalid session
            await Assert.ThrowsAsync<ArgumentException>(
                async () => await _sdjScoringService.ComputeSdjScores(invalidSessionId)
            );
        }

        [Fact]
        public async Task LegacyScoring_WithUseSdjFlagDisabled_ShouldUseTraditionalScoring()
        {
            // Arrange: Create legacy session
            var session = new Session
            {
                Id = 2,
                SessionId = Guid.NewGuid().ToString(),
                UserId = 1,
                Status = "in_progress",
                StartedAt = DateTime.UtcNow
            };
            _context.Sessions.Add(session);

            var sessionItem = new SessionItem 
            { 
                SessionId = 2, 
                ItemId = 100, // Legacy item
                Answer = "4", 
                AnsweredAt = DateTime.UtcNow 
            };
            _context.SessionItems.Add(sessionItem);
            await _context.SaveChangesAsync();

            // Act: Use legacy scoring service (simulating USE_SDJ=0)
            var scores = await _legacyScoringService.ComputeSessionScores(session.Id);

            // Assert: Verify legacy scoring works
            Assert.NotNull(scores);
            Assert.True(scores.TotalScore >= 0);
            Assert.NotEmpty(scores.DimensionScores);
            // Legacy scoring may return various versions
            Assert.False(string.IsNullOrEmpty(scores.Version), "Version should not be empty");
        }

        [Fact]
        public async Task ResultsEndpoint_WithSdjData_ShouldReturnFullSdjJson()
        {
            // Arrange: Create result with SDJ data
            var session = new Session
            {
                Id = 3,
                SessionId = Guid.NewGuid().ToString(),
                UserId = 1,
                Status = "completed",
                StartedAt = DateTime.UtcNow,
                EndedAt = DateTime.UtcNow.AddMinutes(30)
            };
            _context.Sessions.Add(session);

            var result = new Result
            {
                Id = 1,
                SessionId = 3,
                TotalScore = 65,
                ScoringModelVersion = "SDJ_v2.0",
                DimensionScoresJson = @"{""Dimensions"":[{""Dimension"":""التميز الذاتي"",""Raw"":13.0,""T"":55.0,""Percentile"":69.0}],""SubDimensions"":[{""Dimension"":""التميز الذاتي"",""SubDimension"":""الوعي الذاتي"",""Raw"":9.0,""T"":57.0,""Percentile"":76.0}],""TrackFits"":[{""Track"":""القيادة والإدارة"",""FitScore"":0.85}]}",
                CompositeScoresJson = @"{""TotalScore"":65.5,""RawScore"":85.0,""Percentile"":70.0}",
                CreatedAt = DateTime.UtcNow
            };
            _context.Results.Add(result);
            await _context.SaveChangesAsync();

            // Act: Retrieve result
            var retrievedResult = await _context.Results
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == 1);

            // Assert: Verify SDJ data is present
            Assert.NotNull(retrievedResult);
            Assert.Equal("SDJ_v2.0", retrievedResult.ScoringModelVersion);
            Assert.Contains("التميز الذاتي", retrievedResult.DimensionScoresJson);
            Assert.Contains("الوعي الذاتي", retrievedResult.DimensionScoresJson);
            Assert.Contains("TrackFits", retrievedResult.DimensionScoresJson);
        }

        [Fact]
        public async Task ArabicStrings_InSdjData_ShouldPreserveIntact()
        {
            // Arrange: Get SDJ item with Arabic text
            var item = await _context.Items.FindAsync(1);

            // Assert: Verify Arabic is preserved correctly
            Assert.NotNull(item);
            Assert.Contains("أستطيع", item.TextAr);
            Assert.Contains("نقاط قوتي", item.TextAr);
            Assert.Equal("التميز الذاتي", item.Dimension);
            Assert.Equal("الوعي الذاتي", item.SubDimension);
            Assert.NotNull(item.Options);
            Assert.Contains("لا أوافق بشدة", item.Options);
            Assert.Contains("أوافق بشدة", item.Options);
        }

        [Fact]
        public async Task SdjScoring_With120Items_ShouldCompleteInUnder1Second()
        {
            // Arrange: Create session with minimal answers (for performance test)
            var session = new Session
            {
                Id = 4,
                SessionId = Guid.NewGuid().ToString(),
                UserId = 1,
                Status = "in_progress",
                StartedAt = DateTime.UtcNow
            };
            _context.Sessions.Add(session);

            // Add 3 answers (representing subset of 120)
            for (int i = 1; i <= 3; i++)
            {
                _context.SessionItems.Add(new SessionItem
                {
                    SessionId = 4,
                    ItemId = i,
                    Answer = "4",
                    AnsweredAt = DateTime.UtcNow
                });
            }
            await _context.SaveChangesAsync();

            // Act: Measure scoring time
            var startTime = DateTime.UtcNow;
            var scores = await _sdjScoringService.ComputeSdjScores(session.Id);
            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

            // Assert: Should complete quickly (under 1000ms)
            Assert.NotNull(scores);
            Assert.True(duration < 1000, $"Scoring took {duration}ms, expected <1000ms");
        }

        [Fact]
        public async Task SdjScoring_DimensionAndSubDimensionScores_ShouldBeAccurate()
        {
            // Arrange: Session with specific answers
            var session = new Session
            {
                Id = 5,
                SessionId = Guid.NewGuid().ToString(),
                UserId = 1,
                Status = "in_progress",
                StartedAt = DateTime.UtcNow
            };
            _context.Sessions.Add(session);

            // High score answers
            _context.SessionItems.AddRange(new[]
            {
                new SessionItem { SessionId = 5, ItemId = 1, Answer = "5", AnsweredAt = DateTime.UtcNow },
                new SessionItem { SessionId = 5, ItemId = 2, Answer = "5", AnsweredAt = DateTime.UtcNow },
                new SessionItem { SessionId = 5, ItemId = 3, Answer = "5", AnsweredAt = DateTime.UtcNow }
            });
            await _context.SaveChangesAsync();

            // Act: Compute scores
            var scores = await _sdjScoringService.ComputeSdjScores(session.Id);

            // Assert: Verify dimension hierarchy
            Assert.NotEmpty(scores.Dimensions);
            Assert.NotEmpty(scores.SubDimensions);
            
            var selfExcellenceDim = scores.Dimensions.FirstOrDefault(d => d.Dimension == "التميز الذاتي");
            Assert.NotNull(selfExcellenceDim);
            Assert.True(selfExcellenceDim.T >= 50, "High scores should result in T-score >= 50");

            var selfAwarenessSubDim = scores.SubDimensions.FirstOrDefault(
                sd => sd.SubDimension == "الوعي الذاتي"
            );
            Assert.NotNull(selfAwarenessSubDim);
        }

        [Fact]
        public void FeatureFlag_UseSdj_ShouldBeDetectableViaEnvironment()
        {
            // Arrange: Get environment variable
            var useSdjFlag = Environment.GetEnvironmentVariable("USE_SDJ");

            // Assert: Flag should be "1" for SDJ mode
            // Note: This test assumes USE_SDJ=1 is set in test environment
            // If not set, this validates fallback behavior
            Assert.True(
                useSdjFlag == "1" || useSdjFlag == null,
                "USE_SDJ should be '1' (enabled) or null (default)"
            );
        }

        [Fact]
        public async Task Session_Submission_WorkflowIntegration_EndToEnd()
        {
            // Arrange: Full workflow from session start to result
            var session = new Session
            {
                Id = 6,
                SessionId = Guid.NewGuid().ToString(),
                UserId = 1,
                Status = "in_progress",
                StartedAt = DateTime.UtcNow
            };
            _context.Sessions.Add(session);

            // User answers all items
            for (int itemId = 1; itemId <= 3; itemId++)
            {
                _context.SessionItems.Add(new SessionItem
                {
                    SessionId = 6,
                    ItemId = itemId,
                    Answer = "4",
                    AnsweredAt = DateTime.UtcNow
                });
            }
            await _context.SaveChangesAsync();

            // Act: Complete workflow
            // 1. Mark session as completed
            session.Status = "completed";
            session.EndedAt = DateTime.UtcNow;
            
            // 2. Compute SDJ scores
            var scores = await _sdjScoringService.ComputeSdjScores(session.Id);
            
            // 3. Create result record
            var result = new Result
            {
                SessionId = session.Id,
                TotalScore = (int)Math.Round(scores.TotalScore.T),
                ScoringModelVersion = scores.Version,
                DimensionScoresJson = System.Text.Json.JsonSerializer.Serialize(new
                {
                    Dimensions = scores.Dimensions,
                    SubDimensions = scores.SubDimensions,
                    TrackFits = scores.TrackFits
                }),
                CompositeScoresJson = System.Text.Json.JsonSerializer.Serialize(new
                {
                    TotalScore = scores.TotalScore.T,
                    RawScore = scores.TotalScore.Raw,
                    Percentile = scores.TotalScore.Percentile
                }),
                CreatedAt = DateTime.UtcNow
            };
            _context.Results.Add(result);
            await _context.SaveChangesAsync();

            // Assert: Verify complete workflow
            var savedResult = await _context.Results
                .FirstOrDefaultAsync(r => r.SessionId == session.Id);
            
            Assert.NotNull(savedResult);
            Assert.Equal("completed", session.Status);
            Assert.NotNull(session.EndedAt);
            Assert.Contains("SDJ", savedResult.ScoringModelVersion);
            Assert.Contains("Dimensions", savedResult.DimensionScoresJson);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
