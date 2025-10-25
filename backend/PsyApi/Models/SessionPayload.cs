using System.Text.Json.Serialization;

namespace PsyApi.Models;

public class SessionPayload
{
    [JsonPropertyName("startedAtUtc")]
    public DateTime StartedAtUtc { get; set; }

    [JsonPropertyName("answers")]
    public List<SessionAnswer> Answers { get; set; } = new();
}

public class SessionAnswer
{
    [JsonPropertyName("itemId")]
    public string ItemId { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }

    [JsonPropertyName("ts")]
    public DateTime Timestamp { get; set; }
}