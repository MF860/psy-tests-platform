using PsyApi.Models;

namespace PsyApi.Services.Reports
{
    using DimensionScoreDto = PsyApi.Models.DimensionScore;

    public interface IPdfReportService
    {
        Task<byte[]> RenderResultPdfAsync(Result result, User user, IEnumerable<DimensionScoreDto> dimensions, CancellationToken ct = default);
        Task<byte[]> RenderSdjResultPdfAsync(Result result, User user, CancellationToken ct = default);
    }
}
