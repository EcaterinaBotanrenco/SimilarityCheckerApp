using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimilarityChecker.Api.Data;
using SimilarityChecker.Api.Data.Entities;
using SimilarityChecker.Api.Models;
using SimilarityChecker.Api.Services;
using SimilarityChecker.Api.Services.Storage;
using SimilarityChecker.Shared.Dto;
using SimilarityChecker.UI.Services.TextExtraction;
using System.Security.Claims;
using System.Text.RegularExpressions;
using MultipleDocumentUploadResponse = SimilarityChecker.Shared.Dto.MultipleDocumentUploadResponse;
using SingleDocumentImportResult = SimilarityChecker.Shared.Dto.SingleDocumentImportResult;

namespace SimilarityChecker.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/documents")]
public sealed class DocumentsController : ControllerBase
{
    private readonly SimilarityCheckerDbContext _db;
    private readonly TextExtractionService _textExtraction;
    private readonly IDocumentStorageService _storage;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".docx", ".txt"
    };

    private const long MaxFileSizeBytes = 20 * 1024 * 1024;

    public DocumentsController(
        SimilarityCheckerDbContext db,
        TextExtractionService textExtraction,
        IDocumentStorageService storage)
    {
        _db = db;
        _textExtraction = textExtraction;
        _storage = storage;
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<DocumentUploadResponse>> Upload(
        [FromForm] DocumentUploadRequest request,
        CancellationToken ct)
    {
        if (request.File is null)
            return BadRequest("Fișierul este obligatoriu.");

        var currentUserId = GetCurrentUserId();

        var result = await ProcessSingleFileAsync(request.File, currentUserId, ct);

        if (!result.IsSuccess)
            return BadRequest(result.Message);

        return Ok(new DocumentUploadResponse
        {
            DocumentId = result.DocumentId!.Value,
            FileName = result.FileName,
            WordCount = result.WordCount ?? 0,
            Sha256 = result.Sha256 ?? string.Empty
        });
    }

    [HttpPost("upload-multiple")]
    [Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<MultipleDocumentUploadResponse>> UploadMultiple(
    [FromForm] List<IFormFile> files,
    CancellationToken ct)
    {
        if (files is null || files.Count == 0)
            return BadRequest("Trebuie selectat cel puțin un fișier.");

        var currentUserId = GetCurrentUserId();
        var response = new MultipleDocumentUploadResponse
        {
            TotalFiles = files.Count
        };

        foreach (var file in files)
        {
            var result = await ProcessSingleFileAsync(file, currentUserId, ct);
            response.Results.Add(result);

            if (result.IsSuccess)
                response.ImportedCount++;
            else
                response.SkippedCount++;
        }

        return Ok(response);
    }

    private async Task<SingleDocumentImportResult> ProcessSingleFileAsync(
        IFormFile? file,
        Guid currentUserId,
        CancellationToken ct)
    {
        if (file is null)
        {
            return new SingleDocumentImportResult
            {
                FileName = "(fără nume)",
                IsSuccess = false,
                Message = "Fișierul este obligatoriu."
            };
        }

        if (file.Length == 0)
        {
            return new SingleDocumentImportResult
            {
                FileName = file.FileName,
                IsSuccess = false,
                Message = "Fișierul este gol."
            };
        }

        if (file.Length > MaxFileSizeBytes)
        {
            return new SingleDocumentImportResult
            {
                FileName = file.FileName,
                IsSuccess = false,
                Message = "Fișierul depășește dimensiunea maximă admisă de 20 MB."
            };
        }

        var fileName = file.FileName?.Trim();
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return new SingleDocumentImportResult
            {
                FileName = "(nume invalid)",
                IsSuccess = false,
                Message = "Numele fișierului este invalid."
            };
        }

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            return new SingleDocumentImportResult
            {
                FileName = fileName,
                IsSuccess = false,
                Message = "Format de fișier neacceptat. Sunt permise doar PDF, DOCX și TXT."
            };
        }

        byte[] bytes;
        try
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms, ct);
            bytes = ms.ToArray();
        }
        catch
        {
            return new SingleDocumentImportResult
            {
                FileName = fileName,
                IsSuccess = false,
                Message = "Fișierul nu a putut fi citit corect."
            };
        }

        if (bytes.Length == 0)
        {
            return new SingleDocumentImportResult
            {
                FileName = fileName,
                IsSuccess = false,
                Message = "Fișierul încărcat nu conține date valide."
            };
        }

        var sha = Hasher.Sha256Hex(bytes);

        var existing = await _db.Documents
            .FirstOrDefaultAsync(d => d.Sha256 == sha && d.UserId == currentUserId, ct);

        if (existing is not null)
        {
            return new SingleDocumentImportResult
            {
                FileName = existing.FileName,
                IsSuccess = true,
                IsDuplicate = true,
                DocumentId = existing.Id,
                WordCount = existing.WordCount,
                Sha256 = existing.Sha256,
                Message = "Document deja existent în baza de date."
            };
        }

        string extractedText;
        try
        {
            extractedText = await _textExtraction.ExtractAsync(bytes, fileName, ct);
        }
        catch
        {
            return new SingleDocumentImportResult
            {
                FileName = fileName,
                IsSuccess = false,
                Message = "Fișierul este corupt, protejat sau nu a putut fi procesat."
            };
        }

        var normalizedText = NormalizeExtractedText(extractedText);
        var wordCount = CountWords(normalizedText);

        if (string.IsNullOrWhiteSpace(normalizedText) || wordCount == 0)
        {
            return new SingleDocumentImportResult
            {
                FileName = fileName,
                IsSuccess = false,
                Message = "Fișierul nu conține text procesabil pentru analiză."
            };
        }

        var documentId = Guid.NewGuid();

        var storedFile = await _storage.SaveOriginalFileAsync(documentId, fileName, bytes, ct);
        var extractedTextPath = await _storage.SaveExtractedTextAsync(documentId, normalizedText, ct);

        var doc = new DocumentEntity
        {
            Id = documentId,
            FileName = fileName,
            FileType = extension.TrimStart('.').ToLowerInvariant(),
            Sha256 = sha,
            UserId = currentUserId,
            WordCount = wordCount,
            CreatedAtUtc = DateTime.UtcNow,
            StoredFilePath = storedFile.RelativePath,
            ExtractedTextPath = extractedTextPath,
            FileSizeBytes = storedFile.FileSizeBytes
        };

        _db.Documents.Add(doc);
        await _db.SaveChangesAsync(ct);

        return new SingleDocumentImportResult
        {
            FileName = doc.FileName,
            IsSuccess = true,
            IsDuplicate = false,
            DocumentId = doc.Id,
            WordCount = doc.WordCount,
            Sha256 = doc.Sha256,
            Message = "Document importat cu succes."
        };
    }

    private static string NormalizeExtractedText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // O variantă infinit mai rapidă care evită blocajele Regex alocând memorie o singură dată
        var sb = new System.Text.StringBuilder(text.Length);
        bool previousWasWhitespace = false;

        foreach (char c in text)
        {
            if (char.IsWhiteSpace(c))
            {
                if (!previousWasWhitespace)
                {
                    sb.Append(' ');
                    previousWasWhitespace = true;
                }
            }
            else
            {
                sb.Append(c);
                previousWasWhitespace = false;
            }
        }

        return sb.ToString().Trim();
    }

    private static int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        int wordCount = 0;
        bool inWord = false;

        // Parcurgem lista de caractere direct (0 alocări de memorie, viteză instantanee)
        for (int i = 0; i < text.Length; i++)
        {
            if (char.IsWhiteSpace(text[i]))
            {
                inWord = false;
            }
            else
            {
                if (!inWord)
                {
                    wordCount++;
                    inWord = true;
                }
            }
        }

        return wordCount;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

        if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
            throw new InvalidOperationException("User ID claim not found or invalid.");

        return userId;
    }
}