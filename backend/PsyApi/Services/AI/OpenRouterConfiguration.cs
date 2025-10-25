namespace PsyApi.Services.AI
{
    /// <summary>
    /// Configuration options for OpenRouter API integration
    /// </summary>
    public class OpenRouterConfiguration
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "deepseek/deepseek-chat";
        public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1";
        public int MaxTokens { get; set; } = 800;
        public double Temperature { get; set; } = 0.2;
        public int MaxRetries { get; set; } = 3;
        public int TimeoutSeconds { get; set; } = 15;
        public int CacheDurationHours { get; set; } = 24;
        public string? HttpReferer { get; set; }
        public string? XTitle { get; set; } = "Psy Tests Admin";
    }
}