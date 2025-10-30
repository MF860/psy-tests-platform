using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using PsyApi.Middleware;
using Microsoft.EntityFrameworkCore;
using PsyApi.Data;
using PsyApi.Services;
using PsyApi.Services.Scoring;
using PsyApi.Services.Reports;
using PsyApi.Services.AI;
using PsyApi.Security;
using PsyApi.Services.Audit;
using PsyApi.Services.Testing;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IO.Compression;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Set server URL with env override
var urlsEnv = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
if (!string.IsNullOrWhiteSpace(urlsEnv))
{
    builder.WebHost.UseUrls(urlsEnv);
}   
else
{
    builder.WebHost.UseUrls("http://localhost:5019");
}

// Add services to the container.
// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.Converters.Insert(0, new PsyApi.Controllers.Json.LenientNullableDoubleConverter());
});
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
{
    // Enable automatic 400 with our custom factory
    options.SuppressModelStateInvalidFilter = false;
    // Provide a clear 400 message for TIMED_NUMERIC invalid numeric payloads
    options.InvalidModelStateResponseFactory = context =>
    {
        try
        {
            var cad = context.ActionDescriptor as Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor;
            if (cad != null && string.Equals(cad.ControllerName, "Sessions", StringComparison.OrdinalIgnoreCase)
                && string.Equals(cad.ActionName, "SubmitAnswer", StringComparison.OrdinalIgnoreCase))
            {
                return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(new { error = "TIMED_NUMERIC requires a numeric value." });
            }
        }
        catch { }
        return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(context.ModelState);
    };
});

// Use lenient double? converter so invalid numeric strings bind as null (controller will validate)
builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.Converters.Insert(0, new PsyApi.Controllers.Json.LenientNullableDoubleConverter());
});

