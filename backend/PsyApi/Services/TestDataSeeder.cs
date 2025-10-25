using Microsoft.EntityFrameworkCore;
using PsyApi.Data;
using PsyApi.Models;
using System.Text.Json;
using System.Text;

namespace PsyApi.Services
{
    public class TestDataSeeder
    {
        private readonly AppDbContext _context;
        private readonly Serilog.ILogger _logger;

        public TestDataSeeder(AppDbContext context, Serilog.ILogger logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedTestSessionsAndResultsAsync()
        {
            // Check if we already have test data
            if (await _context.Sessions.AnyAsync(s => s.User.NationalId.StartsWith("test")))
            {
                _logger.Information("Test sessions already exist. Skipping seeding test data.");
                return;
            }

            try
            {
                // Create test users
                var testUsers = new List<User>
                {
                    new User { NationalId = "test123456789", FullName = "مستخدم تجريبي 1", CreatedAt = DateTime.UtcNow },
                    new User { NationalId = "test987654321", FullName = "مستخدم تجريبي 2", CreatedAt = DateTime.UtcNow },
                    new User { NationalId = "test456789123", FullName = "مستخدم تجريبي 3", CreatedAt = DateTime.UtcNow },
                    new User { NationalId = "test789123456", FullName = "مستخدم تجريبي 4", CreatedAt = DateTime.UtcNow },
                    new User { NationalId = "test321654987", FullName = "مستخدم تجريبي 5", CreatedAt = DateTime.UtcNow }
                };

                await _context.Users.AddRangeAsync(testUsers);
                await _context.SaveChangesAsync();

                // Get some items for the sessions
                var items = await _context.Items.Take(20).ToListAsync();
                if (items.Count == 0)
                {
                    _logger.Warning("No items found in database. Cannot create test sessions.");
                    return;
                }

                // Create test sessions and results
                var random = new Random();
                var testSessions = new List<Session>();
                var testResults = new List<Result>();
                var testSessionItems = new List<SessionItem>();

                foreach (var user in testUsers)
                {
                    // Create a session for each user
                    var session = new Session
                    {
                        UserId = user.Id,
                        Status = "Completed",
                        StartedAt = DateTime.UtcNow.AddDays(-random.Next(1, 30)),
                        EndedAt = DateTime.UtcNow.AddDays(-random.Next(0, 29))
                    };
                    testSessions.Add(session);

                    // Create session items
                    var sessionItemsForSession = new List<SessionItem>();
                    foreach (var item in items)
                    {
                        var isCorrect = random.NextDouble() > 0.3; // 70% chance of being correct
                        var responseTimeMs = random.Next(1000, 30000); // Random response time between 1-30 seconds

                        sessionItemsForSession.Add(new SessionItem
                        {
                            SessionId = session.Id,
                            ItemId = item.Id,
                            Answer = "Test answer",
                            RawCorrect = isCorrect,
                            ResponseTimeMs = responseTimeMs
                        });
                    }
                    testSessionItems.AddRange(sessionItemsForSession);

                    // Create dimension scores
                    var dimensionScores = new List<DimensionScore>
                    {
                        new DimensionScore { Dimension = "Anxiety", Raw = random.Next(1, 5), Z = random.NextDouble() * 2 - 1, T = random.Next(30, 70), Percentile = random.Next(1, 100) },
                        new DimensionScore { Dimension = "Depression", Raw = random.Next(1, 5), Z = random.NextDouble() * 2 - 1, T = random.Next(30, 70), Percentile = random.Next(1, 100) },
                        new DimensionScore { Dimension = "Stress", Raw = random.Next(1, 5), Z = random.NextDouble() * 2 - 1, T = random.Next(30, 70), Percentile = random.Next(1, 100) }
                    };

                    // Create a result for the session
                    var totalScore = random.Next(20, 100);
                    var result = new Result
                    {
                        SessionId = session.Id,
                        TotalScore = totalScore,
                        DimensionScoresJson = JsonSerializer.Serialize(dimensionScores),
                        ScoringModelVersion = "1.0.0"
                    };
                    testResults.Add(result);
                }

                // Add all the test data to the context
                await _context.Sessions.AddRangeAsync(testSessions);
                await _context.Results.AddRangeAsync(testResults);
                await _context.SessionItems.AddRangeAsync(testSessionItems);
                await _context.SaveChangesAsync();

                _logger.Information("Successfully seeded {Count} test sessions with results.", testSessions.Count);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "An error occurred while seeding test sessions and results.");
                throw;
            }
        }
    }
}
