using System.ComponentModel.DataAnnotations;

namespace PsyApi.Models
{
    public class Item
    {
        public int Id { get; set; }

        [Required]
        public string TextAr { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Type { get; set; } = string.Empty; // LIKERT, MCQ, TIMED_NUMERIC, TEXT, ORDERING

        [Required]
        public string DimensionTags { get; set; } = string.Empty; // Comma-separated tags

        [Required]
        [Range(1, 5)]
        public int Difficulty { get; set; }

        [Required]
        [Range(1, 300)]
        public int TimeLimitSeconds { get; set; }

        [Required]
        [Range(1, 10)]
        public int MaxScore { get; set; }

        public string? CorrectAnswer { get; set; } // For MCQ or numeric questions

        // Navigation properties
        public ICollection<SessionItem> SessionItems { get; set; } = new List<SessionItem>();
    }
}
