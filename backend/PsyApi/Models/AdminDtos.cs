using System.ComponentModel.DataAnnotations;
using DimensionScoreDto = PsyApi.Models.DimensionScore;

namespace PsyApi.Models
{
    public class AdminLoginRequest
    {
        [Required]
        public string Username { get; set; } = string.Empty;
        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class AdminLoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }

    public class AdminResultListItem
    {
        public int ResultId { get; set; }
        public int SessionId { get; set; }
        public string NationalId { get; set; } = string.Empty;
        public string FullName { get; set; } = "N/A";
        public double TotalScore { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AdminResultDetail
    {
        public int ResultId { get; set; }
        public int SessionId { get; set; }
        public string NationalId { get; set; } = string.Empty;
        public string FullName { get; set; } = "N/A";
        public double TotalScore { get; set; }
        public string ScoringModelVersion { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<DimensionScoreDto> Dimensions { get; set; } = new();
    }

    public class ResultQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public string? Search { get; set; }
        public string? Sort { get; set; }
        public string? Dir { get; set; }
    }

    public class PagedResponse<T>
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public List<T> Data { get; set; } = new();
    }
}

