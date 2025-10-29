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

    // SDJ V1 FIELDS (existing)
    [StringLength(100)]
    public string? Dimension { get; set; } // Parent dimension (e.g., "التميز الذاتي")
    
    [StringLength(100)]
    public string? SubDimension { get; set; } // Sub-dimension (e.g., "الوعي الذاتي")
    
    public bool Reverse { get; set; } = false; // Reverse scoring flag

    // SDJ V2 SEVEN PATTERNS FIELDS (new)
    [StringLength(10)]
    public string? PatternId { get; set; } // Pattern ID (e.g., "P1")
    
    [StringLength(50)]
    public string? PatternKey { get; set; } // Pattern key (e.g., "personality_patterns")
    
    [StringLength(100)]
    public string? PatternNameAr { get; set; } // Pattern Arabic name (e.g., "الأنماط الشخصية")
    
    [StringLength(10)]
    public string? SubId { get; set; } // Sub-dimension ID (e.g., "P1_S1")
    
    [StringLength(50)]
    public string? SubKey { get; set; } // Sub-dimension key (e.g., "mbti")
    
    [StringLength(100)]
    public string? SubNameAr { get; set; } // Sub-dimension Arabic name (e.g., "MBTI")

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
