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

// CORS for Admin UI and User UI
builder.Services.AddCors(o =>
{
    o.AddPolicy("AdminCors", p => p
        .WithOrigins("http://localhost:5173", "http://localhost:5174", "http://localhost:3000")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
    o.AddPolicy("UserCors", p => p
        .WithOrigins("http://localhost:5175", "http://localhost:5176", "http://localhost:4173")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
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
    builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
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

// Reports - Ultimate Arabic PDF Service (v3.0)
builder.Services.AddScoped<IPdfReportService, UltimateArabicPdfReportService>();
// Legacy service still available if needed
builder.Services.AddScoped<ModernPdfReportService>();

// Recommendations
builder.Services.AddScoped<IRecommendationService, RecommendationService>();

// AI Analyzer (now using OpenRouter)
builder.Services.AddScoped<IAiAnalyzerService, AiAnalyzerService>();

// OpenRouter AI Analyzer Configuration
builder.Services.Configure<PsyApi.Services.AI.OpenRouterConfiguration>(config =>
{
    config.ApiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY") ?? 
                   builder.Configuration["OpenRouter:ApiKey"] ?? 
                   "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";
    
    config.Model = Environment.GetEnvironmentVariable("OPENROUTER_MODEL") ?? 
                   builder.Configuration["OpenRouter:Model"] ?? 
                   "deepseek/deepseek-chat";
    
    config.MaxTokens = int.TryParse(Environment.GetEnvironmentVariable("OPENROUTER_MAX_TOKENS"), out int maxTokens) 
                       ? maxTokens 
                       : builder.Configuration.GetValue<int>("OpenRouter:MaxTokens", 800);
    
    config.Temperature = double.TryParse(Environment.GetEnvironmentVariable("OPENROUTER_TEMPERATURE"), out double temp)
                         ? temp
                         : builder.Configuration.GetValue<double>("OpenRouter:Temperature", 0.2);
    
    config.BaseUrl = Environment.GetEnvironmentVariable("OPENROUTER_BASE_URL") ?? 
                     builder.Configuration["OpenRouter:BaseUrl"] ?? 
                     "https://openrouter.ai/api/v1";
    
    config.TimeoutSeconds = builder.Configuration.GetValue<int>("OpenRouter:TimeoutSeconds", 15);
    config.MaxRetries = builder.Configuration.GetValue<int>("OpenRouter:MaxRetries", 3);
    config.CacheDurationHours = builder.Configuration.GetValue<int>("OpenRouter:CacheDurationHours", 24);
    config.HttpReferer = Environment.GetEnvironmentVariable("OPENROUTER_REFERER") ?? 
                        builder.Configuration["OpenRouter:HttpReferer"];
    config.XTitle = builder.Configuration.GetValue<string>("OpenRouter:XTitle", "Psy Tests Admin");
    
    Log.Information("OpenRouter configured with model: {Model}, max tokens: {MaxTokens}, temperature: {Temperature}", 
        config.Model, config.MaxTokens, config.Temperature);
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
// Apply different CORS policies based on the route
app.UseWhen(context => context.Request.Path.StartsWithSegments("/api/admin"), app => app.UseCors("AdminCors"));
app.UseWhen(context => !context.Request.Path.StartsWithSegments("/api/admin"), app => app.UseCors("UserCors"));
app.UseRateLimiting(); // Custom rate limiting for /api/sessions/start
app.UseRateLimiter(); // Built-in rate limiting for other endpoints
app.UseAuthentication();
app.UseAuthorization();
app.UseAuthorization();
app.MapControllers();

// Seed data on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var appDb = services.GetRequiredService<AppDbContext>();
    try
    {
        await appDb.Database.EnsureCreatedAsync();
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Database EnsureCreated failed");
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



