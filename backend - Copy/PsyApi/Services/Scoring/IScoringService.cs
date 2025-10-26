using PsyApi.Models;

namespace PsyApi.Services.Scoring
{
    public interface IScoringService
    {
        Task<EnhancedScoreSummary> ComputeSessionScores(int sessionId);
    }
}

