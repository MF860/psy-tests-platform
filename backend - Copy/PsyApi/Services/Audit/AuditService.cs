using Microsoft.EntityFrameworkCore;
using PsyApi.Data;
using PsyApi.Models;

namespace PsyApi.Services.Audit
{
    public class AuditService : IAuditService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AuditService> _logger;

        public AuditService(AppDbContext context, ILogger<AuditService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task LogAsync(int? adminId, string action, string details, string? ipAddress)
        {
            try
            {
                var log = new AuditLog
                {
                    UserId = adminId,
                    Action = action,
                    Details = details,
                    IpAddress = ipAddress,
                    CreatedAt = DateTime.UtcNow
                };
                _context.AuditLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log audit event {Action}", action);
            }
        }
    }
}

