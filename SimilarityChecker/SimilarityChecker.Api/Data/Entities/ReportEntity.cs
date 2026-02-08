using System.ComponentModel.DataAnnotations;

namespace SimilarityChecker.Api.Data.Entities;

public sealed class ReportEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DocumentId { get; set; }
    public DocumentEntity Document { get; set; } = null!;

    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;

    // Păstrezi raportul complet (surse, match-uri, scoruri, etc.)
    public string ReportJson { get; set; } = "{}";

    [MaxLength(20)]
    public string Status { get; set; } = "Completed"; // Pending/Completed/Failed

    [MaxLength(500)]
    public string? Error { get; set; }
}
