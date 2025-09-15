using psy_tests_platform.Models;

namespace psy_tests_platform.Services.Scoring
{
    public interface IScoringService
    {
        Task<ScoreSummary> ComputeSessionScores(int sessionId);
    }
}
