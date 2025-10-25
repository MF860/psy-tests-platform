using System.ComponentModel.DataAnnotations;

namespace PsyApi.Models
{
    public class Session
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public string SessionId { get; set; } = Guid.NewGuid().ToString("N");

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        public DateTime? EndedAt { get; set; }

        [Required]
        public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

        [Required]
        public int CurrentIndex { get; set; } = 0;

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "in_progress"; // in_progress, completed, abandoned

        public string? Payload { get; set; }

        // Navigation properties
        public User User { get; set; } = null!;
        public ICollection<SessionItem> SessionItems { get; set; } = new List<SessionItem>();
        public Result? Result { get; set; }
    }
}
