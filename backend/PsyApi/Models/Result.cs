using System.ComponentModel.DataAnnotations;

namespace PsyApi.Models
{
    public class Result
    {
        public int Id { get; set; }

        [Required]
        public int SessionId { get; set; }

        [Required]
        public int TotalScore { get; set; }

        [StringLength(255)]
        public string? PdfPath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Session Session { get; set; } = null!;
        public ICollection<AIJob> AIJobs { get; set; } = new List<AIJob>();
    }
}
