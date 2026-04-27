namespace SimilarityChecker.Shared.Dto;

public sealed class MultipleDocumentUploadResponseDto
{
    public int TotalFiles { get; set; }
    public int ImportedCount { get; set; }
    public int SkippedCount { get; set; }
    public List<SingleDocumentImportResultDto> Results { get; set; } = new();
}