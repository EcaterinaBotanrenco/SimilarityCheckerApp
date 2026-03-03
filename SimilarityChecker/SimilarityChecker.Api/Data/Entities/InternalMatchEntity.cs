using System.ComponentModel.DataAnnotations;

namespace SimilarityChecker.Api.Data.Entities;

public sealed class InternalMatchEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DocumentId { get; set; }
    public DocumentEntity Document { get; set; } = null!;

    public Guid ComparedDocumentId { get; set; }
    public DocumentEntity ComparedDocument { get; set; } = null!;

    public double Score { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [MaxLength(50)]
    public string AlgorithmVersion { get; set; } = "internal-v1";
}
