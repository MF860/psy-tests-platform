using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PsyApi.Data;
using PsyApi.Models;
using PsyApi.Security;
using PsyApi.Services.Reports;
using PsyApi.Services.Audit;
using PsyApi.Services;
using PsyApi.Services.Testing;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace PsyApi.Controllers
{
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILogger<AdminController> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly JwtOptions _jwt;
        private readonly IPdfReportService _pdf;
        private readonly IAuditService _audit;
        private readonly SessionPersistenceTester _persistenceTester;

        public AdminController(
            AppDbContext db, 
            ILogger<AdminController> logger, 
            ILoggerFactory loggerFactory, 
            IOptions<JwtOptions> jwt, 
            IPdfReportService pdf, 
            IAuditService audit,
            SessionPersistenceTester persistenceTester)
        {
            _db = db;
            _logger = logger;
            _loggerFactory = loggerFactory;
            _jwt = jwt.Value;
            _pdf = pdf;
            _audit = audit;
            _persistenceTester = persistenceTester;
        }

        [HttpPost("login")]
        [EnableRateLimiting("login")]
        public async Task<ActionResult<AdminLoginResponse>> Login([FromBody] AdminLoginRequest req)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            try
            {
                var user = await _db.Admins.AsNoTracking().FirstOrDefaultAsync(a => a.Username == req.Username);
                if (user == null || !PasswordHasher.Verify(req.Password, user.PasswordHash))
                {
                    _logger.LogWarning("Admin login failed for {User} from {IP}", req.Username, ip);
                    // await _audit.LogAsync(null, "login_fail", $"username={req.Username}", ip);
                    return Unauthorized();
                }

                var expires = DateTime.UtcNow.AddHours(2);
                var claims = new List<Claim>
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.Username),
                    new Claim(ClaimTypes.Role, "admin")
                };
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                var token = new JwtSecurityToken(
                    issuer: _jwt.Issuer,
                    audience: _jwt.Audience,
                    claims: claims,
                    expires: expires,
                    signingCredentials: creds);

                var jwt = new JwtSecurityTokenHandler().WriteToken(token);
                _logger.LogInformation("Admin {User} logged in, Trace={Trace}", user.Username, HttpContext.TraceIdentifier);
                // await _audit.LogAsync(user.Id, "login_success", $"username={user.Username}", ip);
                return Ok(new AdminLoginResponse { Token = jwt, ExpiresAt = expires });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Admin login error for {User}", req.Username);
                return StatusCode(500, new { error = "Login error" });
            }
        }

        [HttpGet("results")]
        [Authorize(Policy = "Admin")]
        [EnableRateLimiting("admin")]
        public async Task<ActionResult<PagedResponse<AdminResultListItem>>> ListResults([FromQuery] ResultQuery q)
        {
            q.Page = Math.Max(1, q.Page);
            q.PageSize = Math.Clamp(q.PageSize, 1, 200);

            var query = _db.Results.AsNoTracking()
                .Join(_db.Sessions.AsNoTracking(), r => r.SessionId, s => s.Id, (r, s) => new { r, s })
                .Join(_db.Users.AsNoTracking(), rs => rs.s.UserId, u => u.Id, (rs, u) => new { rs.r, rs.s, u });

            if (!string.IsNullOrWhiteSpace(q.Search))
            {
                query = query.Where(x => x.u.NationalId.Contains(q.Search!) || x.u.FullName.Contains(q.Search!));
            }

            query = (q.Sort?.ToLower()) switch
            {
                "total_score" => (q.Dir?.ToLower() == "asc") ? query.OrderBy(x => x.r.TotalScore) : query.OrderByDescending(x => x.r.TotalScore),
                _ => (q.Dir?.ToLower() == "asc") ? query.OrderBy(x => x.r.CreatedAt) : query.OrderByDescending(x => x.r.CreatedAt)
            };

            var total = await query.CountAsync();
            var data = await query
                .Skip((q.Page - 1) * q.PageSize)
                .Take(q.PageSize)
                .Select(x => new AdminResultListItem
                {
                    ResultId = x.r.Id,
                    SessionId = x.r.SessionId,
                    SessionGuid = x.s.SessionId,
                    NationalId = x.u.NationalId,
                    FullName = x.u.FullName,
                    TotalScore = x.r.TotalScore,
                    CreatedAt = x.r.CreatedAt
                })
                .ToListAsync();

            Response.Headers["X-Total-Count"] = total.ToString();
            _logger.LogInformation("Admin results list by {User} Trace={Trace}", User?.Identity?.Name, HttpContext.TraceIdentifier);
            return Ok(new PagedResponse<AdminResultListItem> { Page = q.Page, PageSize = q.PageSize, Total = total, Data = data });
        }

        [HttpGet("results/{id:int}")]
        [Authorize(Policy = "Admin")]
        [EnableRateLimiting("admin")]
        public async Task<ActionResult<AdminResultDetail>> GetResult(int id)
        {
            var entity = await _db.Results.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
            if (entity == null) return NotFound();
            var session = await _db.Sessions.AsNoTracking().FirstOrDefaultAsync(s => s.Id == entity.SessionId);
            if (session == null) return NotFound();
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == session.UserId);
            if (user == null) return NotFound();

            var dims = ParseDimensionScores(entity.DimensionScoresJson);

            // Extract SDJ data from DimensionScoresJson (V2) or Session.Payload (V1 fallback)
            SdjDataDto? sdjData = null;
            
            // Try parsing SDJ V2 format first (PatternScores/SubDimensionScores)
            if (!string.IsNullOrWhiteSpace(entity.DimensionScoresJson))
            {
                try
                {
                    var scoresDoc = JsonDocument.Parse(entity.DimensionScoresJson);
                    
                    // Check for V2 format (PatternScores)
                    if (scoresDoc.RootElement.TryGetProperty("PatternScores", out var patternScores) && patternScores.ValueKind == JsonValueKind.Array)
                    {
                        sdjData = new SdjDataDto
                        {
                            Dimensions = patternScores.EnumerateArray()
                                .Select(p => new SdjDimensionDto
                                {
                                    Dimension = p.GetProperty("PatternNameAr").GetString() ?? "",
                                    Raw = p.GetProperty("Raw").GetDouble(),
                                    T = p.GetProperty("TScore").GetDouble(),
                                    Percentile = p.GetProperty("Percentile").GetDouble(),
                                    Band = p.GetProperty("Band").GetString() ?? ""
                                }).ToList(),
                            SubDimensions = scoresDoc.RootElement.TryGetProperty("SubDimensionScores", out var subDimScores) && subDimScores.ValueKind == JsonValueKind.Array
                                ? subDimScores.EnumerateArray()
                                    .Select(sd => new SdjSubDimensionDto
                                    {
                                        Dimension = sd.GetProperty("PatternId").GetString() ?? "",
                                        SubDimension = sd.GetProperty("SubNameAr").GetString() ?? "",
                                        T = sd.GetProperty("TScore").GetDouble(),
                                        Band = sd.GetProperty("Band").GetString() ?? ""
                                    }).ToList()
                                : new List<SdjSubDimensionDto>(),
                            TrackFits = new List<SdjTrackDto>(), // V2 doesn't use track fits
                            SevenPatternScores = patternScores.EnumerateArray()
                                .Select(p => new SevenPatternScoreDto
                                {
                                    PatternNameAr = p.GetProperty("PatternNameAr").GetString() ?? "",
                                    PatternNameEn = p.GetProperty("PatternKey").GetString() ?? "",
                                    TScore = p.GetProperty("TScore").GetDouble(),
                                    Band = p.GetProperty("Band").GetString() ?? "",
                                    SubDimensions = p.TryGetProperty("SubDimensions", out var subs) && subs.ValueKind == JsonValueKind.Array
                                        ? subs.EnumerateArray().Select(s => 
                                            s.TryGetProperty("SubNameAr", out var name) ? name.GetString() ?? "" : "").ToList()
                                        : new List<string>()
                                }).ToList(),
                            Version = scoresDoc.RootElement.TryGetProperty("Version", out var version)
                                ? version.GetString()
                                : "SDJ_v2"
                        };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Not V2 format, trying V1 fallback for result {ResultId}", id);
                }
            }
            
            // Fallback to V1 format from Session.Payload
            if (sdjData == null && !string.IsNullOrWhiteSpace(session.Payload))
            {
                try
                {
                    var payloadDoc = JsonDocument.Parse(session.Payload);
                    if (payloadDoc.RootElement.TryGetProperty("SdjData", out var sdjElement))
                    {
                        sdjData = new SdjDataDto
                        {
                            Dimensions = sdjElement.GetProperty("Dimensions").EnumerateArray()
                                .Select(d => new SdjDimensionDto
                                {
                                    Dimension = d.GetProperty("Dimension").GetString() ?? "",
                                    Raw = d.GetProperty("Raw").GetDouble(),
                                    T = d.GetProperty("T").GetDouble(),
                                    Percentile = d.GetProperty("Percentile").GetDouble(),
                                    Band = d.GetProperty("Band").GetString() ?? ""
                                }).ToList(),
                            SubDimensions = sdjElement.TryGetProperty("SubDimensions", out var subDims) 
                                ? subDims.EnumerateArray()
                                    .Select(sd => new SdjSubDimensionDto
                                    {
                                        Dimension = sd.GetProperty("Dimension").GetString() ?? "",
                                        SubDimension = sd.GetProperty("SubDimension").GetString() ?? "",
                                        T = sd.GetProperty("T").GetDouble(),
                                        Band = sd.GetProperty("Band").GetString() ?? ""
                                    }).ToList()
                                : new List<SdjSubDimensionDto>(),
                            TrackFits = sdjElement.TryGetProperty("TrackFits", out var tracks) 
                                ? tracks.EnumerateArray()
                                    .Select(t => new SdjTrackDto
                                    {
                                        TrackNameAr = t.GetProperty("TrackNameAr").GetString() ?? "",
                                        FitLevel = t.GetProperty("FitLevel").GetString() ?? "",
                                        FitScore = t.GetProperty("FitScore").GetDouble()
                                    }).ToList()
                                : new List<SdjTrackDto>(),
                            SevenPatternScores = sdjElement.TryGetProperty("SevenPatternScores", out var sevenPatterns)
                                ? sevenPatterns.EnumerateArray()
                                    .Select(sp => new SevenPatternScoreDto
                                    {
                                        PatternNameAr = sp.GetProperty("PatternNameAr").GetString() ?? "",
                                        PatternNameEn = sp.GetProperty("PatternNameEn").GetString() ?? "",
                                        TScore = sp.GetProperty("TScore").GetDouble(),
                                        Band = sp.GetProperty("Band").GetString() ?? "",
                                        SubDimensions = sp.TryGetProperty("SubDimensions", out var subs)
                                            ? subs.EnumerateArray().Select(s => s.GetString() ?? "").ToList()
                                            : new List<string>()
                                    }).ToList()
                                : new List<SevenPatternScoreDto>(),
                            Version = sdjElement.TryGetProperty("Version", out var version)
                                ? version.GetString()
                                : null
                        };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse SDJ data from session payload for result {ResultId}", id);
                }
            }

            var detail = new AdminResultDetail
            {
                ResultId = entity.Id,
                SessionId = entity.SessionId,
                SessionGuid = session.SessionId,
                NationalId = user.NationalId,
                FullName = user.FullName ?? "N/A",
                TotalScore = entity.TotalScore,
                ScoringModelVersion = entity.ScoringModelVersion ?? string.Empty,
                CreatedAt = entity.CreatedAt,
                Dimensions = dims,
                SdjData = sdjData
            };

            var eTag = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(entity.Id + ":" + entity.CreatedAt.Ticks + ":" + (entity.DimensionScoresJson ?? string.Empty))));
            Response.Headers["ETag"] = $"W/\"{eTag}\"";
            _logger.LogInformation("Admin viewed result {ResultId} user={Admin} Trace={Trace}", id, User?.Identity?.Name, HttpContext.TraceIdentifier);
            try
            {
                var sub = User?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
                var admin = string.IsNullOrWhiteSpace(sub) ? null : await _db.Admins.AsNoTracking().FirstOrDefaultAsync(a => a.Username == sub);
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                await _audit.LogAsync(admin?.Id, "view_result", $"resultId={id}", ip);
            }
            catch { }
            return Ok(detail);
        }

        [HttpGet("results/{id:int}/pdf")]
        [Authorize(Policy = "Admin")]
        [EnableRateLimiting("admin")]
        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any, NoStore = false)]
        public async Task<IActionResult> GetResultPdf(int id, CancellationToken ct)
        {
            var entity = await _db.Results.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, ct);
            if (entity == null) return NotFound();
            var session = await _db.Sessions.AsNoTracking().FirstOrDefaultAsync(s => s.Id == entity.SessionId, ct);
            if (session == null) return NotFound();
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == session.UserId, ct);
            if (user == null) return NotFound();

            // Compute weak ETag from payload signature
            var sigSource = $"{entity.Id}:{entity.SessionId}:{entity.TotalScore}:{entity.ScoringModelVersion}:{entity.DimensionScoresJson?.Length ?? 0}:{entity.CreatedAt.Ticks}";
            var sigBytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(sigSource));
            var etag = "W/\"" + Convert.ToBase64String(sigBytes)[..16] + "\"";

            var ifNoneMatch = Request.Headers["If-None-Match"].ToString();
            if (!string.IsNullOrEmpty(ifNoneMatch) && string.Equals(ifNoneMatch, etag, StringComparison.Ordinal))
            {
                Response.Headers["ETag"] = etag;
                return StatusCode(304);
            }

            // Detect SDJ data
            var hasSdjData = !string.IsNullOrWhiteSpace(entity.DimensionScoresJson) && 
                             entity.DimensionScoresJson.Contains("\"SubDimensions\"");

            byte[] bytes;
            if (hasSdjData)
            {
                _logger.LogInformation("Admin generating SDJ PDF for result {ResultId}", id);
                bytes = await _pdf.RenderSdjResultPdfAsync(entity, user, ct);
            }
            else
            {
                _logger.LogInformation("Admin generating legacy PDF for result {ResultId}", id);
                var dims = ParseDimensionScores(entity.DimensionScoresJson);
                bytes = await _pdf.RenderResultPdfAsync(entity, user, dims, ct);
            }

            var filename = $"psy-report-{user.NationalId}-{entity.Id}.pdf";
            _logger.LogInformation("Admin downloaded PDF result {ResultId} user={Admin} Trace={Trace}", id, User?.Identity?.Name, HttpContext.TraceIdentifier);
            try
            {
                var sub = User?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
                var admin = string.IsNullOrWhiteSpace(sub) ? null : await _db.Admins.AsNoTracking().FirstOrDefaultAsync(a => a.Username == sub, ct);
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                await _audit.LogAsync(admin?.Id, "download_pdf", $"resultId={id}", ip);
            }
            catch { }
            Response.Headers["ETag"] = etag;
            Response.Headers["Cache-Control"] = "public, max-age=300";
            return File(bytes, "application/pdf", filename);
        }

        [HttpGet("questions/validate")]
        [Authorize(Policy = "Admin")]
        [EnableRateLimiting("admin")]
        public async Task<IActionResult> ValidateQuestions()
        {
            var validator = new QuestionValidatorService(_db, _loggerFactory.CreateLogger<QuestionValidatorService>());
            var result = await validator.ValidateAllQuestionsAsync();
            
            var violations = result.InvalidQuestions.Select(q => new {
                itemId = q.ItemId,
                type = q.CurrentType,
                expectedType = q.ExpectedType,
                messages = q.Issues
            }).ToList();
            
            return Ok(new { 
                ok = result.InvalidQuestions.Count == 0, 
                count = result.TotalQuestions, 
                validCount = result.ValidQuestions,
                violations = violations 
            });
        }
        
        [HttpPost("questions/fix")]
        [Authorize(Policy = "Admin")]
        [EnableRateLimiting("admin")]
        public async Task<IActionResult> FixQuestions()
        {
            var validator = new QuestionValidatorService(_db, _loggerFactory.CreateLogger<QuestionValidatorService>());
            var result = await validator.FixAllQuestionsAsync();
            
            return Ok(new { 
                totalQuestions = result.TotalQuestions, 
                fixedCount = result.FixedQuestions,
                details = result.FixedQuestionDetails
            });
        }

        [HttpPost("change-password")]
        [Authorize(Policy = "Admin")]
        [EnableRateLimiting("login")]
        public async Task<ActionResult<ChangePasswordResponse>> ChangePassword(
            [FromBody] ChangePasswordRequest req,
            [FromServices] IAuditService audit,
            [FromServices] AppDbContext db)
        {
            var username = User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(username))
                return Unauthorized(new { error = "Invalid token" });

            var admin = await db.Admins.FirstOrDefaultAsync(a => a.Username == username);
            if (admin == null)
                return Unauthorized(new { error = "Admin not found" });

            // Verify old password
            if (!PasswordHasher.Verify(req.OldPassword, admin.PasswordHash))
            {
                await audit.LogAsync(admin.Id, "change_password_fail", "wrong old password", HttpContext.Connection.RemoteIpAddress?.ToString());
                return BadRequest(new { error = "Invalid current password" });
            }

            // Hash and update new password
            admin.PasswordHash = PasswordHasher.Hash(req.NewPassword);
            await db.SaveChangesAsync();

            // Audit log
            await audit.LogAsync(admin.Id, "change_password", "password changed", HttpContext.Connection.RemoteIpAddress?.ToString());

            return Ok(new ChangePasswordResponse { Message = "Password changed successfully" });
        }

        [HttpGet("test/session-persistence/{sessionId:int}")]
        [Authorize(Policy = "Admin")]
        [EnableRateLimiting("admin")]
        public async Task<ActionResult<Dictionary<string, bool>>> ValidateSessionPersistence(int sessionId)
        {
            try
            {
                var results = await _persistenceTester.ValidateSessionPersistence(sessionId);
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating session persistence for session {SessionId}", sessionId);
                return StatusCode(500, new { error = "Validation error" });
            }
        }

        [HttpPost("results/{id:int}/recommendations")]
        [Authorize(Policy = "Admin")]
        [EnableRateLimiting("admin")]
        public async Task<ActionResult<RecommendationsResponse>> GetRecommendations(
            int id, 
            [FromBody] GetRecommendationsRequest? request = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Get the result and validate it exists
                var result = await _db.Results.AsNoTracking()
                    .Include(r => r.Session)
                    .ThenInclude(s => s.User)
                    .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

                if (result == null)
                {
                    return NotFound(new { error = "Result not found" });
                }

                // Parse dimension scores
                var dimensionScores = ParseDimensionScores(result.DimensionScoresJson);

                if (!dimensionScores.Any())
                {
                    return BadRequest(new { error = "No dimension scores available for recommendations" });
                }

                // Get OpenAI recommendations service
                var openAIService = HttpContext.RequestServices.GetService<PsyApi.Services.AI.IOpenAIRecommendationsService>();
                if (openAIService == null)
                {
                    _logger.LogWarning("OpenAI recommendations service not configured");
                    return StatusCode(503, new { error = "AI recommendations service unavailable" });
                }

                // Generate recommendations
                var participantId = result.Session?.User?.NationalId ?? $"user_{result.Session?.UserId}";
                var recommendations = await openAIService.GenerateRecommendationsAsync(
                    resultId: id,
                    participantId: participantId,
                    dimensions: dimensionScores,
                    totalScore: result.TotalScore,
                    context: request?.Context,
                    forceRegenerate: request?.ForceRegenerate ?? false,
                    cancellationToken: cancellationToken);

                // Log the action for audit
                var adminUsername = User?.Identity?.Name ?? "Unknown";
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                _logger.LogInformation(
                    "Admin {Admin} generated AI recommendations for result {ResultId} (IP: {IP})", 
                    adminUsername, id, ip);

                return Ok(recommendations);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Recommendations request cancelled for result {ResultId}", id);
                return StatusCode(408, new { error = "Request timeout" });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "OpenAI API error for result {ResultId}", id);
                return StatusCode(502, new { error = "External AI service error", details = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating recommendations for result {ResultId}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("recommendations/usage")]
        [Authorize(Policy = "Admin")]
        [EnableRateLimiting("admin")]
        public async Task<ActionResult<OpenAIUsageMetrics>> GetRecommendationsUsage()
        {
            try
            {
                var openAIService = HttpContext.RequestServices.GetService<PsyApi.Services.AI.IOpenAIRecommendationsService>();
                if (openAIService == null)
                {
                    return StatusCode(503, new { error = "AI recommendations service unavailable" });
                }

                var usage = await openAIService.GetUsageMetricsAsync();
                return Ok(usage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting OpenAI usage metrics");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("audit")]
        [Authorize(Policy = "Admin")]
        public async Task<ActionResult<PagedResponse<AuditLogDto>>> GetAudit([FromQuery] AuditQuery q)
        {
            q.Page = Math.Max(1, q.Page);
            q.PageSize = Math.Clamp(q.PageSize, 1, 200);
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var logs = _db.AuditLogs.AsNoTracking();

            if (q.From.HasValue)
                logs = logs.Where(l => l.CreatedAt >= q.From.Value);
            if (q.To.HasValue)
                logs = logs.Where(l => l.CreatedAt <= q.To.Value);

            // Left join to admins by UserId
            var query = from l in logs
                        join a in _db.Admins.AsNoTracking() on l.UserId equals a.Id into gj
                        from a in gj.DefaultIfEmpty()
                        select new { l, a };

            if (!string.IsNullOrWhiteSpace(q.Search))
            {
                var s = q.Search!;
                query = query.Where(x => x.l.Action.Contains(s) || (x.a != null && x.a.Username.Contains(s)));
            }

            var total = await query.CountAsync();
            var data = await query
                .OrderByDescending(x => x.l.CreatedAt)
                .Skip((q.Page - 1) * q.PageSize)
                .Take(q.PageSize)
                .Select(x => new AuditLogDto
                {
                    Id = x.l.Id,
                    Action = x.l.Action,
                    AdminUsername = x.a != null ? x.a.Username : string.Empty,
                    IpAddress = x.l.IpAddress ?? string.Empty,
                    Details = x.l.Details ?? string.Empty,
                    CreatedAt = x.l.CreatedAt
                })
                .ToListAsync();

            _logger.LogInformation("Admin audit list by {Admin} ip={IP} Trace={Trace}", User?.Identity?.Name, ip, HttpContext.TraceIdentifier);
            return Ok(new PagedResponse<AuditLogDto> { Page = q.Page, PageSize = q.PageSize, Total = total, Data = data });
        }

        // Helper method to parse dimension scores from both SDJ and legacy formats
        private List<DimensionScore> ParseDimensionScores(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<DimensionScore>();
            }

            var opts = new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

            try
            {
                // Try SDJ V2 format first (PatternScores)
                var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("PatternScores", out var patternScores) && patternScores.ValueKind == JsonValueKind.Array)
                {
                    // Map PatternScores to DimensionScore for V2 format
                    return patternScores.EnumerateArray()
                        .Select(p => new DimensionScore
                        {
                            Dimension = p.GetProperty("PatternNameAr").GetString() ?? "",
                            Raw = p.GetProperty("Raw").GetDouble(),
                            T = p.GetProperty("TScore").GetDouble(),
                            Percentile = p.GetProperty("Percentile").GetDouble(),
                            Z = 0 // Z-score not stored in V2 format
                        }).ToList();
                }
                
                // Try SDJ V1 format (Dimensions in root)
                if (doc.RootElement.TryGetProperty("Dimensions", out var dimensions) && dimensions.ValueKind == JsonValueKind.Array)
                {
                    return dimensions.EnumerateArray()
                        .Select(d => new DimensionScore
                        {
                            Dimension = d.GetProperty("Dimension").GetString() ?? "",
                            Raw = d.GetProperty("Raw").GetDouble(),
                            T = d.GetProperty("T").GetDouble(),
                            Percentile = d.GetProperty("Percentile").GetDouble(),
                            Z = 0
                        }).ToList();
                }
            }
            catch (JsonException ex)
            {
                _logger.LogDebug(ex, "Not SDJ format, trying legacy format");
            }

            try
            {
                // Try legacy format (flat array of DimensionScore)
                var scores = JsonSerializer.Deserialize<List<DimensionScore>>(json, opts);
                return scores ?? new List<DimensionScore>();
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse dimension scores from JSON");
                return new List<DimensionScore>();
            }
        }

    }
}
