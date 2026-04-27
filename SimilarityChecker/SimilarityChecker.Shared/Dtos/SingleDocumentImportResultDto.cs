namespace SimilarityChecker.Shared.Dto;

public sealed class SingleDocumentImportResultDto
{
    public string FileName { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public bool IsDuplicate { get; set; }
    public Guid? DocumentId { get; set; }
    public int? WordCount { get; set; }
    public string? Sha256 { get; set; }
    public string Message { get; set; } = string.Empty;
}