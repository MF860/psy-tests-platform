namespace PsyApi.Services.AI
{
    /// <summary>
    /// Configuration options for DeepSeek API integration
    /// Now using DeepSeek directly (not via OpenRouter)
    /// </summary>
    public class DeepSeekConfiguration
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "deepseek-chat";
        public string BaseUrl { get; set; } = "https://api.deepseek.com/v1";
        public int MaxTokens { get; set; } = 1500;
        public double Temperature { get; set; } = 0.2;
        public int MaxRetries { get; set; } = 3;
        public int TimeoutSeconds { get; set; } = 30;
        public int CacheDurationHours { get; set; } = 24;
    }
    
    /// <summary>
    /// Legacy configuration name kept for backward compatibility
    /// </summary>
    public class OpenRouterConfiguration : DeepSeekConfiguration
    {
    }
}