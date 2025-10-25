using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PsyApi.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string NationalId { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = "user";

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public bool IsActive { get; set; } = true;

        public DateTime? BirthDate { get; set; }
        public int? Age { get; set; }
        public string? Gender { get; set; }
        public string? City { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public string? Education { get; set; }
        public string? Occupation { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<Session> Sessions { get; set; } = new List<Session>();
    }
}




