namespace SimilarityChecker.Api.Models;

public sealed class MultipleDocumentUploadResponse
{
    public int TotalFiles { get; set; }
    public int ImportedCount { get; set; }
    public int SkippedCount { get; set; }
    public List<SingleDocumentImportResult> Results { get; set; } = new();
}