using System.ComponentModel.DataAnnotations;

namespace PsyApi.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Action { get; set; } = string.Empty;

        public int? UserId { get; set; }

        [StringLength(64)]
        public string? IpAddress { get; set; }

        public string? Details { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public User? User { get; set; }
    }
}
