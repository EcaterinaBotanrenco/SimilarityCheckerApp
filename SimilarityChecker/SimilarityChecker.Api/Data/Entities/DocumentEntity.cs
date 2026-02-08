using System.ComponentModel.DataAnnotations;

namespace SimilarityChecker.Api.Data.Entities;

public sealed class DocumentEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(260)]
    public string FileName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string FileType { get; set; } = string.Empty; // pdf/docx/txt

    [MaxLength(64)]
    public string Sha256 { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public int WordCount { get; set; }

    [MaxLength(10)]
    public string? Language { get; set; }

    public string ExtractedText { get; set; } = string.Empty;

    // Navigații
    public List<SearchQueryEntity> SearchQueries { get; set; } = new();
    public List<MatchEntity> Matches { get; set; } = new();
    public List<ReportEntity> Reports { get; set; } = new();
}
