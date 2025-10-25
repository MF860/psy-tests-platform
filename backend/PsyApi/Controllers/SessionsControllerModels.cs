using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using PsyApi.Controllers.Json;

namespace PsyApi.Controllers
{
    public class StartSessionRequest
    {
        [Required]
        public string NationalId { get; set; } = string.Empty;
        public string? ClientId { get; set; }
        public string? UserAgent { get; set; }
    }

    public class SubmitAnswerRequest
    {
        [Required]
        public string ItemId { get; set; } = string.Empty;
        public string? Answer { get; set; }
        [JsonConverter(typeof(LenientNullableDoubleConverter))]
        public double? NumericAnswer { get; set; }
        public int? ResponseTimeMs { get; set; }
    }

    public class QuestionResponse
    {
        [JsonPropertyName("id")]
        public int Id { get; init; }

        [JsonPropertyName("item_id")]
        public string ItemId { get; init; } = string.Empty;

        [JsonPropertyName("text_ar")]
        public string TextAr { get; init; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; init; } = string.Empty;

        [JsonPropertyName("dimension_tags")]
        public string DimensionTags { get; init; } = string.Empty;

        [JsonPropertyName("difficulty")]
        public int Difficulty { get; init; }

        [JsonPropertyName("time_limit_seconds")]
        public int TimeLimitSeconds { get; init; }

        [JsonPropertyName("max_score")]
        public int MaxScore { get; init; }

        [JsonPropertyName("options")]
        public string? Options { get; init; }
    }
}