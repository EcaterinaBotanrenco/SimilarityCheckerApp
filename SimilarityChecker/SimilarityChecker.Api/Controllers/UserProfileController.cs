using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimilarityChecker.Api.Data;
using SimilarityChecker.Shared.Dtos;
using System.Security.Claims;
using System.Text.Json;

namespace SimilarityChecker.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/profile")]
    public sealed class ProfileController : ControllerBase
    {
        private readonly SimilarityCheckerDbContext _db;

        public ProfileController(SimilarityCheckerDbContext db)
        {
            _db = db;
        }

        [HttpGet("me")]
        public async Task<ActionResult<UserProfileDto>> GetMyProfile(CancellationToken ct)
        {
            var currentUserId = GetCurrentUserId();

            var user = await _db.AppUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == currentUserId, ct);

            if (user is null)
                return NotFound("Utilizatorul nu a fost găsit.");

            var reports = await _db.Reports
                .AsNoTracking()
                .Include(r => r.Document)
                .Where(r => r.UserId == currentUserId)
                .OrderByDescending(r => r.GeneratedAtUtc)
                .ToListAsync(ct);

            var reportDtos = reports.Select(r =>
            {
                int exactPercent = 0;
                int paraphrasePercent = 0;
                int cleanPercent = 0;

                try
                {
                    using var jsonDoc = JsonDocument.Parse(r.ReportJson);
                    var root = jsonDoc.RootElement;

                    if (root.TryGetProperty("exactPercent", out var exactProp))
                        exactPercent = exactProp.GetInt32();

                    if (root.TryGetProperty("paraphrasePercent", out var paraProp))
                        paraphrasePercent = paraProp.GetInt32();

                    if (root.TryGetProperty("cleanPercent", out var cleanProp))
                        cleanPercent = cleanProp.GetInt32();
                }
                catch
                {
                    // lăsăm valorile 0 dacă JSON-ul nu poate fi citit
                }

                return new UserReportItemDto
                {
                    ReportId = r.Id,
                    DocumentId = r.DocumentId,
                    FileName = r.Document.FileName,
                    GeneratedAtUtc = r.GeneratedAtUtc,
                    Status = r.Status,
                    ExactPercent = exactPercent,
                    ParaphrasePercent = paraphrasePercent,
                    CleanPercent = cleanPercent
                };
            }).ToList();

            var dto = new UserProfileDto
            {
                UserId = user.Id.ToString(),
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                DisplayName = $"{user.FirstName} {user.LastName}".Trim(),
                Roles = user.RolesCsv
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                Reports = reportDtos
            };

            return Ok(dto);
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
                throw new InvalidOperationException("User ID claim not found or invalid.");

            return userId;
        }
    }
}