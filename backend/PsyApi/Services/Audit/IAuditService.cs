using System.Threading.Tasks;

namespace PsyApi.Services.Audit
{
    public interface IAuditService
    {
        Task LogAsync(int? adminId, string action, string details, string? ipAddress);
    }
}

