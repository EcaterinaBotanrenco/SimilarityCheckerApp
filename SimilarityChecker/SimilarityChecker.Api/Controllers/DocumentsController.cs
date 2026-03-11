using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimilarityChecker.Api.Data;
using SimilarityChecker.Api.Data.Entities;
using SimilarityChecker.Api.Models;
using SimilarityChecker.Api.Services;
using SimilarityChecker.Api.Services.TextExtraction;
using SimilarityChecker.UI.Services.TextExtraction;
using System.Security.Claims;

namespace SimilarityChecker.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/documents")]
public sealed class DocumentsController : ControllerBase
{
    private readonly SimilarityCheckerDbContext _db;
    private readonly TextExtractionService _textExtraction;

    public DocumentsController(SimilarityCheckerDbContext db, TextExtractionService textExtraction)
    {
        _db = db;
        _textExtraction = textExtraction;
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<DocumentUploadResponse>> Upload(
        [FromForm] DocumentUploadRequest request,
        CancellationToken ct)
    {
        if (request.File == null || request.File.Length == 0)
            return BadRequest("Fișierul este obligatoriu.");

        var currentUserId = GetCurrentUserId();

        byte[] bytes;
        using (var ms = new MemoryStream())
        {
            await request.File.CopyToAsync(ms, ct);
            bytes = ms.ToArray();
        }

        var fileName = request.File.FileName;
        var sha = Hasher.Sha256Hex(bytes);

        var existing = await _db.Documents
            .FirstOrDefaultAsync(d => d.Sha256 == sha && d.UserId == currentUserId, ct);

        if (existing != null)
        {
            return Ok(new DocumentUploadResponse
            {
                DocumentId = existing.Id,
                FileName = existing.FileName,
                WordCount = existing.WordCount,
                Sha256 = existing.Sha256
            });
        }

        var text = await _textExtraction.ExtractAsync(bytes, fileName, ct);
        var wordCount = CountWords(text);

        var doc = new DocumentEntity
        {
            FileName = fileName,
            FileType = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant(),
            Sha256 = sha,
            UserId = currentUserId,
            ExtractedText = text,
            WordCount = wordCount,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Documents.Add(doc);
        await _db.SaveChangesAsync(ct);

        return Ok(new DocumentUploadResponse
        {
            DocumentId = doc.Id,
            FileName = doc.FileName,
            WordCount = doc.WordCount,
            Sha256 = doc.Sha256
        });
    }

    private static int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;

        return text.Split(new[] { ' ', '\r', '\n', '\t' },
            StringSplitOptions.RemoveEmptyEntries).Length;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

        if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
            throw new InvalidOperationException("User ID claim not found or invalid.");

        return userId;
    }
}