// Response Compression
builder.Services.AddResponseCompression(o =>
{
    o.EnableForHttps = true;
    o.Providers.Add<BrotliCompressionProvider>();
    o.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);

// CORS for Admin UI and User UI - cloud-ready with Vercel support
var corsAllowedOriginsEnv = builder.Configuration["CORS_ALLOWED_ORIGINS"] ?? 
                              Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS");

builder.Services.AddCors(o =>
{
    o.AddPolicy("AdminCors", p =>
    {
        var origins = new List<string> 
        { 
            // Local development origins
            "http://localhost:5173", "http://localhost:5174", "http://localhost:3000",
            "https://localhost:5173", "https://localhost:5174",
            "http://localhost:5175", "http://localhost:5176", "http://localhost:4173",
            "https://localhost:5175", "https://localhost:5176",
            
            // Production Vercel deployments (always included)
            "https://admin-ui-lyart-nu.vercel.app",
            "https://psy-tests-platform.vercel.app"
        };
        
        // Add additional production origins from environment variable
        if (!string.IsNullOrWhiteSpace(corsAllowedOriginsEnv))
        {
            var prodOrigins = corsAllowedOriginsEnv
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(o => !string.IsNullOrWhiteSpace(o));
            origins.AddRange(prodOrigins);
            Log.Information("CORS: Added {Count} custom production origins from env", prodOrigins.Count());
        }
        
        Log.Information("CORS: Total configured origins: {Count}", origins.Count);
        foreach (var origin in origins)
        {
            Log.Information("  - {Origin}", origin);
        }
        
        p.WithOrigins(origins.ToArray())
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .WithExposedHeaders("Content-Disposition", "ETag", "Cache-Control");
    });
    
    o.AddPolicy("UserCors", p =>
    {
        var origins = new List<string> 
        { 
            "http://localhost:5175", "http://localhost:5176", "http://localhost:4173",
            "https://localhost:5175", "https://localhost:5176",
            
            // Production Vercel deployments
            "https://psy-tests-platform.vercel.app"
        };
        
        // Add production origins from environment variable (same as Admin)
        if (!string.IsNullOrWhiteSpace(corsAllowedOriginsEnv))
        {
            var prodOrigins = corsAllowedOriginsEnv
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(o => !string.IsNullOrWhiteSpace(o));
            origins.AddRange(prodOrigins);
        }
        
        p.WithOrigins(origins.ToArray())
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .WithExposedHeaders("Content-Disposition", "ETag", "Cache-Control");
    });
});

// Add DbContext (Auto-detect SQLite or Postgres based on connection string and environment)
var useSqliteEnv = Environment.GetEnvironmentVariable("USE_SQLITE")?.Trim();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Debug logging
Log.Information("USE_SQLITE environment variable: '{UseSqlite}'", useSqliteEnv ?? "null");
Log.Information("Default connection string: '{ConnectionString}'", connectionString ?? "null");

// Determine database type
bool useSqlite = false;
if (!string.IsNullOrWhiteSpace(useSqliteEnv) && useSqliteEnv == "1")
{
    useSqlite = true;
    Log.Information("Using SQLite due to USE_SQLITE=1 environment variable");
}
else if (!string.IsNullOrWhiteSpace(connectionString) && connectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
{
    useSqlite = true;
    Log.Information("Auto-detected SQLite from connection string format");
}
else if (!string.IsNullOrWhiteSpace(connectionString) && (connectionString.Contains("Host=") || connectionString.Contains("Server=")))
{
    useSqlite = false;
    Log.Information("Auto-detected PostgreSQL from connection string format");
}
else
{
    // Default to SQLite for development
    useSqlite = true;
    Log.Warning("Could not determine database type. Defaulting to SQLite for development.");
}

if (useSqlite)
{
    var sqlitePath = Environment.GetEnvironmentVariable("SQLITE_PATH") ?? "psy_dev.db";
    if (!string.IsNullOrWhiteSpace(connectionString) && connectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
    {
        // Use connection string as-is if it's already SQLite format
        Log.Information("Using SQLite with connection string from configuration: {ConnectionString}", connectionString);
        builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
    }
    else
    {
        // Use environment variable path
        var sqliteConnectionString = $"Data Source={sqlitePath}";
        Log.Information("Using SQLite with path: {Path}", sqlitePath);
        builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(sqliteConnectionString));
    }
}
else
{
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
    }
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        connectionString = "Host=localhost;Database=dev;Username=dev;Password=dev";
        Log.Warning("Connection string 'DefaultConnection' missing. Using fallback local Postgres connection string. Set ConnectionStrings__DefaultConnection to override.");
    }
    Log.Information("Using PostgreSQL with connection string: {ConnectionString}", connectionString);
    builder.Services.AddDbContext<AppDbContext>(options =>
    {
        options.UseNpgsql(connectionString);
        // Suppress pending model changes warning during initial migration
        options.ConfigureWarnings(warnings =>
            warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    });
}

// Add DataSeeder service with Serilog.ILogger
builder.Services.AddScoped<DataSeeder>(provider => 
    new DataSeeder(
        provider.GetRequiredService<AppDbContext>(),
        Log.ForContext<DataSeeder>()
    )
);

// Add ScoringService as scoped
builder.Services.AddScoped<IScoringService>(provider =>
    new ScoringService(
        provider.GetRequiredService<AppDbContext>(),
        Log.ForContext<ScoringService>()
    )
);

// SDJ Scoring Service (registered conditionally, null if not needed)
builder.Services.AddScoped<ISdjScoringService>(provider =>
    new SdjScoringService(
        provider.GetRequiredService<AppDbContext>(),
        provider.GetRequiredService<ILogger<SdjScoringService>>()
    )
);

// SDJ V2 Scoring Service (Seven Patterns with MCQ support)
builder.Services.AddScoped<ISdjV2ScoringService>(provider =>
    new SdjV2ScoringService(
        provider.GetRequiredService<AppDbContext>(),
        provider.GetRequiredService<ILogger<SdjV2ScoringService>>()
    )
);

// Reports - Ultimate Arabic PDF Service (v3.0)
builder.Services.AddScoped<IPdfReportService, UltimateArabicPdfReportService>();
// Legacy service still available if needed
builder.Services.AddScoped<ModernPdfReportService>();

// Recommendations
builder.Services.AddScoped<IRecommendationService, RecommendationService>();

// AI Analyzer (now using DeepSeek direct API)
builder.Services.AddScoped<IAiAnalyzerService, AiAnalyzerService>();

// DeepSeek AI Analyzer Configuration
builder.Services.Configure<PsyApi.Services.AI.DeepSeekConfiguration>(config =>
{
    // CRITICAL: Read API key from environment ONLY - never from appsettings
    config.ApiKey = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY") ?? 
                   Environment.GetEnvironmentVariable("OPENROUTER_API_KEY") ?? // Backward compatibility
                   string.Empty; // Empty if not set - will fallback to rules-based
    
    config.Model = Environment.GetEnvironmentVariable("DEEPSEEK_MODEL") ?? 
                   "deepseek-chat";
    
    config.MaxTokens = int.TryParse(Environment.GetEnvironmentVariable("DEEPSEEK_MAX_TOKENS"), out int maxTokens) 
                       ? maxTokens 
                       : 1500;
    
    config.Temperature = double.TryParse(Environment.GetEnvironmentVariable("DEEPSEEK_TEMPERATURE"), out double temp)
                         ? temp
                         : 0.2;
    
    config.BaseUrl = Environment.GetEnvironmentVariable("DEEPSEEK_BASE_URL") ?? 
                     "https://api.deepseek.com/v1";
    
    config.TimeoutSeconds = 30;
    config.MaxRetries = 3;
    config.CacheDurationHours = 24;
    
    var hasKey = !string.IsNullOrWhiteSpace(config.ApiKey);
    Log.Information("DeepSeek AI configured - Model: {Model}, MaxTokens: {MaxTokens}, Temperature: {Temperature}, API Key: {HasKey}", 
        config.Model, config.MaxTokens, config.Temperature, hasKey ? "✓ Set" : "✗ Not Set (will use fallback)");
});

// Legacy OpenRouterConfiguration for backward compatibility
builder.Services.Configure<PsyApi.Services.AI.OpenRouterConfiguration>(config =>
{
    config.ApiKey = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY") ?? 
                   Environment.GetEnvironmentVariable("OPENROUTER_API_KEY") ?? 
                   string.Empty;
    config.Model = Environment.GetEnvironmentVariable("DEEPSEEK_MODEL") ?? "deepseek-chat";
    config.MaxTokens = 1500;
    config.Temperature = 0.2;
    config.BaseUrl = Environment.GetEnvironmentVariable("DEEPSEEK_BASE_URL") ?? "https://api.deepseek.com/v1";
    config.TimeoutSeconds = 30;
    config.MaxRetries = 3;
    config.CacheDurationHours = 24;
});

// Register DeepSeek HTTP client with proper configuration
builder.Services.AddHttpClient<IDeepSeekClient, DeepSeekClient>()
    .SetHandlerLifetime(TimeSpan.FromMinutes(5))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
    });

