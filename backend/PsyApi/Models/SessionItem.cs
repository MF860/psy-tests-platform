using System.ComponentModel.DataAnnotations;

namespace PsyApi.Models
{
    public class SessionItem
    {
        public int Id { get; set; }

        [Required]
        public int SessionId { get; set; }

        [Required]
        public int ItemId { get; set; }

        public string? Answer { get; set; }

        public int? Score { get; set; }

        public DateTime? AnsweredAt { get; set; }

        // Navigation properties
        public Session Session { get; set; } = null!;
        public Item Item { get; set; } = null!;
    }
}
