using Microsoft.AspNetCore.Http;

namespace SimilarityChecker.Api.Models;

public sealed class DocumentUploadRequest
{
    public IFormFile File { get; set; } = default!;
}