// OpenAI Recommendations Service (keeping existing)
builder.Services.Configure<PsyApi.Services.AI.OpenAIConfiguration>(config =>
{
    config.ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? 
                   builder.Configuration["OpenAI:ApiKey"] ?? 
                   throw new InvalidOperationException("OpenAI API Key is required. Set OPENAI_API_KEY environment variable or OpenAI:ApiKey in configuration.");
    
    config.Model = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? 
                   builder.Configuration["OpenAI:Model"] ?? 
                   "gpt-4o-mini";
    
    config.MaxTokens = int.TryParse(Environment.GetEnvironmentVariable("OPENAI_MAX_TOKENS"), out int maxTokens) 
                       ? maxTokens 
                       : builder.Configuration.GetValue<int>("OpenAI:MaxTokens", 2000);
    
    config.Temperature = double.TryParse(Environment.GetEnvironmentVariable("OPENAI_TEMPERATURE"), out double temp)
                         ? temp
                         : builder.Configuration.GetValue<double>("OpenAI:Temperature", 0.3);
    
    config.BaseUrl = Environment.GetEnvironmentVariable("OPENAI_BASE_URL") ?? 
                     builder.Configuration["OpenAI:BaseUrl"] ?? 
                     "https://api.openai.com/v1";
    
    config.TimeoutSeconds = builder.Configuration.GetValue<int>("OpenAI:TimeoutSeconds", 30);
    config.MaxRetries = builder.Configuration.GetValue<int>("OpenAI:MaxRetries", 3);
    config.CacheDurationHours = builder.Configuration.GetValue<int>("OpenAI:CacheDurationHours", 24);
    config.EnableCaching = builder.Configuration.GetValue<bool>("OpenAI:EnableCaching", true);
    
    Log.Information("OpenAI configured with model: {Model}, max tokens: {MaxTokens}, temperature: {Temperature}", 
        config.Model, config.MaxTokens, config.Temperature);
});

// Add HttpClient for OpenRouter AI Analysis
builder.Services.AddHttpClient<PsyApi.Services.AI.IOpenRouterClient, PsyApi.Services.AI.OpenRouterClient>(client =>
{
    // BaseAddress and headers will be set by the OpenRouterClient using configuration
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.Add("User-Agent", "PsyAPI-AIAnalysis/1.0");
});

// Register OpenRouter client as scoped
builder.Services.AddScoped<PsyApi.Services.AI.IOpenRouterClient, PsyApi.Services.AI.OpenRouterClient>();

