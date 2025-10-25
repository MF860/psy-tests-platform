using System.Collections.Concurrent;
using System.Net;

namespace PsyApi.Middleware
{
    public class RateLimitingMiddleware
    {
        private static readonly ConcurrentDictionary<string, Queue<DateTime>> _requestTimes = new();
        private const int MaxRequests = 10;
        private static readonly TimeSpan TimeWindow = TimeSpan.FromMinutes(15);
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitingMiddleware> _logger;

        public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments("/api/sessions/start") && 
                context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                var ipAddress = GetIpAddress(context);
                var now = DateTime.UtcNow;

                // Get or create request queue for this IP
                var requests = _requestTimes.GetOrAdd(ipAddress, _ => new Queue<DateTime>());

                // Remove old requests outside the time window
                while (requests.Count > 0 && now - requests.Peek() > TimeWindow)
                {
                    requests.Dequeue();
                }

                // Check if we're over the limit
                if (requests.Count >= MaxRequests)
                {
                    _logger.LogWarning("Rate limit exceeded for IP {IpAddress}", ipAddress);
                    context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                    await context.Response.WriteAsJsonAsync(new { error = "Too many requests. Please try again later." });
                    return;
                }

                // Add current request
                requests.Enqueue(now);
            }

            await _next(context);
        }

        private static string GetIpAddress(HttpContext context)
        {
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            return !string.IsNullOrEmpty(forwardedFor) 
                ? forwardedFor.Split(',')[0].Trim() 
                : context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }

    public static class RateLimitingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RateLimitingMiddleware>();
        }
    }
}