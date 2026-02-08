using Microsoft.AspNetCore.Http;

namespace SimilarityChecker.Api.Models;

public sealed class PlagiarismUploadRequest
{
    public IFormFile MainFile { get; set; } = default!;
    public IFormFile? ReferenceFile { get; set; }
}