// Add HttpClient for OpenAI
builder.Services.AddHttpClient<PsyApi.Services.AI.IOpenAIRecommendationsService, PsyApi.Services.AI.OpenAIRecommendationsService>(client =>
{
    // BaseAddress will be set by the OpenAI service using configuration
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "PsyAPI-Recommendations/1.0");
});

// Register OpenAI service as scoped
builder.Services.AddScoped<PsyApi.Services.AI.IOpenAIRecommendationsService, PsyApi.Services.AI.OpenAIRecommendationsService>();

// Add memory cache for OpenAI responses
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = builder.Configuration.GetValue<long>("Cache:SizeLimit", 100); // 100 entries max
});

// Audit
builder.Services.AddScoped<IAuditService, AuditService>();

// Testing Services
builder.Services.AddScoped<SessionPersistenceTester>();

// JWT Options
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();

// AuthN/AuthZ
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, opt =>
    {
        opt.RequireHttpsMetadata = false;
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization(opt =>
{
    opt.AddPolicy("Admin", p => p.RequireAuthenticatedUser());
});

// Rate Limiting
builder.Services.AddRateLimiter(opt =>
{
    opt.AddFixedWindowLimiter("login", o =>
    {
        o.Window = TimeSpan.FromMinutes(1);
        o.PermitLimit = 5;
        o.QueueLimit = 0;
        o.AutoReplenishment = true;
    });
    opt.AddFixedWindowLimiter("admin", o =>
    {
        o.Window = TimeSpan.FromMinutes(1);
        o.PermitLimit = 60;
        o.QueueLimit = 0;
        o.AutoReplenishment = true;
    });
    opt.AddFixedWindowLimiter("api", o =>
    {
        o.Window = TimeSpan.FromMinutes(1);
        o.PermitLimit = 10;
        o.QueueLimit = 0;
        o.AutoReplenishment = true;
    });
});

var app = builder.Build();

// Register code pages for robust font support (QuestPDF)
Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseResponseCompression();
app.UseHttpsRedirection();

// CRITICAL FIX: Use single CORS policy for all routes to avoid middleware pipeline issues
// AdminCors already includes all origins from CORS_ALLOWED_ORIGINS
app.UseCors("AdminCors");

app.UseRateLimiting(); // Custom rate limiting for /api/sessions/start
app.UseRateLimiter(); // Built-in rate limiting for other endpoints
app.UseAuthentication();
app.UseAuthorization();

// Health check endpoint for cloud platforms (Render, etc.)
app.MapGet("/health", () => Results.Ok(new 
{ 
    status = "healthy", 
    timestamp = DateTime.UtcNow,
    environment = app.Environment.EnvironmentName,
    useSdj = Environment.GetEnvironmentVariable("USE_SDJ") != "0"
})).WithTags("Health");

app.MapControllers();

// Seed data on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var appDb = services.GetRequiredService<AppDbContext>();
    try
    {
        // For Neon/PostgreSQL: Use Migrate() to apply all migrations
        // This creates tables even if database already has other content
        Log.Information("Applying database migrations...");
        await appDb.Database.MigrateAsync();
        Log.Information("Database migrations completed successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Database initialization failed");
        
        // Provide helpful error message
        if (ex.Message.Contains("database") && ex.Message.Contains("does not exist"))
        {
            Log.Error("The database does not exist. Using 'postgres' database is recommended.");
            Log.Error("Current connection string uses: Database=postgres");
        }
        
        throw; // Fail fast if DB init fails
    }
    var dataSeeder = services.GetRequiredService<DataSeeder>();
    await dataSeeder.SeedItemsAsync(true);
    await dataSeeder.SeedItemParametersAsync(true);
    await dataSeeder.SeedMockUsersAsync(true);



    // Seed Admin (Development only)
    if (app.Environment.IsDevelopment())
    {
        var db = services.GetRequiredService<AppDbContext>();
        if (!await db.Admins.AnyAsync(a => a.Username == "root"))
        {
            var pass = Environment.GetEnvironmentVariable("ADMIN_SEED_PASSWORD") ?? "StrongAdmin!23!";
            var hash = PsyApi.Security.PasswordHasher.Hash(pass);
            db.Admins.Add(new PsyApi.Models.Admin { Username = "root", PasswordHash = hash, CreatedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();
            Log.Warning("Seeded development admin 'root'. Change password via env var ADMIN_SEED_PASSWORD.");
        }
    }
}

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}



