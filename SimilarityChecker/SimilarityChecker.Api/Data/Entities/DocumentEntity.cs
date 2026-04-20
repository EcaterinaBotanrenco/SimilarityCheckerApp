using System.ComponentModel.DataAnnotations;

namespace SimilarityChecker.Api.Data.Entities;

public sealed class DocumentEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public AppUserEntity User { get; set; } = null!;

    [MaxLength(260)]
    public string FileName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string FileType { get; set; } = string.Empty;

    [MaxLength(64)]
    public string Sha256 { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public int WordCount { get; set; }

    [MaxLength(10)]
    public string? Language { get; set; }

    [MaxLength(500)]
    public string StoredFilePath { get; set; } = string.Empty;

    [MaxLength(500)]
    public string ExtractedTextPath { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    public List<SearchQueryEntity> SearchQueries { get; set; } = new();
    public List<MatchEntity> Matches { get; set; } = new();
    public List<ReportEntity> Reports { get; set; } = new();
}