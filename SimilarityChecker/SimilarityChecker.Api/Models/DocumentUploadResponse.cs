namespace SimilarityChecker.Api.Models;

public sealed class DocumentUploadResponse
{
    public Guid DocumentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int WordCount { get; set; }
    public string Sha256 { get; set; } = string.Empty;
}
