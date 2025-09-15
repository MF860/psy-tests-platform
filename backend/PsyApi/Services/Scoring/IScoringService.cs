using PsyApi.Services.Scoring.Models;

namespace PsyApi.Services.Scoring
{
    public interface IScoringService
    {
        Task<ScoreSummary> ComputeSessionScores(int sessionId);
    }
}

