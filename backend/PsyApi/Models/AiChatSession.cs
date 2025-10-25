using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PsyApi.Models
{
    /// <summary>
    /// Represents an AI chat session for result analysis and interpretation
    /// </summary>
    public class AiChatSession
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Unique identifier for the chat session
        /// </summary>
        [Required]
        [StringLength(36)]
        public string SessionId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// User who initiated the chat (optional for anonymous sessions)
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// Result being analyzed in this chat session
        /// </summary>
        public int? ResultId { get; set; }

        /// <summary>
        /// JSON array of messages in the chat
        /// Format: [{"role": "user|assistant", "message": "content", "timestamp": "ISO8601"}]
        /// </summary>
        [Column(TypeName = "text")]
        public string MessagesJson { get; set; } = "[]";

        /// <summary>
        /// Chat session title (auto-generated from first message)
        /// </summary>
        [StringLength(200)]
        public string? Title { get; set; }

        /// <summary>
        /// Chat session status
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Active";

        /// <summary>
        /// Total number of messages in the session
        /// </summary>
        public int MessageCount { get; set; } = 0;

        /// <summary>
        /// Last activity timestamp
        /// </summary>
        public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the chat session was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual User? User { get; set; }
        public virtual Result? Result { get; set; }
    }

    /// <summary>
    /// Chat message DTO for JSON serialization
    /// </summary>
    public class ChatMessage
    {
        public string Role { get; set; } = string.Empty; // "user" or "assistant"
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? MessageId { get; set; } = Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Chat session status enumeration
    /// </summary>
    public static class ChatSessionStatus
    {
        public const string Active = "Active";
        public const string Archived = "Archived";
        public const string Deleted = "Deleted";
    }
}