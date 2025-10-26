using System.ComponentModel.DataAnnotations;

namespace PsyApi.Models
{
    public class Item
    {
        public int Id { get; set; }

        [Required]
        [StringLength(10)]
        public string ItemCode { get; set; } = string.Empty; // E.g. "I001"

        [Required]
        public string TextAr { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string Type { get; set; } = string.Empty; // LIKERT, LIKERTAGREEMENT, MCQ, TIMED_NUMERIC, TEXT, ORDERING, FREQUENCY

    [Required]
    public string DimensionTags { get; set; } = string.Empty; // Comma-separated tags (DEPRECATED: Use Dimension + SubDimension)

    // NEW SDJ FIELDS
    [StringLength(100)]
    public string? Dimension { get; set; } // Parent dimension (e.g., "التميز الذاتي")
    
    [StringLength(100)]
    public string? SubDimension { get; set; } // Sub-dimension (e.g., "الوعي الذاتي")
    
    public bool Reverse { get; set; } = false; // Reverse scoring flag

    [Required]
    [Range(1, 5)]
    public int Difficulty { get; set; }        [Required]
        [Range(1, 300)]
        public int TimeLimitSeconds { get; set; }

        [Required]
        [Range(1, 10)]
        public int MaxScore { get; set; }

        public string? CorrectAnswer { get; set; } // For MCQ or numeric questions
        
        public string? Options { get; set; } // For Likert, Frequency, and other option-based questions

        // Navigation properties
        public ICollection<SessionItem> SessionItems { get; set; } = new List<SessionItem>();
    }
}
