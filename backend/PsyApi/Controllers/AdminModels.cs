namespace PsyApi.Controllers
{
    public class AdminResultListItem
    {
        public int ResultId { get; set; }
        public int SessionId { get; set; }
        public string SessionGuid { get; set; } = string.Empty;
        public string NationalId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int TotalScore { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AdminResultDetail
    {
        public int ResultId { get; set; }
        public int SessionId { get; set; }
        public string SessionGuid { get; set; } = string.Empty;
        public string NationalId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int TotalScore { get; set; }
        public string ScoringModelVersion { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<PsyApi.Models.DimensionScore> Dimensions { get; set; } = new();
    }

    public class AdminLoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class AdminLoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }

    public class ChangePasswordRequest
    {
        public string OldPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class ChangePasswordResponse
    {
        public string Message { get; set; } = string.Empty;
    }

    public class ResultQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Sort { get; set; }
        public string? Dir { get; set; }
        public string? Search { get; set; }
    }

    public class AuditQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Search { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }

    public class PagedResponse<T>
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public List<T> Data { get; set; } = new();
    }

    public class AuditLogDto
    {
        public int Id { get; set; }
        public string Action { get; set; } = string.Empty;
        public string AdminUsername { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}