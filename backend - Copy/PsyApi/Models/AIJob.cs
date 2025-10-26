using System.ComponentModel.DataAnnotations;

namespace PsyApi.Models
{
    public class AIJob
    {
        public int Id { get; set; }

        [Required]
        public int ResultId { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Processing, Completed, Failed

        public string? Response { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Result Result { get; set; } = null!;
    }
}
