using System.ComponentModel.DataAnnotations;

namespace PsyApi.Models
{
    public class Session
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        public DateTime? EndedAt { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "InProgress"; // InProgress, Completed, Abandoned

        // Navigation properties
        public User User { get; set; } = null!;
        public ICollection<SessionItem> SessionItems { get; set; } = new List<SessionItem>();
        public Result? Result { get; set; }
    }
}
