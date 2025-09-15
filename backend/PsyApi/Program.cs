using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using PsyApi.Data;
using PsyApi.Services;
using PsyApi.Services.Scoring;
using PsyApi.Services.Reports;
using PsyApi.Security;
using PsyApi.Services.Audit;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IO.Compression;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddControllers();

// Response Compression
builder.Services.AddResponseCompression(o =>
{
    o.EnableForHttps = true;
    o.Providers.Add<BrotliCompressionProvider>();
    o.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);

// CORS for Admin UI
builder.Services.AddCors(o =>
{
    o.AddPolicy("AdminCors", p => p
        .WithOrigins("http://localhost:5173", "http://localhost:3000")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

// Add DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

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

// Reports
builder.Services.AddScoped<IPdfReportService, PdfReportService>();

// Audit
builder.Services.AddScoped<IAuditService, AuditService>();

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
app.UseCors("AdminCors");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseAuthorization();
app.MapControllers();

// Seed data on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dataSeeder = services.GetRequiredService<DataSeeder>();
    await dataSeeder.SeedItemsAsync();
    await dataSeeder.SeedItemParametersAsync();

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
