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
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace PsyApi.Controllers
{
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILogger<AdminController> _logger;
        private readonly JwtOptions _jwt;
        private readonly IPdfReportService _pdf;

        public AdminController(AppDbContext db, ILogger<AdminController> logger, IOptions<JwtOptions> jwt, IPdfReportService pdf)
        {
            _db = db;
            _logger = logger;
            _jwt = jwt.Value;
            _pdf = pdf;
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

            var opts = new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
            var dims = string.IsNullOrWhiteSpace(entity.DimensionScoresJson)
                ? new List<DimensionScore>()
                : (JsonSerializer.Deserialize<List<DimensionScore>>(entity.DimensionScoresJson, opts) ?? new List<DimensionScore>());

            var detail = new AdminResultDetail
            {
                ResultId = entity.Id,
                SessionId = entity.SessionId,
                NationalId = user.NationalId,
                FullName = user.FullName ?? "N/A",
                TotalScore = entity.TotalScore,
                ScoringModelVersion = entity.ScoringModelVersion ?? string.Empty,
                CreatedAt = entity.CreatedAt,
                Dimensions = dims
            };

            var eTag = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(entity.Id + ":" + entity.CreatedAt.Ticks + ":" + (entity.DimensionScoresJson ?? string.Empty))));
            Response.Headers["ETag"] = $"W/\"{eTag}\"";
            _logger.LogInformation("Admin viewed result {ResultId} user={Admin} Trace={Trace}", id, User?.Identity?.Name, HttpContext.TraceIdentifier);
            return Ok(detail);
        }

        [HttpGet("results/{id:int}/pdf")]
        [Authorize(Policy = "Admin")]
        [EnableRateLimiting("admin")]
        public async Task<IActionResult> GetResultPdf(int id, CancellationToken ct)
        {
            var entity = await _db.Results.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, ct);
            if (entity == null) return NotFound();
            var session = await _db.Sessions.AsNoTracking().FirstOrDefaultAsync(s => s.Id == entity.SessionId, ct);
            if (session == null) return NotFound();
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == session.UserId, ct);
            if (user == null) return NotFound();

            var opts = new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
            var dims = string.IsNullOrWhiteSpace(entity.DimensionScoresJson)
                ? new List<DimensionScore>()
                : (JsonSerializer.Deserialize<List<DimensionScore>>(entity.DimensionScoresJson, opts) ?? new List<DimensionScore>());

            var bytes = await _pdf.RenderResultPdfAsync(entity, user, dims, ct);
            var filename = $"psy-report-{user.NationalId}-{entity.Id}.pdf";
            _logger.LogInformation("Admin downloaded PDF result {ResultId} user={Admin} Trace={Trace}", id, User?.Identity?.Name, HttpContext.TraceIdentifier);
            return File(bytes, "application/pdf", filename);
        }
    }
}

