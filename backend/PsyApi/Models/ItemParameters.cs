using System.ComponentModel.DataAnnotations;

namespace PsyApi.Models
{
    public class ItemParameters
    {
        public int Id { get; set; }

        [Required]
        public int ItemId { get; set; }

        [Required]
        [StringLength(16)]
        public string ModelType { get; set; } = string.Empty;

        public double? A { get; set; }

        public double? B { get; set; }

        public double? C { get; set; }

        public string? ThresholdsJson { get; set; }

        public string? PcmStepsJson { get; set; }

        public double? TimeAlpha { get; set; }

        public double? TimeBeta { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public Item Item { get; set; } = null!;
    }
}