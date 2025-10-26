using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using PsyApi.Data;
using PsyApi.Models;
using PsyApi.Services.AI;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace PsyApi.Controllers
{
    [ApiController]
    [Route("api/recommendations")]
    public class RecommendationsController : ControllerBase
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = false
        };

        private readonly AppDbContext _db;
        private readonly ILogger<RecommendationsController> _logger;

        public RecommendationsController(AppDbContext db, ILogger<RecommendationsController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpPost("ai")]
        [EnableRateLimiting("api")]
        public async Task<IActionResult> GenerateAIRecommendations([FromBody] TestRecommendationsRequest request)
        {
            try
            {
                // Get OpenAI recommendations service
                var openAIService = HttpContext.RequestServices.GetService<PsyApi.Services.AI.IOpenAIRecommendationsService>();
                if (openAIService == null)
                {
                    _logger.LogWarning("OpenAI recommendations service not configured");
                    return StatusCode(503, new { error = "AI recommendations service unavailable" });
                }

                // Create dimension scores from personality scores and cognitive abilities
                var dimensionScores = new List<DimensionScore>();
                
                if (request.PersonalityScores != null)
                {
                    dimensionScores.AddRange(new[]
                    {
                        new DimensionScore { Dimension = "Openness", T = request.PersonalityScores.Openness * 100 },
                        new DimensionScore { Dimension = "Conscientiousness", T = request.PersonalityScores.Conscientiousness * 100 },
                        new DimensionScore { Dimension = "Extraversion", T = request.PersonalityScores.Extraversion * 100 },
                        new DimensionScore { Dimension = "Agreeableness", T = request.PersonalityScores.Agreeableness * 100 },
                        new DimensionScore { Dimension = "Neuroticism", T = request.PersonalityScores.Neuroticism * 100 }
                    });
                }

                if (request.CognitiveAbilities != null)
                {
                    dimensionScores.AddRange(new[]
                    {
                        new DimensionScore { Dimension = "VerbalReasoning", T = request.CognitiveAbilities.VerbalReasoning * 100 },
                        new DimensionScore { Dimension = "NumericalReasoning", T = request.CognitiveAbilities.NumericalReasoning * 100 },
                        new DimensionScore { Dimension = "AbstractReasoning", T = request.CognitiveAbilities.AbstractReasoning * 100 }
                    });
                }

                // Calculate total score as average of all dimension scores
                var totalScore = dimensionScores.Any() ? dimensionScores.Average(d => d.T) : 0;

                // Generate recommendations
                var participantId = $"test_user_{request.UserId}";
                var recommendations = await openAIService.GenerateRecommendationsAsync(
                    resultId: request.TestResultId ?? 0,
                    participantId: participantId,
                    dimensions: dimensionScores,
                    totalScore: totalScore,
                    context: request.Context,
                    forceRegenerate: true,
                    cancellationToken: HttpContext.RequestAborted);

                _logger.LogInformation("Generated AI recommendations for test user {UserId}", request.UserId);

                return Ok(recommendations);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Recommendations request cancelled");
                return StatusCode(408, new { error = "Request timeout" });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "OpenAI API error");
                return StatusCode(502, new { error = "External AI service error", details = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating recommendations");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }

    // Request model for test recommendations
    public class TestRecommendationsRequest
    {
        public int UserId { get; set; }
        public int? TestResultId { get; set; }
        public PersonalityScores? PersonalityScores { get; set; }
        public CognitiveAbilities? CognitiveAbilities { get; set; }
        public string? Context { get; set; }
    }

    public class PersonalityScores
    {
        public double Openness { get; set; }
        public double Conscientiousness { get; set; }
        public double Extraversion { get; set; }
        public double Agreeableness { get; set; }
        public double Neuroticism { get; set; }
    }

    public class CognitiveAbilities
    {
        public double VerbalReasoning { get; set; }
        public double NumericalReasoning { get; set; }
        public double AbstractReasoning { get; set; }
    }
}